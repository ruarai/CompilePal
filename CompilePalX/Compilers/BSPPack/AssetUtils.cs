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

        public static List<string> findMdlRefs(string path)
        {
            var references = new List<string>();

            var variations = new List<string> { ".dx80.vtx", ".dx90.vtx", ".phy", ".sw.vtx", ".vvd" };
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
                    vtfList.Add("materials/" +
                        param.Split(new char[] { ' ' }, 2)[1].Trim() + ".vtf");
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
                    vmtList.Add("materials/" + param.Split(new char[] { ' ' }, 2)[1].Trim());
                    if (!vmtList.Last().EndsWith(".vmt"))
                        vmtList[vmtList.Count - 1] += ".vmt";
                }
            }
            return vmtList;
        }

        public static List<string> findSoundscapeSounds(string fullpath)
        {
            // finds audio files from soundscape file

            List<string> audioFiles = new List<string>();
            foreach (string line in File.ReadAllLines(fullpath))
            {
                string param = Regex.Replace(line, "[\t|\"]", " ").Trim();
                if (param.ToLower().StartsWith("wave"))
                {
                    string clip = param.Split(new char[] { ' ' }, 2)[1].Trim(' ', ')', '(');
                    audioFiles.Add("sound/" + clip);
                }
            }
            return audioFiles;
        }

        public static List<string> findPcfMaterials(string path)
        {
            List<string> materials = new List<string>();

            if (File.Exists(path))
            {
                FileStream pcf = new FileStream(path, FileMode.Open);
                BinaryReader reader = new BinaryReader(pcf);

                string ver = readNullTerminatedString(pcf, reader);

                if (!ver.Equals("<!-- dmx encoding binary 2 format pcf 1 -->\n"))
                {
                    Console.WriteLine("Warning: Pcf File not supported,\n" +
                    "\t custom materials will not be added if used");
                    return materials;
                }

                // read pcf strings
                pcf.Seek(45, SeekOrigin.Begin);

                uint nbstring = reader.ReadUInt16();
                string[] pcfStrings = new string[nbstring];
                for (int i = 0; i < nbstring; i++)
                    pcfStrings[i] = readNullTerminatedString(pcf, reader);

                // skipping over pcf elements
                int nbElements = reader.ReadInt32();
                for (int i = 0; i < nbElements; i++)
                {
                    pcf.Seek(2, SeekOrigin.Current);
                    readNullTerminatedString(pcf, reader);
                    pcf.Seek(16, SeekOrigin.Current);
                }

                // read element data
                for (int e = 0; e < nbElements; e++)
                {
                    int nbElemAtribs = reader.ReadInt32();
                    for (int p = 0; p < nbElemAtribs; p++)
                    {
                        int typeid = reader.ReadInt16();
                        int attributeType = reader.ReadByte();
                        int count = (attributeType > 14) ? reader.ReadInt32() : 1;
                        attributeType = (attributeType > 14) ? attributeType - 14 : attributeType;

                        int[] typelength = { 0, 4, 4, 4, 1, 1, 4, 4, 4, 8, 12, 16, 12, 16, 64 };

                        switch (attributeType)
                        {
                            case 5:
                                string attributeValue = readNullTerminatedString(pcf, reader);
                                if (pcfStrings[typeid] == "material")
                                    materials.Add("materials/" + attributeValue);
                                break;

                            case 6:
                                for (int i = 0; i < count; i++)
                                {
                                    uint len = reader.ReadUInt32();
                                    pcf.Seek(len, SeekOrigin.Current);
                                }
                                break;

                            default:
                                pcf.Seek(typelength[attributeType] * count, SeekOrigin.Current);
                                break;
                        }

                    }

                }
                pcf.Close();
            }
            return materials;
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
            if (Directory.Exists("tmp"))
                foreach (String file in Directory.EnumerateFiles("tmp", "*.vmt", SearchOption.AllDirectories))
                    foreach (string material in AssetUtils.findVmtMaterials(new FileInfo(file).FullName))
                        bsp.TextureList.Add(material);
        }

        public static void findBspUtilityFiles(BSP bsp, List<string> sourceDirectories)
        {
            // Utility files are other files that are not assets
            // those are manifests, soundscapes, nav and detail files

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
                        // particle files
                        if (f.Name.StartsWith(name + "_particles") || f.Name.StartsWith(name + "_manifest"))
                            bsp.particleManifest = new KeyValuePair<string, string>(internalDir + f.Name, externalDir + f.Name);
                        // soundscript
                        else if (f.Name.StartsWith(name + "_level_sounds"))
                            bsp.soundscript = new KeyValuePair<string, string>(internalDir + f.Name, externalDir + f.Name);
                        // presumably language files
                        else
                            langfiles.Add(new KeyValuePair<string, string>(internalDir + f.Name, externalDir + f.Name));
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
            } while (v != '\0');

            return Encoding.ASCII.GetString(verString.ToArray()).Trim('\0');
        }
    }
}
