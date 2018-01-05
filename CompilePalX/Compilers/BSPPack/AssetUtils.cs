using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompilePalX.Compilers.BSPPack
{
    static class AssetUtils
    {

        public static List<string> findMdlMaterials(string path, List<int> skins = null)
        {
            List<string> materials = new List<string>();

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

                int bodypart_count = reader.ReadInt32();
                int bodypart_index = reader.ReadInt32();

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

                    for (int i = 0; i < bodypart_count; i++)
                    // we are reading an array of mstudiobodyparts_t
                    {
                        mdl.Seek(bodypart_index + i * 32, SeekOrigin.Begin);

                        mdl.Seek(4, SeekOrigin.Current);
                        int nummodels = reader.ReadInt32();
                        mdl.Seek(4, SeekOrigin.Current);
                        int modelindex = reader.ReadInt32();

                        for (int j = 0; j < nummodels; j++)
                        // we are reading an array of mstudiomodel_t
                        {
                            mdl.Seek(bodypart_index + modelindex + j * 140, SeekOrigin.Begin);

                            mdl.Seek(72, SeekOrigin.Current);
                            int nummeshes = reader.ReadInt32();
                            int meshindex = reader.ReadInt32();

                            for (int k = 0; k < nummeshes; k++)
                            // we are reading an array of mstudiomesh_t
                            {
                                mdl.Seek(bodypart_index + modelindex + meshindex + (k * 116), SeekOrigin.Begin);
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
                    if (skins.IndexOf(0) == -1 && skins.Where(s => s >= trimmedtable.GetLength(0)).Count() != 0)
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
                mdl.Close();
            }
            return materials;
        }

        public static List<string> findPhyGibs(string path)
        {
            // finds gibs and ragdolls found in .phy files

            List<string> models = new List<string>();

            if (File.Exists(path))
            {
                FileStream phy = new FileStream(path, FileMode.Open);
                BinaryReader reader = new BinaryReader(phy);
                int header_size = reader.ReadInt32();
                phy.Seek(4, SeekOrigin.Current);
                int solidCount = reader.ReadInt32();

                phy.Seek(header_size, SeekOrigin.Begin);
                int solid_size = reader.ReadInt32();
                
                phy.Seek(solid_size, SeekOrigin.Current);
                string something = readNullTerminatedString(phy, reader);
                
                string[] entries = something.Split(new char [] { '{','}' });
                for (int i = 0; i < entries.Count(); i++ )
                {
                    if (entries[i].Trim().Equals("break")){
                        string[] entry = entries[i + 1].Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);

                        for (int j = 0; j < entry.Count(); j++)
                            if (entry[j].Equals("\"model\"") || entry[j].Equals("\"ragdoll\""))
                                models.Add("models\\" + entry[j + 1].Trim('"')+".mdl");
                    }
                }
            }
            return models;
        }

        public static List<string> findMdlRefs(string path)
        {
            // finds files associated with .mdl

            var references = new List<string>();

            var variations = new List<string> { ".dx80.vtx", ".dx90.vtx", ".phy", ".sw.vtx", ".vtx", ".xbox.vtx", ".vvd" };
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
                    vtfList.Add("materials/" + vmtPathParser(param) + ".vtf");
                    if (param.ToLower().StartsWith("$envmap" + " "))
                        vtfList.Add("materials/" + vmtPathParser(param) + ".hdr.vtf");
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
                    vmtList.Add("materials/" + vmtPathParser(param) + ".vmt");
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
                    string path = "materials/vgui/" + vmtPathParser(param) + ".vmt";
                    path = path.Replace("/vgui/..", "");
                    vmtList.Add(path);
                }
            }
            return vmtList;
        }

        public static List<string> findRadarDdsFiles(string fullpath)
        {
            // finds vmt files associated with vmt file

            List<string> DDSs = new List<string>();
            foreach (string line in File.ReadAllLines(fullpath))
            {   
                string param = line.Replace("\"", " ").Replace("\t", " ").Trim();
                if (param.StartsWith("material "))
                {
                    DDSs.Add("resource/" + vmtPathParser(param) + "_radar.dds");
                    DDSs.Add("resource/" + vmtPathParser(param) + "_radar_spectate.dds");
                    break;
                }
            }
            return DDSs;
        }

        public static string vmtPathParser(string vmtline)
        {
            vmtline = vmtline.Split(new char[] { ' ' }, 2)[1]; // removes the parameter name
            vmtline = vmtline.Split(new string[] { "//", "\\\\" }, StringSplitOptions.None)[0]; // removes endline parameter
            vmtline = vmtline.Trim(new char[] { ' ', '/', '\\' }); // removes leading slashes
            vmtline = vmtline.Split(new string[] { "materials/" }, StringSplitOptions.RemoveEmptyEntries)[0];// removes materials/ for consistency
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
                    foreach (string material in AssetUtils.findVmtMaterials(new FileInfo(file).FullName))
                        bsp.TextureList.Add(material);
        }

        public static void findBspUtilityFiles(BSP bsp, List<string> sourceDirectories, bool renamenav, bool genparticlemanifest)
        {
            // Utility files are other files that are not assets and are sometimes not referenced in the bsp
            // those are manifests, soundscapes, nav, radar and detail files
            
            // Soundscape file
            string internalPath = "scripts/soundscapes_" + bsp.file.Name.Replace(".bsp", ".txt");
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.soundscape = new KeyValuePair<string, string>(internalPath, externalPath);
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
                        if (!genparticlemanifest)
                            if (f.Name.StartsWith(name + "_particles") || f.Name.StartsWith(name + "_manifest"))
                                bsp.particleManifest =
                                    new KeyValuePair<string, string>(internalDir + f.Name, externalDir + f.Name);

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
