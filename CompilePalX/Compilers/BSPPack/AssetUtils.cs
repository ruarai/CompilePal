﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using CompilePalX;
using CompilePalX.Compiling;
using ValveKeyValue;

namespace CompilePalX.Compilers.BSPPack
{
    static class AssetUtils
    {
        public static ValveKeyValue.KVSerializer KVSerializer = ValveKeyValue.KVSerializer.Create(ValveKeyValue.KVSerializationFormat.KeyValues1Text);

        public static Tuple<List<string>, List<string>> FindMdlMaterialsAndModels(string path, List<int>? skins = null, List<string>? vtxVmts = null)
        {
            List<string> materials = [];
            List<string> models = [];

            if (File.Exists(path))
            {
                using FileStream mdl = new FileStream(path, FileMode.Open);
                BinaryReader reader = new BinaryReader(mdl);

                mdl.Seek(4, SeekOrigin.Begin);
                int ver = reader.ReadInt32();

                List<string> modelVmts = [];
                List<string> modelDirs = [];

                mdl.Seek(76, SeekOrigin.Begin);
                int datalength = reader.ReadInt32();
                mdl.Seek(124, SeekOrigin.Current);

                int textureCount = reader.ReadInt32();
                int textureOffset = reader.ReadInt32();

                int textureDirCount = reader.ReadInt32();
                int textureDirOffset = reader.ReadInt32();

                int skinreferenceCount = reader.ReadInt32();
                int skinrfamilyCount = reader.ReadInt32();
                int skinreferenceIndex = reader.ReadInt32();

                int bodypartCount = reader.ReadInt32();
                int bodypartIndex = reader.ReadInt32();

                // skip to keyvalues
                mdl.Seek(72, SeekOrigin.Current);
                int keyvalueIndex = reader.ReadInt32();
                int keyvalueCount = reader.ReadInt32();

                // skip to includemodel
                mdl.Seek(16, SeekOrigin.Current);
                //mdl.Seek(96, SeekOrigin.Current);
                int includeModelCount = reader.ReadInt32();
                int includeModelIndex = reader.ReadInt32();

                // find model names
                for (int i = 0; i < textureCount; i++)
                {
                    mdl.Seek(textureOffset + (i * 64), SeekOrigin.Begin);
                    int textureNameOffset = reader.ReadInt32();

                    mdl.Seek(textureOffset + (i * 64) + textureNameOffset, SeekOrigin.Begin);
                    modelVmts.Add(ReadNullTerminatedString(mdl, reader));
                }

                // find model dirs
                List<int> textureDirOffsets = [];
                for (int i = 0; i < textureDirCount; i++)
                {
                    mdl.Seek(textureDirOffset + (4 * i), SeekOrigin.Begin);
                    int offset = reader.ReadInt32();
                    mdl.Seek(offset, SeekOrigin.Begin);

                    string model = ReadNullTerminatedString(mdl, reader);
                    model = model.TrimStart(['/', '\\']);
                    modelDirs.Add(model);
                }

                if (skins != null)
                {
                    // load specific skins
                    List<int> material_ids = [];

                    for (int i = 0; i < bodypartCount; i++)
                    // we are reading an array of mstudiobodyparts_t
                    {
                        mdl.Seek(bodypartIndex + i * 16, SeekOrigin.Begin);

                        mdl.Seek(4, SeekOrigin.Current);
                        int nummodels = reader.ReadInt32();
                        mdl.Seek(4, SeekOrigin.Current);
                        int modelindex = reader.ReadInt32();

                        for (int j = 0; j < nummodels; j++)
                        // we are reading an array of mstudiomodel_t
                        {
                            mdl.Seek((bodypartIndex + i * 16) + modelindex + j * 148, SeekOrigin.Begin);
                            long modelFileInputOffset = mdl.Position;

                            mdl.Seek(72, SeekOrigin.Current);
                            int nummeshes = reader.ReadInt32();
                            int meshindex = reader.ReadInt32();

                            for (int k = 0; k < nummeshes; k++)
                            // we are reading an array of mstudiomesh_t
                            {
                                mdl.Seek(modelFileInputOffset + meshindex + (k * 116), SeekOrigin.Begin);
                                int mat_index = reader.ReadInt32();

                                if (!material_ids.Contains(mat_index))
                                    material_ids.Add(mat_index);
                            }
                        }
                    }

                    // read the skintable
                    mdl.Seek(skinreferenceIndex, SeekOrigin.Begin);
                    short[,] skintable = new short[skinrfamilyCount, skinreferenceCount];
                    for (int i = 0; i < skinrfamilyCount; i++)
                    {
                        for (int j = 0; j < skinreferenceCount; j++)
                        {
                            skintable[i, j] = reader.ReadInt16();
                        }
                    }

                    // trim the larger than required skintable
                    short[,] trimmedtable = new short[skinrfamilyCount, material_ids.Count];
                    for (int i = 0; i < skinrfamilyCount; i++)
                        for (int j = 0; j < material_ids.Count; j++)
                            trimmedtable[i, j] = skintable[i, material_ids[j]];

                    // add default skin 0 in case of non-existing skin indexes
                    if (skins.IndexOf(0) == -1 && skins.Count(s => s >= trimmedtable.GetLength(0)) != 0)
                        skins.Add(0);

                    // use the trimmed table to fetch used vmts
                    foreach (int skin in skins.Where(skin => skin < trimmedtable.GetLength(0)))
                        for (int j = 0; j < material_ids.Count; j++)
                            for (int k = 0; k < modelDirs.Count; k++)
                            {
                                short id = trimmedtable[skin, j];
                                materials.Add("materials/" + modelDirs[k] + modelVmts[id] + ".vmt");
                            }
                }
                else
                    // load all vmts
                    for (int i = 0; i < modelVmts.Count; i++)
                        for (int j = 0; j < modelDirs.Count; j++)
                            materials.Add("materials/" + modelDirs[j] + modelVmts[i] + ".vmt");

                // add materials found in vtx file
                for (int i = 0; i < vtxVmts.Count; i++)
                    for (int j = 0; j < modelDirs.Count; j++)
                        materials.Add($"materials/{modelDirs[j]}{vtxVmts[i]}.vmt");

                // find included models. mdl v44 and up have same includemodel format
                if (ver > 44)
                {
                    mdl.Seek(includeModelIndex, SeekOrigin.Begin);

                    var includeOffsetStart = mdl.Position;
                    for (int j = 0; j < includeModelCount; j++)
                    {
                        var includeStreamPos = mdl.Position;

                        var labelOffset = reader.ReadInt32();
                        var includeModelPathOffset = reader.ReadInt32();

                        // skip unknown section made up of 27 ints
                        // TODO: not needed?
                        //mdl.Seek(27 * 4, SeekOrigin.Current);

                        var currentOffset = mdl.Position;

                        string label = "";

                        if (labelOffset != 0)
                        {
                            // go to label offset
                            mdl.Seek(labelOffset, SeekOrigin.Begin);
                            label = ReadNullTerminatedString(mdl, reader);

                            // return to current offset
                            mdl.Seek(currentOffset, SeekOrigin.Begin);
                        }

                        if (includeModelPathOffset != 0)
                        {
                            // go to model offset
                            mdl.Seek(includeModelPathOffset + includeOffsetStart, SeekOrigin.Begin);
                            models.Add(ReadNullTerminatedString(mdl, reader));

                            // return to current offset
                            mdl.Seek(currentOffset, SeekOrigin.Begin);
                        }


                    }
                }

                // find models referenced in keyvalues
                if (keyvalueCount > 0)
                {
                    mdl.Seek(keyvalueIndex, SeekOrigin.Begin);
                    string kvString = new string(reader.ReadChars(keyvalueCount - 1));

                    // "mdlkeyvalue" and "{" are on separate lines, merge them or it doesnt parse kv name
                    int firstNewlineIndex = kvString.IndexOf("\n", StringComparison.Ordinal);
                    if (firstNewlineIndex > 0)
                        kvString = kvString.Remove(firstNewlineIndex, 1);

                    kvString = KV.StringUtil.GetFormattedKVString(kvString);

                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(kvString)))
                    {
                        KVObject kv = KVSerializer.Deserialize(stream);

                        // pack door damage models
                        var doorBlock = kv["door_options"];
                        if (doorBlock is not null)
                        {
                            var defaultsBlock = doorBlock["defaults"];
                            if (defaultsBlock is not null)
                            {
                                var damageModel1 = defaultsBlock["damage1"];
                                if (damageModel1 is not null)
                                    models.Add($"models\\{damageModel1}.mdl");

                                var damageModel2 = defaultsBlock["damage2"];
                                if (damageModel2 is not null)
                                    models.Add($"models\\{damageModel2}.mdl");

                                var damageModel3 = defaultsBlock["damage3"];
                                if (damageModel3 is not null)
                                    models.Add($"models\\{damageModel3}.mdl");
                            }
                        }
                    }
                }
            }

