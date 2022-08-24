using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CompilePalX.KV;

namespace CompilePalX.Compilers.BSPPack
{
    static class AssetUtils
    {

        public static Tuple<List<string>, List<string>> findMdlMaterialsAndModels(string path, List<int> skins = null, List<string> vtxVmts = null)
        {
            var materials = new List<string>();
            var models = new List<string>();

            if (File.Exists(path))
            {

                var mdl = new FileStream(path, FileMode.Open);
                var reader = new BinaryReader(mdl);

                mdl.Seek(4, SeekOrigin.Begin);
                var ver = reader.ReadInt32();

                var modelVmts = new List<string>();
                var modelDirs = new List<string>();

                mdl.Seek(76, SeekOrigin.Begin);
                var datalength = reader.ReadInt32();
                mdl.Seek(124, SeekOrigin.Current);

                var textureCount = reader.ReadInt32();
                var textureOffset = reader.ReadInt32();

                var textureDirCount = reader.ReadInt32();
                var textureDirOffset = reader.ReadInt32();

                var skinreferenceCount = reader.ReadInt32();
                var skinrfamilyCount = reader.ReadInt32();
                var skinreferenceIndex = reader.ReadInt32();

                var bodypartCount = reader.ReadInt32();
                var bodypartIndex = reader.ReadInt32();

                // skip to keyvalues
                mdl.Seek(72, SeekOrigin.Current);
                var keyvalueIndex = reader.ReadInt32();
                var keyvalueCount = reader.ReadInt32();

                // skip to includemodel
                mdl.Seek(16, SeekOrigin.Current);
                //mdl.Seek(96, SeekOrigin.Current);
                var includeModelCount = reader.ReadInt32();
                var includeModelIndex = reader.ReadInt32();

                // find model names
                for (var i = 0; i < textureCount; i++)
                {
                    mdl.Seek(textureOffset + i * 64, SeekOrigin.Begin);
                    var textureNameOffset = reader.ReadInt32();

                    mdl.Seek(textureOffset + i * 64 + textureNameOffset, SeekOrigin.Begin);
                    modelVmts.Add(readNullTerminatedString(mdl, reader));
                }

                // find model dirs
                var textureDirOffsets = new List<int>();
                for (var i = 0; i < textureDirCount; i++)
                {
                    mdl.Seek(textureDirOffset + 4 * i, SeekOrigin.Begin);
                    var offset = reader.ReadInt32();
                    mdl.Seek(offset, SeekOrigin.Begin);

                    var model = readNullTerminatedString(mdl, reader);
                    model = model.TrimStart('/', '\\');
                    modelDirs.Add(model);
                }

                if (skins != null)
                {
                    // load specific skins
                    var material_ids = new List<int>();

                    for (var i = 0; i < bodypartCount; i++)
                        // we are reading an array of mstudiobodyparts_t
                    {
                        mdl.Seek(bodypartIndex + i * 16, SeekOrigin.Begin);

                        mdl.Seek(4, SeekOrigin.Current);
                        var nummodels = reader.ReadInt32();
                        mdl.Seek(4, SeekOrigin.Current);
                        var modelindex = reader.ReadInt32();

                        for (var j = 0; j < nummodels; j++)
                            // we are reading an array of mstudiomodel_t
                        {
                            mdl.Seek(bodypartIndex + i * 16 + modelindex + j * 148, SeekOrigin.Begin);
                            var modelFileInputOffset = mdl.Position;

                            mdl.Seek(72, SeekOrigin.Current);
                            var nummeshes = reader.ReadInt32();
                            var meshindex = reader.ReadInt32();

                            for (var k = 0; k < nummeshes; k++)
                                // we are reading an array of mstudiomesh_t
                            {
                                mdl.Seek(modelFileInputOffset + meshindex + k * 116, SeekOrigin.Begin);
                                var mat_index = reader.ReadInt32();

                                if (!material_ids.Contains(mat_index))
                                {
                                    material_ids.Add(mat_index);
                                }
                            }
                        }
                    }

                    // read the skintable
                    mdl.Seek(skinreferenceIndex, SeekOrigin.Begin);
                    var skintable = new short[skinrfamilyCount, skinreferenceCount];
                    for (var i = 0; i < skinrfamilyCount; i++)
                    {
                        for (var j = 0; j < skinreferenceCount; j++)
                        {
                            skintable[i, j] = reader.ReadInt16();
                        }
                    }

                    // trim the larger than required skintable
                    var trimmedtable = new short[skinrfamilyCount, material_ids.Count];
                    for (var i = 0; i < skinrfamilyCount; i++)
                    {
                        for (var j = 0; j < material_ids.Count; j++)
                        {
                            trimmedtable[i, j] = skintable[i, material_ids[j]];
                        }
                    }

                    // add default skin 0 in case of non-existing skin indexes
                    if (skins.IndexOf(0) == -1 && skins.Count(s => s >= trimmedtable.GetLength(0)) != 0)
                    {
                        skins.Add(0);
                    }

                    // use the trimmed table to fetch used vmts
                    foreach (var skin in skins.Where(skin => skin < trimmedtable.GetLength(0)))
                    {
                        for (var j = 0; j < material_ids.Count; j++)
                        {
                            for (var k = 0; k < modelDirs.Count; k++)
                            {
                                var id = trimmedtable[skin, j];
                                materials.Add("materials/" + modelDirs[k] + modelVmts[id] + ".vmt");
                            }
                        }
                    }
                }
                else
                    // load all vmts
                {
                    for (var i = 0; i < modelVmts.Count; i++)
                    {
                        for (var j = 0; j < modelDirs.Count; j++)
                        {
                            materials.Add("materials/" + modelDirs[j] + modelVmts[i] + ".vmt");
                        }
                    }
                }

                // add materials found in vtx file
                for (var i = 0; i < vtxVmts.Count; i++)
                {
                    for (var j = 0; j < modelDirs.Count; j++)
                    {
                        materials.Add($"materials/{modelDirs[j]}{vtxVmts[i]}.vmt");
                    }
                }

                // find included models. mdl v44 and up have same includemodel format
                if (ver > 44)
                {
                    mdl.Seek(includeModelIndex, SeekOrigin.Begin);

                    var includeOffsetStart = mdl.Position;
                    for (var j = 0; j < includeModelCount; j++)
                    {
                        var includeStreamPos = mdl.Position;

                        var labelOffset = reader.ReadInt32();
                        var includeModelPathOffset = reader.ReadInt32();

                        // skip unknown section made up of 27 ints
                        // TODO: not needed?
                        //mdl.Seek(27 * 4, SeekOrigin.Current);

                        var currentOffset = mdl.Position;

                        var label = "";

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
                    var kv = new string(reader.ReadChars(keyvalueCount - 1));

                    // "mdlkeyvalue" and "{" are on separate lines, merge them or it doesnt parse kv name
                    var firstNewlineIndex = kv.IndexOf("\n", StringComparison.Ordinal);
                    if (firstNewlineIndex > 0)
                    {
                        kv = kv.Remove(firstNewlineIndex, 1);
                    }

                    kv = StringUtil.GetFormattedKVString(kv);
                    var data = DataBlock.FromString(kv);

                    var mdlKvBlock = data.GetFirstByName("mdlkeyvalue");
                    var doorDefaultsBlock = mdlKvBlock?.GetFirstByName("door_options")?.GetFirstByName("\"defaults\"");
                    if (doorDefaultsBlock != null)
                    {
                        var damageModel1 = doorDefaultsBlock.TryGetStringValue("damage1");
                        if (damageModel1 != "")
                        {
                            models.Add($"models\\{damageModel1}.mdl");
                        }
                        var damageModel2 = doorDefaultsBlock.TryGetStringValue("damage2");
                        if (damageModel2 != "")
                        {
                            models.Add($"models\\{damageModel2}.mdl");
                        }
                    }
                }


                mdl.Close();
            }

            for (var i = 0; i < materials.Count; i++)
            {
                materials[i] = Regex.Replace(materials[i], "/+", "/"); // remove duplicate slashes
            }

            return new Tuple<List<string>, List<string>>(materials, models);
        }

        public static List<string> FindVtxMaterials(string path)
        {
            var vtxMaterials = new List<string>();
            if (File.Exists(path))
            {
                using (var vtx = new FileStream(path, FileMode.Open))
                {
                    var reader = new BinaryReader(vtx);

                    var version = reader.ReadInt32();

                    vtx.Seek(20, SeekOrigin.Begin);
                    var numLODs = reader.ReadInt32();

                    // contains no LODs, no reason to continue parsing
                    if (numLODs == 0)
                    {
                        return vtxMaterials;
                    }

                    var materialReplacementListOffset = reader.ReadInt32();

                    // all LOD materials stored in the materialReplacementList
                    // reading material replacement list
                    for (var i = 0; i < numLODs; i++)
                    {
                        var materialReplacementStreamPosition = materialReplacementListOffset + i * 8;
                        vtx.Seek(materialReplacementStreamPosition, SeekOrigin.Begin);
                        var numReplacements = reader.ReadInt32();
                        var replacementOffset = reader.ReadInt32();

                        if (numReplacements == 0)
                        {
                            continue;
                        }

                        vtx.Seek(materialReplacementStreamPosition + replacementOffset, SeekOrigin.Begin);
                        // reading material replacement
                        for (var j = 0; j < numReplacements; j++)
                        {
                            var streamPositionStart = vtx.Position;

                            int materialIndex = reader.ReadInt16();
                            var nameOffset = reader.ReadInt32();

                            var streamPositionEnd = vtx.Position;
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

            var models = new List<string>();

            if (File.Exists(path))
            {
                using (var phy = new FileStream(path, FileMode.Open))
                {
                    using (var reader = new BinaryReader(phy))
                    {
                        var header_size = reader.ReadInt32();
                        phy.Seek(4, SeekOrigin.Current);
                        var solidCount = reader.ReadInt32();

                        phy.Seek(header_size, SeekOrigin.Begin);
                        var solid_size = reader.ReadInt32();

                        phy.Seek(solid_size, SeekOrigin.Current);
                        var something = readNullTerminatedString(phy, reader);

                        var entries = something.Split('{', '}');
                        for (var i = 0; i < entries.Count(); i++)
                        {
                            if (entries[i].Trim().Equals("break"))
                            {
                                var entry = entries[i + 1].Split(new[]
                                {
                                    ' '
                                }, StringSplitOptions.RemoveEmptyEntries);

                                for (var j = 0; j < entry.Count(); j++)
                                {
                                    if (entry[j].Equals("\"model\"") || entry[j].Equals("\"ragdoll\""))
                                    {
                                        models.Add("models\\" + entry[j + 1].Trim('"') + (entry[j + 1].Trim('"').EndsWith(".mdl") ? "" : ".mdl"));
                                    }
                                }
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

            var variations = new List<string>
            {
                ".dx80.vtx",
                ".dx90.vtx",
                ".phy",
                ".sw.vtx",
                ".vtx",
                ".xbox.vtx",
                ".vvd",
                ".ani"
            };
            foreach (var variation in variations)
            {
                var variant = Path.ChangeExtension(path, variation);
                //variant = variant.Replace('/', '\\');
                references.Add(variant);
            }
            return references;
        }

        public static List<string> findVmtTextures(string fullpath)
        {
            // finds vtfs files associated with vmt file

            var vtfList = new List<string>();
            foreach (var line in File.ReadAllLines(fullpath))
            {
                var param = line.Replace("\"", " ").Replace("\t", " ").Trim();

                if (Keys.vmtTextureKeyWords.Any(key => param.ToLower().StartsWith(key + " ")))
                {
                    vtfList.Add("materials/" + vmtPathParser2(line) + ".vtf");
                    if (param.ToLower().StartsWith("$envmap" + " "))
                    {
                        vtfList.Add("materials/" + vmtPathParser2(line) + ".hdr.vtf");
                    }
                }
            }
            return vtfList;
        }

        public static List<string> findVmtMaterials(string fullpath)
        {
            // finds vmt files associated with vmt file

            var vmtList = new List<string>();
            foreach (var line in File.ReadAllLines(fullpath))
            {
                var param = line.Replace("\"", " ").Replace("\t", " ").Trim();
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

            var vmtList = new List<string>();
            foreach (var line in File.ReadAllLines(fullpath))
            {
                var param = line.Replace("\"", " ").Replace("\t", " ").Trim();
                if (param.StartsWith("image ", StringComparison.CurrentCultureIgnoreCase))
                {
                    var path = "materials/vgui/" + vmtPathParser2(line) + ".vmt";
                    path = path.Replace("/vgui/..", "");
                    vmtList.Add(path);
                }
            }
            return vmtList;
        }

        public static List<string> findRadarDdsFiles(string fullpath)
        {
            // finds vmt files associated with radar overview files

            var DDSs = new List<string>();
            var overviewFile = new FileData(fullpath);

            // Contains no blocks, return empty list
            if (overviewFile.headnode.subBlocks.Count == 0)
            {
                return DDSs;
            }

            foreach (var subblock in overviewFile.headnode.subBlocks)
            {
                var material = subblock.TryGetStringValue("material");
                // failed to get material, file contains no materials
                if (material == "")
                {
                    break;
                }

                // add default radar
                DDSs.Add($"resource/{vmtPathParser(material, false)}_radar.dds");

                var verticalSections = subblock.GetFirstByName("\"verticalsections\"");
                if (verticalSections == null)
                {
                    break;
                }

                // add multi-level radars
                foreach (var section in verticalSections.subBlocks)
                {
                    DDSs.Add($"resource/{vmtPathParser(material, false)}_{section.name.Replace("\"", string.Empty)}_radar.dds");
                }
            }

            return DDSs;
        }

        public static string vmtPathParser(string vmtline, bool needsSplit = true)
        {
            if (needsSplit)
            {
                vmtline = vmtline.Split(new[]
                {
                    ' '
                }, 2)[1]; // removes the parameter name
            }
            vmtline = vmtline.Split(new[]
            {
                "//", "\\\\"
            }, StringSplitOptions.None)[0]; // removes endline parameter
            vmtline = vmtline.Trim(' ', '/', '\\'); // removes leading slashes
            vmtline = vmtline.Replace('\\', '/'); // normalize slashes
            if (vmtline.StartsWith("materials/"))
            {
                vmtline = vmtline.Remove(0, "materials/".Length); // removes materials/ if its the beginning of the string for consistency
            }
            if (vmtline.EndsWith(".vmt") || vmtline.EndsWith(".vtf")) // removes extentions if present for consistency
            {
                vmtline = vmtline.Substring(0, vmtline.Length - 4);
            }
            return vmtline;
        }

        // same as above but quotes are not replaced and line does not need to be trimmed, quotes are needed to tell if // are comments or not
        public static string vmtPathParser2(string vmtline)
        {
            vmtline = vmtline.Trim(' ', '\t');

            // remove key
            if (vmtline[0] == '"')
            {
                vmtline = Regex.Match(vmtline, "\"[^\"]+\"(.*)$").Groups[1].Value;
            }
            else
            {
                vmtline = Regex.Match(vmtline, "[^ \t]+(.*)$").Groups[1].Value;
            }

            vmtline = vmtline.TrimStart(' ', '\t');
            // process value
            if (vmtline[0] == '"')
            {
                vmtline = Regex.Match(vmtline, "\"([^\"]+)\"").Groups[1].Value;
            }
            else
            {
                // strip c style comments like this one
                var commentIndex = vmtline.IndexOf("//");
                if (commentIndex > -1)
                {
                    vmtline = vmtline.Substring(0, commentIndex);
                }
                vmtline = Regex.Match(vmtline, "[^ \t]+").Groups[0].Value;
            }

            vmtline = vmtline.Trim(' ', '/', '\\'); // removes leading slashes
            vmtline = vmtline.Replace('\\', '/'); // normalize slashes
            vmtline = Regex.Replace(vmtline, "/+", "/"); // remove duplicate slashes

            if (vmtline.StartsWith("materials/"))
            {
                vmtline = vmtline.Remove(0, "materials/".Length); // removes materials/ if its the beginning of the string for consistency
            }
            if (vmtline.EndsWith(".vmt") || vmtline.EndsWith(".vtf")) // removes extentions if present for consistency
            {
                vmtline = vmtline.Substring(0, vmtline.Length - 4);
            }
            return vmtline;
        }

        public static List<string> findSoundscapeSounds(string fullpath)
        {
            // finds audio files from soundscape file

            char[] special_caracters =
            {
                '*', '#', '@', '>', '<', '^', '(', ')', '}', '$', '!', '?', ' '
            };

            var audioFiles = new List<string>();
            foreach (var line in File.ReadAllLines(fullpath))
            {
                var param = Regex.Replace(line, "[\t|\"]", " ").Trim();
                if (param.ToLower().StartsWith("wave"))
                {
                    var clip = param.Split(new[]
                    {
                        ' '
                    }, 2)[1].Trim(special_caracters);
                    audioFiles.Add("sound/" + clip);
                }
            }
            return audioFiles;
        }

        public static List<string> findManifestPcfs(string fullpath)
        {
            // finds pcf files from the manifest file

            var pcfs = new List<string>();
            foreach (var line in File.ReadAllLines(fullpath))
            {
                if (line.ToLower().Contains("file"))
                {
                    var l = line.Split('"');
                    pcfs.Add(l[l.Count() - 2].TrimStart('!'));
                }
            }
            return pcfs;
        }

        public static void findBspPakDependencies(BSP bsp, string tempdir)
        {
            // Search the temp folder to find dependencies of files extracted from the pak file
            if (Directory.Exists(tempdir))
            {
                foreach (var file in Directory.EnumerateFiles(tempdir, "*.vmt", SearchOption.AllDirectories))
                {
                    foreach (var material in findVmtMaterials(new FileInfo(file).FullName))
                    {
                        bsp.TextureList.Add(material);
                    }

                    foreach (var material in findVmtTextures(new FileInfo(file).FullName))
                    {
                        bsp.TextureList.Add(material);
                    }
                }
            }

        }

        public static void findBspUtilityFiles(BSP bsp, List<string> sourceDirectories, bool renamenav, bool genparticlemanifest)
        {
            // Utility files are other files that are not assets and are sometimes not referenced in the bsp
            // those are manifests, soundscapes, nav, radar and detail files

            // Soundscape file
            var internalPath = "scripts/soundscapes_" + bsp.file.Name.Replace(".bsp", ".txt");
            // Soundscapes can have either .txt or .vsc extensions
            var internalPathVsc = "scripts/soundscapes_" + bsp.file.Name.Replace(".bsp", ".vsc");
            foreach (var source in sourceDirectories)
            {
                var externalPath = source + "/" + internalPath;
                var externalVscPath = source + "/" + internalPathVsc;

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
            foreach (var source in sourceDirectories)
            {
                var externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.soundscript = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                }
            }

            // Nav file (.nav)
            internalPath = "maps/" + bsp.file.Name.Replace(".bsp", ".nav");
            foreach (var source in sourceDirectories)
            {
                var externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    if (renamenav)
                    {
                        internalPath = "maps/embed.nav";
                    }
                    bsp.nav = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                }
            }

            // detail file (.vbsp)
            var worldspawn = bsp.entityList.First(item => item["classname"] == "worldspawn");
            if (worldspawn.ContainsKey("detailvbsp"))
            {
                internalPath = worldspawn["detailvbsp"];

                foreach (var source in sourceDirectories)
                {
                    var externalPath = source + "/" + internalPath;

                    if (File.Exists(externalPath))
                    {
                        bsp.detail = new KeyValuePair<string, string>(internalPath, externalPath);
                        break;
                    }
                }
            }


            // Vehicle scripts
            var vehicleScripts = new List<KeyValuePair<string, string>>();
            foreach (var ent in bsp.entityList)
            {
                if (ent.ContainsKey("vehiclescript"))
                {
                    foreach (var source in sourceDirectories)
                    {
                        var externalPath = source + "/" + ent["vehiclescript"];
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
            var effectScripts = new List<KeyValuePair<string, string>>();
            foreach (var ent in bsp.entityList)
            {
                if (ent.ContainsKey("scriptfile"))
                {
                    foreach (var source in sourceDirectories)
                    {
                        var externalPath = source + "/" + ent["scriptfile"];
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
            var pd_ent = bsp.entityList.FirstOrDefault(item => item["classname"] == "tf_logic_player_destruction");
            if (pd_ent != null && pd_ent.ContainsKey("res_file"))
            {
                foreach (var source in sourceDirectories)
                {
                    var externalPath = source + "/" + pd_ent["res_file"];
                    if (File.Exists(externalPath))
                    {
                        bsp.res = new KeyValuePair<string, string>(pd_ent["res_file"], externalPath);
                        break;
                    }
                }
            }

            // Radar file
            internalPath = "resource/overviews/" + bsp.file.Name.Replace(".bsp", ".txt");
            var ddsfiles = new List<KeyValuePair<string, string>>();
            foreach (var source in sourceDirectories)
            {
                var externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.radartxt = new KeyValuePair<string, string>(internalPath, externalPath);
                    bsp.TextureList.AddRange(findVmtMaterials(externalPath));

                    var ddsInternalPaths = findRadarDdsFiles(externalPath);
                    //find out if they exists or not
                    foreach (var ddsInternalPath in ddsInternalPaths)
                    {
                        foreach (var source2 in sourceDirectories)
                        {
                            var ddsExternalPath = source2 + "/" + ddsInternalPath;
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
            foreach (var source in sourceDirectories)
            {
                var externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.kv = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                }
            }

            // csgo loading screen text file (.txt)
            internalPath = "maps/" + bsp.file.Name.Replace(".bsp", ".txt");
            foreach (var source in sourceDirectories)
            {
                var externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.txt = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                }
            }

            // csgo loading screen image (.jpg)
            internalPath = "maps/" + bsp.file.Name.Replace(".bsp", "");
            foreach (var source in sourceDirectories)
            {
                var externalPath = source + "/" + internalPath;

                foreach (var extension in new[]
                {
                    ".jpg", ".jpeg"
                })
                {
                    if (File.Exists(externalPath + extension))
                    {
                        bsp.jpg = new KeyValuePair<string, string>(internalPath + ".jpg", externalPath + extension);
                    }
                }
            }

            // csgo panorama map icons (.png)
            internalPath = "materials/panorama/images/map_icons/screenshots/";
            var panoramaMapIcons = new List<KeyValuePair<string, string>>();
            foreach (var source in sourceDirectories)
            {
                var externalPath = source + "/" + internalPath;
                var bspName = bsp.file.Name.Replace(".bsp", "");

                foreach (var resolution in new[]
                {
                    "360p", "1080p"
                })
                {
                    if (File.Exists($"{externalPath}{resolution}/{bspName}.png"))
                    {
                        panoramaMapIcons.Add(new KeyValuePair<string, string>($"{internalPath}{resolution}/{bspName}.png", $"{externalPath}{resolution}/{bspName}.png"));
                    }
                }
            }
            bsp.PanoramaMapIcons = panoramaMapIcons;

            // language files, particle manifests and soundscript file
            // (these language files are localized text files for tf2 mission briefings)
            var internalDir = "maps/";
            var name = bsp.file.Name.Replace(".bsp", "");
            var searchPattern = name + "*.txt";
            var langfiles = new List<KeyValuePair<string, string>>();

            foreach (var source in sourceDirectories)
            {
                var externalDir = source + "/" + internalDir;
                var dir = new DirectoryInfo(externalDir);

                if (dir.Exists)
                {
                    foreach (var f in dir.GetFiles(searchPattern))
                    {
                        // particle files if particle manifest is not being generated
                        if (f.Name.StartsWith(name + "_particles") || f.Name.StartsWith(name + "_manifest"))
                        {
                            if (!genparticlemanifest)
                            {
                                bsp.particleManifest = new KeyValuePair<string, string>(internalDir + f.Name, externalDir + f.Name);
                            }
                            continue;
                        }
                        // soundscript
                        if (f.Name.StartsWith(name + "_level_sounds"))
                        {
                            bsp.soundscript =
                                new KeyValuePair<string, string>(internalDir + f.Name, externalDir + f.Name);
                        }
                        // presumably language files
                        else
                        {
                            langfiles.Add(new KeyValuePair<string, string>(internalDir + f.Name, externalDir + f.Name));
                        }
                    }
                }
            }
            bsp.languages = langfiles;

            // ASW/Source2009 branch VScripts
            var vscripts = new List<string>();

            foreach (var entity in bsp.entityList)
            {
                foreach (var kvp in entity)
                {
                    if (kvp.Key.ToLower() == "vscripts")
                    {
                        var scripts = kvp.Value.Split(' ');
                        foreach (var script in scripts)
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
            var verString = new List<byte>();
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
