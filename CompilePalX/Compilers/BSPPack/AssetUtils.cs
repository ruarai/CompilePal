using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CompilePalX;

namespace CompilePalX.Compilers.BSPPack
{
    static class AssetUtils
    {

        public static Tuple<List<string>, List<string>> findMdlMaterialsAndModels(string path, List<int> skins = null, List<string> vtxVmts = null)
        {
            List<string> materials = new List<string>();
            List<string> models = new List<string>();

            if (File.Exists(path))
            {

                FileStream mdl = new FileStream(path, FileMode.Open);
                BinaryReader reader = new BinaryReader(mdl);

                mdl.Seek(4, SeekOrigin.Begin);
                int ver = reader.ReadInt32();

                List<string> modelVmts = new List<string>();
                List<string> modelDirs = new List<string>();

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
                    modelVmts.Add(readNullTerminatedString(mdl, reader));
                }

                // find model dirs
                List<int> textureDirOffsets = new List<int>();
                for (int i = 0; i < textureDirCount; i++)
                {
                    mdl.Seek(textureDirOffset + (4 * i), SeekOrigin.Begin);
                    int offset = reader.ReadInt32();
                    mdl.Seek(offset, SeekOrigin.Begin);

                    string model = readNullTerminatedString(mdl, reader);
                    model = model.TrimStart(new char[] { '/', '\\' });
                    modelDirs.Add(model);
                }

                if (skins != null)
                {
                    // load specific skins
                    List<int> material_ids = new List<int>();

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
                                mdl.Seek( modelFileInputOffset + meshindex + (k * 116), SeekOrigin.Begin);
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
                            label = readNullTerminatedString(mdl, reader);

                            // return to current offset
                            mdl.Seek(currentOffset, SeekOrigin.Begin);
                        }

                        if (includeModelPathOffset != 0)
                        {
                            // go to model offset
                            mdl.Seek(includeModelPathOffset + includeOffsetStart, SeekOrigin.Begin);
                            models.Add(readNullTerminatedString(mdl, reader));

                            // return to current offset
                            mdl.Seek(currentOffset, SeekOrigin.Begin);
                        }


                    }
                }

                // find models referenced in keyvalues
                if (keyvalueCount > 0)
                {
                    mdl.Seek(keyvalueIndex, SeekOrigin.Begin);
                    string kv = new string(reader.ReadChars(keyvalueCount - 1));

                    // "mdlkeyvalue" and "{" are on separate lines, merge them or it doesnt parse kv name
                    int firstNewlineIndex = kv.IndexOf("\n", StringComparison.Ordinal);
                    if (firstNewlineIndex > 0)
                        kv = kv.Remove(firstNewlineIndex, 1);

                    kv = KV.StringUtil.GetFormattedKVString(kv);
                    var data = KV.DataBlock.FromString(kv);

                    var mdlKvBlock = data.GetFirstByName("mdlkeyvalue");
                    var doorDefaultsBlock = mdlKvBlock?.GetFirstByName("door_options")?.GetFirstByName("\"defaults\"");
                    if (doorDefaultsBlock != null)
                    {
                        var damageModel1 = doorDefaultsBlock.TryGetStringValue("damage1");
                        if (damageModel1 != "")
                            models.Add($"models\\{damageModel1}.mdl");
                        var damageModel2 = doorDefaultsBlock.TryGetStringValue("damage2");
                        if (damageModel2 != "")
                            models.Add($"models\\{damageModel2}.mdl");
                    }
                }