            for(int i=0;i<materials.Count;i++)
            {
                materials[i] = Regex.Replace(materials[i], "/+", "/"); // remove duplicate slashes
            }

            return new Tuple<List<string>, List<string>>(materials, models);
        }

        public static List<string> FindVtxMaterials(string path)
        {
            List<string> vtxMaterials = [];
            if (File.Exists(path))
            {
                try
                {
                    using FileStream vtx = new FileStream(path, FileMode.Open);
                    BinaryReader reader = new BinaryReader(vtx);

                    int version = reader.ReadInt32();

                    vtx.Seek(20, SeekOrigin.Begin);
                    int numLODs = reader.ReadInt32();

                    // contains no LODs, no reason to continue parsing
                    if (numLODs == 0)
                        return vtxMaterials;

                    int materialReplacementListOffset = reader.ReadInt32();

                    // all LOD materials stored in the materialReplacementList
                    // reading material replacement list
                    for (int i = 0; i < numLODs; i++)
                    {
                        int materialReplacementStreamPosition = materialReplacementListOffset + i * 8;
                        vtx.Seek(materialReplacementStreamPosition, SeekOrigin.Begin);
                        int numReplacements = reader.ReadInt32();
                        int replacementOffset = reader.ReadInt32();

                        if (numReplacements == 0)
                            continue;

                        vtx.Seek(materialReplacementStreamPosition + replacementOffset, SeekOrigin.Begin);
                        // reading material replacement
                        for (int j = 0; j < numReplacements; j++)
                        {
                            long streamPositionStart = vtx.Position;

                            int materialIndex = reader.ReadInt16();
                            int nameOffset = reader.ReadInt32();

                            long streamPositionEnd = vtx.Position;
                            if (nameOffset != 0)
                            {
                                vtx.Seek(streamPositionStart + nameOffset, SeekOrigin.Begin);
                                vtxMaterials.Add(ReadNullTerminatedString(vtx, reader));
                                vtx.Seek(streamPositionEnd, SeekOrigin.Begin);
                            }
                        }
                    }
                } catch
                {
                    CompilePalLogger.LogCompileError($"Failed to parse file: {path}", 
                        new Error($"Failed to parse file: {path}", ErrorSeverity.Error));
                    throw;
                }
            }

            return vtxMaterials;
        }

        public static List<string> FindPhyGibs(string path)
        {
            // finds gibs and ragdolls found in .phy files

            List<string> models = [];

            if (File.Exists(path))
            {
                using (FileStream phy = new FileStream(path, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(phy))
                    {
                        int header_size = reader.ReadInt32();
                        phy.Seek(4, SeekOrigin.Current);
                        int solidCount = reader.ReadInt32();

                        phy.Seek(header_size, SeekOrigin.Begin);
                        int solid_size = reader.ReadInt32();

                        phy.Seek(solid_size, SeekOrigin.Current);
                        string something = ReadNullTerminatedString(phy, reader);

                        // TODO: can probably use KVSerializer to parse this
                        string[] entries = something.Split(['{', '}']);
                        for (int i = 0; i < entries.Length; i++)
                        {
                            if (entries[i].Trim().Equals("break"))
                            {
                                string[] entry = entries[i + 1].Split([' '], StringSplitOptions.RemoveEmptyEntries);

                                for (int j = 0; j < entry.Length; j++)
                                    if (entry[j].Equals("\"model\"") || entry[j].Equals("\"ragdoll\""))
                                        models.Add("models\\" + entry[j + 1].Trim('"') + (entry[j + 1].Trim('"').EndsWith(".mdl") ? "" : ".mdl"));
                            }
                        }
                    }
                }
            }
            return models;
        }

        public static List<string> FindMdlRefs(string path)
        {
            // finds files associated with .mdl

            var references = new List<string>();

            var variations = new List<string> { ".dx80.vtx", ".dx90.vtx", ".phy", ".sw.vtx", ".vtx", ".xbox.vtx", ".vvd", ".ani" };
            foreach (string variation in variations)
            {
                string variant = Path.ChangeExtension(path, variation);
                references.Add(variant);
            }
            return references;
        }

        public static List<string> FindVmtTextures(string fullpath)
        {
            // finds vtfs files associated with vmt file
            var vtfList = new List<string>();
            using (var w = File.OpenRead(fullpath))
            {
                KVObject kv = KVSerializer.Deserialize(w);
                foreach (var property in kv)
                {
                    if (!Keys.vmtTextureKeyWords.Contains(property.Name))
                        continue;

                    var path = VmtPathParser(property.Value);

                    vtfList.Add($"materials/{path}.vtf");
                    if (property.Name == "$envmap")
                        vtfList.Add($"materials/{path}.hdr.vtf");
                }
            }
            return vtfList;
        }

        public static List<string> FindVmtMaterials(string fullpath)
        {
            // finds vmt files associated with vmt file
            var vmtList = new List<string>();
            using (var w = File.OpenRead(fullpath))
            {
                KVObject kv = KVSerializer.Deserialize(w);
                foreach (var property in kv)
                {
                    if (!Keys.vmtMaterialKeyWords.Contains(property.Name))
                        continue;

                    vmtList.Add($"materials/{VmtPathParser(property.Value)}.vmt");
                }
            }
            return vmtList;
        }

        public static List<string> FindResMaterials(string fullpath)
        {
            // finds vmt files associated with res file

            List<string> vmtList = [];

            using (var w = File.OpenRead(fullpath))
            {
                KVObject kv = KVSerializer.Deserialize(w, new KVSerializerOptions { FileLoader = new IncludeFileLoader() });
                foreach (var value in FindKVKey(kv, "image"))
                {
                    vmtList.Add($"materials/vgui/{VmtPathParser(value)}.vmt");
                }
            }
            return vmtList;
        }

        public static List<string> FindRadarDdsFiles(string fullpath)
        {
            // finds vmt files associated with radar overview files

            List<string> DDSs = [];

            using (var w = File.OpenRead(fullpath))
            {
                KVObject kv = KVSerializer.Deserialize(w);
                var material = kv["material"];

                // failed to get material, file contains no materials
                if (material is null)
                    return DDSs;

                string radarPath = $"resource/{VmtPathParser(material)}";
                // clean path so it never contains _radar
                if (radarPath.EndsWith("_radar"))
                {
                    radarPath = radarPath.Replace("_radar", "");
                }

                // add default radar
                DDSs.Add($"{radarPath}_radar.dds");

                // file contains no vertical sections
                if (kv["verticalsections"] is not IEnumerable<KVObject> verticalSections)
                    return DDSs;

                // add multi-level radars
                foreach (var section in verticalSections)
                {
                    DDSs.Add($"{radarPath}_{section.Name}_radar.dds");
                }
            }

            return DDSs;
        }

        public static string VmtPathParser(KVValue value)
        {
            var line = value.ToString();
            if (line is null)
            {
                CompilePalLogger.LogCompileError($"Failed to parse VMT line value: {value}",
                    new Error($"KVSerializer.Deserialize returned null: {value}", ErrorSeverity.Error));
                line = "";
            }

            line = line.Trim([' ', '/', '\\']); // removes leading slashes
            line = line.Replace('\\', '/'); // normalize slashes
            line = Regex.Replace(line, "/+", "/"); // remove duplicate slashes

            if (line.StartsWith("materials/"))
                line = line.Remove(0, "materials/".Length); // removes materials/ if its the beginning of the string for consistency
            if (line.EndsWith(".vmt") || line.EndsWith(".vtf")) // removes extentions if present for consistency
                line = line[..^4];
            return line;
        }

        public static List<string> FindSoundscapeSounds(string fullpath)
        {
            // finds audio files from soundscape file

            char [] special_caracters = ['*', '#', '@', '>', '<', '^', '(', ')', '}', '$', '!', '?', ' '];

            List<string> audioFiles = [];
            foreach (string line in File.ReadAllLines(fullpath))
            {
                string param = Regex.Replace(line, "[\t|\"]", " ").Trim();
                if (param.StartsWith("wave", StringComparison.CurrentCultureIgnoreCase))
                {
                    string clip = param.Split([' '], 2)[1].Trim(special_caracters);
                    audioFiles.Add("sound/" + clip);
                }
            }
            return audioFiles;
        }

        public static List<string> FindManifestPcfs(string fullpath)
        {
            // finds pcf files from the manifest file

            List<string> pcfs = [];
            foreach (string line in File.ReadAllLines(fullpath))
            {
                if (line.Contains("file", StringComparison.CurrentCultureIgnoreCase))
                {
                    string[] l = line.Split('"');
                    pcfs.Add(l[^2].TrimStart('!'));
                }
            }
            return pcfs;
        }

        public static void FindBspPakDependencies(BSP bsp, string tempdir)
        {
            // Search the temp folder to find dependencies of files extracted from the pak file
            if (Directory.Exists(tempdir))
	            foreach (string file in Directory.EnumerateFiles(tempdir, "*.vmt", SearchOption.AllDirectories))
	            {
                    foreach (string material in AssetUtils.FindVmtMaterials(new FileInfo(file).FullName))
                        bsp.TextureList.Add(material);
				}
        }

        /// <summary>
        /// Finds referenced vscripts
        /// Currently does not support multiline comments
        /// </summary>
        /// <param name="fullpath">Full path to VScript file</param>
        /// <returns>List of VSript references</returns>
        private static readonly string[] vscriptFunctions = ["IncludeScript", "DoIncludeScript", "PrecacheSound", "PrecacheModel"];
        private static readonly string[] vscriptHints = ["!CompilePal::IncludeFile", "!CompilePal::IncludeDirectory"];
        public static (List<string>, List<string>, List<string>, List<string>, List<string>) FindVScriptDependencies(string fullpath)
        {
            var script = File.ReadAllLines(fullpath);
            var commentRegex = new Regex(@"^\/\/");
            var functionParametersRegex = new Regex("\\((.*?)\\)");

            List<string> includedScripts = [];
            List<string> includedModels = [];
            List<string> includedSounds = [];
            List<string> includedFiles = [];
            List<string> includedDirectories = [];

            // currently only squirrel parsing is supported
            foreach (var line in script)
            {
                // statements can also be separated with semicolons
                var statements = line.Split(";").Where(s => !string.IsNullOrWhiteSpace(s));
                foreach (var statement in statements)
                {
                    var cleanStatement = statement;

                    // ignore comments, except for packing hints
                    if (commentRegex.IsMatch(statement)) {
                        cleanStatement = commentRegex.Replace(statement, "");

                        if (!vscriptHints.Any(func => cleanStatement.Contains(func))) {
                            continue;
                        }
                    } else if (!vscriptFunctions.Any(func => cleanStatement.Contains(func))) {
                        continue;
                    }

                    Match m = functionParametersRegex.Match(cleanStatement);
                    if (!m.Success) {
                        CompilePalLogger.LogLineDebug($"Failed to parse function arguments {cleanStatement} in file: {fullpath}");
                        continue;
                    }

                    // capture group 0 is always full match, 1 is capture
                    var functionParameters = m.Groups[1].Value.Split(",");

                    // pack imported VScripts
                    if (cleanStatement.Contains("IncludeScript") || cleanStatement.Contains("DoIncludeScript")) {
                        // only want 1st param (filename)
                        includedScripts.Add(Path.Combine("scripts", "vscripts", functionParameters[0].Replace("\"", "").Trim()));
                    } else if (cleanStatement.Contains("PrecacheModel")) {
                        // pack precached models
                        includedModels.Add(functionParameters[0].Replace("\"", "").Trim());
                    } else if (cleanStatement.Contains("PrecacheSound")) {
                        // pack precached sounds
                        includedSounds.Add(Path.Combine("sound", functionParameters[0].Replace("\"", "").Trim()));                    
                    } else if (cleanStatement.Contains("!CompilePal::IncludeFile")) {
                        // pack file hints
                        includedFiles.Add(Path.Combine(functionParameters[0].Replace("\"", "").Trim()));                    
                    } else if (cleanStatement.Contains("!CompilePal::IncludeDirectory")) {
                        // pack directory hints
                        includedDirectories.Add(Path.Combine(functionParameters[0].Replace("\"", "").Trim()));                    
                    }
                }
            }

            return (includedScripts, includedModels, includedSounds, includedFiles, includedDirectories);

        }

        public static void FindBspUtilityFiles(BSP bsp, List<string> sourceDirectories, bool renamenav, bool genparticlemanifest)
        {
            // Utility files are other files that are not assets and are sometimes not referenced in the bsp
            // those are manifests, soundscapes, nav, radar and detail files
            
            // Soundscape file
            string internalPath = "scripts/soundscapes_" + bsp.file.Name.Replace(".bsp", ".txt");
            // Soundscapes can have either .txt or .vsc extensions
            string internalPathVsc = "scripts/soundscapes_" + bsp.file.Name.Replace(".bsp", ".vsc");
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;
                string externalVscPath = source + "/" + internalPathVsc;

                if (File.Exists(externalPath))
                {
                    bsp.soundscape = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                } 
                if (File.Exists(externalVscPath))
                {
                    bsp.soundscape = new KeyValuePair<string, string>(internalPathVsc, externalVscPath);
                    break;
                }
            }

            // Soundscript file
            internalPath = "maps/" + bsp.file.Name.Replace(".bsp", "") + "_level_sounds.txt";
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.soundscript = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                }
            }

            // Nav file (.nav)
            internalPath = "maps/" + bsp.file.Name.Replace(".bsp", ".nav");
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    if (renamenav)
                        internalPath = "maps/embed.nav";
                    bsp.nav = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                }
            }

            // detail file (.vbsp)
            Dictionary<string, string> worldspawn = bsp.entityList.FirstOrDefault(item => item["classname"] == "worldspawn", []);
            if (worldspawn.TryGetValue("detailvbsp", out var detailvbsp))
            {
                internalPath = detailvbsp;

                foreach (string source in sourceDirectories)
                {
                    string externalPath = source + "/" + internalPath;

                    if (File.Exists(externalPath))
                    {
                        bsp.detail = new KeyValuePair<string, string>(internalPath, externalPath);
                        break;
                    }
                }
            }


            // Vehicle scripts
            List<KeyValuePair<string, string>> vehicleScripts = [];
            foreach (Dictionary<string, string> ent in bsp.entityList)
            {
                if (ent.TryGetValue("vehiclescript", out var vehiclescript))
                {
                    foreach (string source in sourceDirectories)
                    {
                        string externalPath = source + "/" + vehiclescript;
                        if (File.Exists(externalPath))
                        {
                            vehicleScripts.Add(new KeyValuePair<string, string>(vehiclescript, externalPath));
                            break;
                        }
                    }
                }
            }
            bsp.VehicleScriptList = vehicleScripts;

			// Effect Scripts
			List<KeyValuePair<string, string>> effectScripts = [];
			foreach (Dictionary<string, string> ent in bsp.entityList)
			{
				if (ent.TryGetValue("scriptfile", out var scriptfile))
				{
					foreach (string source in sourceDirectories)
					{
						string externalPath = source + "/" + scriptfile;
						if (File.Exists(externalPath))
						{
							effectScripts.Add(new KeyValuePair<string, string>(scriptfile, externalPath));
                            break;
						}
					}
				}
			}
			bsp.EffectScriptList = effectScripts;

            // Res files (for tf2's pd gamemode)
            foreach (Dictionary<string, string> ent in bsp.entityList)
            {
                if (ent.TryGetValue("res_file", out var resFile))
                {
                    foreach (string source in sourceDirectories)
                    {
                        string externalPath = source + "/" + resFile;
                        if (File.Exists(externalPath))
                        {
                            bsp.res.Add(new KeyValuePair<string, string>(resFile, externalPath));
                            break;
                        }
                    }
                }
            }

            // tf2 tc round overview files
            internalPath = "resource/roundinfo/" + bsp.file.Name.Replace(".bsp", ".res");
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.res.Add(new KeyValuePair<string, string>(internalPath, externalPath));
                    break;
                }
            }
            internalPath = "materials/overviews/" + bsp.file.Name.Replace(".bsp", ".vmt");
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.TextureList.Add(internalPath);
                    break;
                }
            }

            // Radar file
            internalPath = "resource/overviews/" + bsp.file.Name.Replace(".bsp", ".txt");
            List<KeyValuePair<string, string>> ddsfiles = [];
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.radartxt = new KeyValuePair<string, string>(internalPath, externalPath);
                    bsp.TextureList.AddRange(FindVmtMaterials(externalPath));

                    List<string> ddsInternalPaths = FindRadarDdsFiles(externalPath);
                    //find out if they exists or not
                    foreach (string ddsInternalPath in ddsInternalPaths)
                    {
                        foreach (string source2 in sourceDirectories)
                        {
                            string ddsExternalPath = source2 + "/" + ddsInternalPath;
                            if (File.Exists(ddsExternalPath))
                            {
                                ddsfiles.Add(new KeyValuePair<string, string>(ddsInternalPath, ddsExternalPath));
                                break;
                            }
                        }
                    }
                    break;
                }
            }
            bsp.radardds = ddsfiles;

            // csgo kv file (.kv)
            internalPath = "maps/" + bsp.file.Name.Replace(".bsp", ".kv");
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.kv = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                }
            }

            // csgo loading screen text file (.txt)
            internalPath = "maps/" + bsp.file.Name.Replace(".bsp", ".txt");
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.txt = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                }
            }

            // csgo loading screen image (.jpg)
            internalPath = "maps/" + bsp.file.Name.Replace(".bsp", "");
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;
                foreach (string extension in new[] {".jpg", ".jpeg"})
                    if (File.Exists(externalPath + extension))
                        bsp.jpg = new KeyValuePair<string, string>(internalPath + ".jpg", externalPath + extension);
            }

            // csgo panorama map backgrounds (.png)
            internalPath = "materials/panorama/images/map_icons/screenshots/"; 
            var panoramaMapBackgrounds = new List<KeyValuePair<string, string>>();
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;
                string bspName = bsp.file.Name.Replace(".bsp", "");

                foreach (string resolution in new[] {"360p", "1080p"})
                    if (File.Exists($"{externalPath}{resolution}/{bspName}.png"))
                        panoramaMapBackgrounds.Add(new KeyValuePair<string, string>($"{internalPath}{resolution}/{bspName}.png", $"{externalPath}{resolution}/{bspName}.png"));
            }
            bsp.PanoramaMapBackgrounds = panoramaMapBackgrounds;

            // csgo panorama map icon
            internalPath = "materials/panorama/images/map_icons/"; 
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;
                string bspName = bsp.file.Name.Replace(".bsp", "");
                foreach (string extension in new[] {".svg"})
                    if (File.Exists($"{externalPath }map_icon_{bspName}{extension}"))
                        bsp.PanoramaMapIcon = new KeyValuePair<string, string>($"{internalPath}map_icon_{bspName}{extension}", $"{externalPath}map_icon_{bspName}{extension}");
            }

            // csgo dz tablets
            internalPath = "materials/models/weapons/v_models/tablet/tablet_radar_" + bsp.file.Name.Replace(".bsp", ".vtf");
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.RadarTablet = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                }
            }

            // language files, particle manifests and soundscript file
            // (these language files are localized text files for tf2 mission briefings)
            string internalDir = "maps/";
            string name = bsp.file.Name.Replace(".bsp", "");
            string searchPattern = name + "*.txt";
            List<KeyValuePair<string, string>> langfiles = [];

            foreach (string source in sourceDirectories)
            {
                string externalDir = source + "/" + internalDir;
                DirectoryInfo dir = new DirectoryInfo(externalDir);

                if (dir.Exists)
                    foreach (FileInfo f in dir.GetFiles(searchPattern))
                    {
                        // particle files if particle manifest is not being generated
                        if (f.Name.StartsWith(name + "_particles") || f.Name.StartsWith(name + "_manifest"))
                        {
                            if(!genparticlemanifest)
                                bsp.particleManifest = new KeyValuePair<string, string>(internalDir + f.Name, externalDir + f.Name);
                            continue;
                        }
                        // soundscript
                        if (f.Name.StartsWith(name + "_level_sounds"))
                            bsp.soundscript =
                                new KeyValuePair<string, string>(internalDir + f.Name, externalDir + f.Name);
                        // presumably language files
                        else
                            langfiles.Add(new KeyValuePair<string, string>(internalDir + f.Name, externalDir + f.Name));
                    }
            }
            bsp.languages = langfiles;

            // ASW/Source2009 branch VScripts
            List<string> vscripts = [];

            foreach(Dictionary<string, string> entity in bsp.entityList)
            {
                foreach(KeyValuePair<string,string> kvp in entity)
                {
                    if(kvp.Key.Equals("vscripts", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string[] scripts = kvp.Value.Split(' ');
                        foreach(string script in scripts)
                        {
                            vscripts.Add("scripts/vscripts/" + script);
                        }
                    }
                }
            }
            bsp.vscriptList = vscripts.Distinct().ToList();
        }

        private static string ReadNullTerminatedString(FileStream fs, BinaryReader reader)
        {
            List<byte> verString = [];
            byte v;
            do
            {
                v = reader.ReadByte();
                verString.Add(v);
            } while (v != '\0' && fs.Position != fs.Length);

            return Encoding.ASCII.GetString(verString.ToArray()).Trim('\0');
        }

        /// <summary>
        /// Recursively searches KV for specified key
        /// </summary>
        /// <param name="kv"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static List<KVValue> FindKVKey(KVObject kv, string key)
        {
            var values = new List<KVValue>();

            foreach (var property in kv)
            {
                if (key == property.Name)
                    values.Add(property.Value);


                // recursively search KV
                if (property is KVObject)
                    values.AddRange(FindKVKey(property, key));
            }

            return values;
        }
    }

    public class IncludeFileLoader : IIncludedFileLoader
    {
        public Stream OpenFile(string filePath)
        {
            if (File.Exists(filePath))
                return File.OpenRead(filePath);

            // if file is not found return empty KV file so it doesnt crash
            return new MemoryStream(Encoding.UTF8.GetBytes("\"\"{}"));
        }
    }
}