                mdl.Close();
            }

            for(int i=0;i<materials.Count;i++)
            {
                materials[i] = Regex.Replace(materials[i], "/+", "/"); // remove duplicate slashes
            }

            return new Tuple<List<string>, List<string>>(materials, models);
        }

        public static List<string> FindVtxMaterials(string path)
        {
            List<string> vtxMaterials = new List<string>();
            if (File.Exists(path))
            {
                using (FileStream vtx = new FileStream(path, FileMode.Open))
                {
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
                                vtxMaterials.Add(readNullTerminatedString(vtx, reader));
                                vtx.Seek(streamPositionEnd, SeekOrigin.Begin);
                            }
                        }
                    }
                }
            }

            return vtxMaterials;
        }

        public static List<string> findPhyGibs(string path)
        {
            // finds gibs and ragdolls found in .phy files

            List<string> models = new List<string>();

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
                        string something = readNullTerminatedString(phy, reader);

                        string[] entries = something.Split(new char[] { '{', '}' });
                        for (int i = 0; i < entries.Count(); i++)
                        {
                            if (entries[i].Trim().Equals("break"))
                            {
                                string[] entry = entries[i + 1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                for (int j = 0; j < entry.Count(); j++)
                                    if (entry[j].Equals("\"model\"") || entry[j].Equals("\"ragdoll\""))
                                        models.Add("models\\" + entry[j + 1].Trim('"') + (entry[j + 1].Trim('"').EndsWith(".mdl") ? "" : ".mdl"));
                            }
                        }
                    }
                }
            }
            return models;
        }

        public static List<string> findMdlRefs(string path)
        {
            // finds files associated with .mdl

            var references = new List<string>();

            var variations = new List<string> { ".dx80.vtx", ".dx90.vtx", ".phy", ".sw.vtx", ".vtx", ".xbox.vtx", ".vvd", ".ani" };
            foreach (string variation in variations)
            {
                string variant = Path.ChangeExtension(path, variation);
                //variant = variant.Replace('/', '\\');
                references.Add(variant);
            }
            return references;
        }

        public static List<string> findVmtTextures(string fullpath)
        {
            // finds vtfs files associated with vmt file

            List<string> vtfList = new List<string>();
            foreach (string line in File.ReadAllLines(fullpath))
            {
                string param = line.Replace("\"", " ").Replace("\t", " ").Trim();

                if (Keys.vmtTextureKeyWords.Any(key => param.ToLower().StartsWith(key + " ")))
                {
                    vtfList.Add("materials/" + vmtPathParser2(line) + ".vtf");
                    if (param.ToLower().StartsWith("$envmap" + " "))
                        vtfList.Add("materials/" + vmtPathParser2(line) + ".hdr.vtf");
                }
            }
            return vtfList;
        }

        public static List<string> findVmtMaterials(string fullpath)
        {
            // finds vmt files associated with vmt file

            List<string> vmtList = new List<string>();
            foreach (string line in File.ReadAllLines(fullpath))
            {
                string param = line.Replace("\"", " ").Replace("\t", " ").Trim();
                if (Keys.vmtMaterialKeyWords.Any(key => param.StartsWith(key + " ")))
                {
                    vmtList.Add("materials/" + vmtPathParser2(line) + ".vmt");
                }
            }
            return vmtList;
        }

        public static List<string> findResMaterials(string fullpath)
        {
            // finds vmt files associated with res file

            List<string> vmtList = new List<string>();
            foreach (string line in File.ReadAllLines(fullpath))
            {
                string param = line.Replace("\"", " ").Replace("\t", " ").Trim();
                if (param.StartsWith("image ", StringComparison.CurrentCultureIgnoreCase))
                {
                    string path = "materials/vgui/" + vmtPathParser2(line) + ".vmt";
                    path = path.Replace("/vgui/..", "");
                    vmtList.Add(path);
                }
            }
            return vmtList;
        }

        public static List<string> findRadarDdsFiles(string fullpath)
        {
            // finds vmt files associated with radar overview files

            List<string> DDSs = new List<string>();
            var overviewFile = new KV.FileData(fullpath);

            // Contains no blocks, return empty list
            if (overviewFile.headnode.subBlocks.Count == 0)
                return DDSs;

            foreach (var subblock in overviewFile.headnode.subBlocks)
            {
                var material = subblock.TryGetStringValue("material");
                // failed to get material, file contains no materials
                if (material == "")
                    break;

                // add default radar
                DDSs.Add($"resource/{vmtPathParser(material, false)}_radar.dds");

                var verticalSections = subblock.GetFirstByName("\"verticalsections\"");
                if (verticalSections == null)
                    break;
                
                // add multi-level radars
                foreach (var section in verticalSections.subBlocks)
                    DDSs.Add($"resource/{vmtPathParser(material, false)}_{section.name.Replace("\"", string.Empty)}_radar.dds");
            }

            return DDSs;
        }

        public static string vmtPathParser(string vmtline, bool needsSplit = true)
        {
            if (needsSplit)
                vmtline = vmtline.Split(new char[] { ' ' }, 2)[1]; // removes the parameter name
            vmtline = vmtline.Split(new string[] { "//", "\\\\" }, StringSplitOptions.None)[0]; // removes endline parameter
            vmtline = vmtline.Trim(new char[] { ' ', '/', '\\' }); // removes leading slashes
            vmtline = vmtline.Replace('\\', '/'); // normalize slashes
            if (vmtline.StartsWith("materials/"))
                vmtline = vmtline.Remove(0, "materials/".Length); // removes materials/ if its the beginning of the string for consistency
            if (vmtline.EndsWith(".vmt") || vmtline.EndsWith(".vtf")) // removes extentions if present for consistency
                vmtline = vmtline.Substring(0, vmtline.Length - 4);
            return vmtline;
        }

        // same as above but quotes are not replaced and line does not need to be trimmed, quotes are needed to tell if // are comments or not
        public static string vmtPathParser2(string vmtline)
        {
            vmtline = vmtline.Trim(new char[] {' ', '\t'});
            
            // remove key
            if(vmtline[0] == '"')
            {
                vmtline = Regex.Match(vmtline, "\"[^\"]+\"(.*)$").Groups[1].Value;
            }
            else
            {
                vmtline = Regex.Match(vmtline, "[^ \t]+(.*)$").Groups[1].Value;
            }

            vmtline = vmtline.TrimStart(new char[] { ' ', '\t' });
            // process value
            if (vmtline[0] == '"')
            {
                vmtline = Regex.Match(vmtline, "\"([^\"]+)\"").Groups[1].Value;
            }
            else
            {
                // strip c style comments like this one
                var commentIndex = vmtline.IndexOf("//");
                if(commentIndex > -1)
                {
                    vmtline = vmtline.Substring(0, commentIndex);
                }
                vmtline = Regex.Match(vmtline, "[^ \t]+").Groups[0].Value;
            }

            vmtline = vmtline.Trim(new char[] { ' ', '/', '\\' }); // removes leading slashes
            vmtline = vmtline.Replace('\\', '/'); // normalize slashes
            vmtline = Regex.Replace(vmtline, "/+", "/"); // remove duplicate slashes

            if (vmtline.StartsWith("materials/"))
                vmtline = vmtline.Remove(0, "materials/".Length); // removes materials/ if its the beginning of the string for consistency
            if (vmtline.EndsWith(".vmt") || vmtline.EndsWith(".vtf")) // removes extentions if present for consistency
                vmtline = vmtline.Substring(0, vmtline.Length - 4);
            return vmtline;
        }

        public static List<string> findSoundscapeSounds(string fullpath)
        {
            // finds audio files from soundscape file

            char [] special_caracters = new char[] {'*', '#', '@', '>', '<', '^', '(', ')', '}', '$', '!', '?', ' '};

            List<string> audioFiles = new List<string>();
            foreach (string line in File.ReadAllLines(fullpath))
            {
                string param = Regex.Replace(line, "[\t|\"]", " ").Trim();
                if (param.ToLower().StartsWith("wave"))
                {
                    string clip = param.Split(new char[] { ' ' }, 2)[1].Trim(special_caracters);
                    audioFiles.Add("sound/" + clip);
                }
            }
            return audioFiles;
        }

        public static List<string> findManifestPcfs(string fullpath)
        {
            // finds pcf files from the manifest file

            List<string> pcfs = new List<string>();
            foreach (string line in File.ReadAllLines(fullpath))
            {
                if (line.ToLower().Contains("file"))
                {
                    string[] l = line.Split('"');
                    pcfs.Add(l[l.Count() - 2].TrimStart('!'));
                }
            }
            return pcfs;
        }

        public static void findBspPakDependencies(BSP bsp, string tempdir)
        {
            // Search the temp folder to find dependencies of files extracted from the pak file
            if (Directory.Exists(tempdir))
	            foreach (String file in Directory.EnumerateFiles(tempdir, "*.vmt", SearchOption.AllDirectories))
	            {
                    foreach (string material in AssetUtils.findVmtMaterials(new FileInfo(file).FullName))
                        bsp.TextureList.Add(material);

					foreach (string material in AssetUtils.findVmtTextures(new FileInfo(file).FullName))
						bsp.TextureList.Add(material);
				}

        }

        public static void findBspUtilityFiles(BSP bsp, List<string> sourceDirectories, bool renamenav, bool genparticlemanifest)
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
            Dictionary<string, string> worldspawn = bsp.entityList.First(item => item["classname"] == "worldspawn");
            if (worldspawn.ContainsKey("detailvbsp"))
            {
                internalPath = worldspawn["detailvbsp"];

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
            List<KeyValuePair<string, string>> vehicleScripts = new List<KeyValuePair<string, string>>();
            foreach (Dictionary<string, string> ent in bsp.entityList)
            {
                if (ent.ContainsKey("vehiclescript"))
                {
                    foreach (string source in sourceDirectories)
                    {
                        string externalPath = source + "/" + ent["vehiclescript"];
                        if (File.Exists(externalPath))
                        {
                            internalPath = ent["vehiclescript"];
                            vehicleScripts.Add(new KeyValuePair<string, string>(ent["vehiclescript"], externalPath));
                        }
                    }
                }
            }
            bsp.VehicleScriptList = vehicleScripts;

			// Effect Scripts
			List<KeyValuePair<string, string>> effectScripts = new List<KeyValuePair<string, string>>();
			foreach (Dictionary<string, string> ent in bsp.entityList)
			{
				if (ent.ContainsKey("scriptfile"))
				{
					foreach (string source in sourceDirectories)
					{
						string externalPath = source + "/" + ent["scriptfile"];
						if (File.Exists(externalPath))
						{
							internalPath = ent["scriptfile"];
							effectScripts.Add(new KeyValuePair<string, string>(ent["scriptfile"], externalPath));
						}
					}
				}
			}
			bsp.EffectScriptList = effectScripts;

			// Res file (for tf2's pd gamemode)
			Dictionary<string, string>  pd_ent = bsp.entityList.FirstOrDefault(item => item["classname"] == "tf_logic_player_destruction");
            if (pd_ent != null && pd_ent.ContainsKey("res_file"))
            {
                foreach (string source in sourceDirectories)
                {
                    string externalPath = source + "/" + pd_ent["res_file"];
                    if (File.Exists(externalPath))
                    {
                        bsp.res = new KeyValuePair<string, string>(pd_ent["res_file"], externalPath);
                        break;
                    }
                }
            }

            // Radar file
            internalPath = "resource/overviews/" + bsp.file.Name.Replace(".bsp", ".txt");
            List<KeyValuePair<string, string>> ddsfiles = new List<KeyValuePair<string, string>>();
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.radartxt = new KeyValuePair<string, string>(internalPath, externalPath);
                    bsp.TextureList.AddRange(findVmtMaterials(externalPath));

                    List<string> ddsInternalPaths = findRadarDdsFiles(externalPath);
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

                foreach (string extension in new String[] {".jpg", ".jpeg"})
                    if (File.Exists(externalPath + extension))
                        bsp.jpg = new KeyValuePair<string, string>(internalPath + ".jpg", externalPath + extension);
            }

            // csgo panorama map icons (.png)
            internalPath = "materials/panorama/images/map_icons/screenshots/"; 
            var panoramaMapIcons = new List<KeyValuePair<string, string>>();
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;
                string bspName = bsp.file.Name.Replace(".bsp", "");

                foreach (string resolution in new[] {"360p", "1080p"})
                    if (File.Exists($"{externalPath}{resolution}/{bspName}.png"))
                        panoramaMapIcons.Add(new KeyValuePair<string, string>($"{internalPath}{resolution}/{bspName}.png", $"{externalPath}{resolution}/{bspName}.png"));
            }
            bsp.PanoramaMapIcons = panoramaMapIcons;

            // language files, particle manifests and soundscript file
            // (these language files are localized text files for tf2 mission briefings)
            string internalDir = "maps/";
            string name = bsp.file.Name.Replace(".bsp", "");
            string searchPattern = name + "*.txt";
            List<KeyValuePair<string, string>> langfiles = new List<KeyValuePair<string, string>>();

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
            List<string> vscripts = new List<string>();

            foreach(Dictionary<string, string> entity in bsp.entityList)
            {
                foreach(KeyValuePair<string,string> kvp in entity)
                {
                    if(kvp.Key.ToLower() == "vscripts")
                    {
                        string[] scripts = kvp.Value.Split(' ');
                        foreach(string script in scripts)
                        {
                            vscripts.Add("scripts/vscripts/" + script);
                        }
                    }
                }
            }
            bsp.vscriptList = vscripts;
        }

        private static string readNullTerminatedString(FileStream fs, BinaryReader reader)
        {
            List<byte> verString = new List<byte>();
            byte v;
            do
            {
                v = reader.ReadByte();
                verString.Add(v);
            } while (v != '\0' && fs.Position != fs.Length);

            return Encoding.ASCII.GetString(verString.ToArray()).Trim('\0');
        }
    }
}
