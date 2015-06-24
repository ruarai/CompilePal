using System    ;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSPPack
{
    static class AssetUtils
    {
        public static List<string> findMdlMaterials(string path, int[] skin = null)
        {
            List<string> materials = new List<string>();

            if (File.Exists(path))
            {

                FileStream mdl = new FileStream(path, FileMode.Open);
                BinaryReader reader = new BinaryReader(mdl);

                List<string> modelVmts = new List<string>();
                List<string> modelDirs = new List<string>();

                mdl.Seek(76, SeekOrigin.Begin);
                int datalength = reader.ReadInt32();
                mdl.Seek(124, SeekOrigin.Current);

                int textureCount = reader.ReadInt32();
                int textureOffset = reader.ReadInt32();

                int textureDirCount = reader.ReadInt32();
                int textureDirOffset = reader.ReadInt32();

                int	skinreferenceCount = reader.ReadInt32();
	            int	skinrfamilyCount = reader.ReadInt32();
	            int skinreferenceIndex = reader.ReadInt32();

                int bodypart_count = reader.ReadInt32();

                // find model names
                for (int i = 0; i < textureCount; i++)
                {
                    mdl.Seek(textureOffset + (i * 64), SeekOrigin.Begin);
                    int textureNameOffset = reader.ReadInt32();

                    mdl.Seek(textureOffset + (i * 64) + textureNameOffset, SeekOrigin.Begin);
                    List<byte> byteString= new List<byte>();
                    byte b;
                    do
                    {
                        b = reader.ReadByte();
                        byteString.Add(b);
                    } while (b != '\0');
                    modelVmts.Add(Encoding.ASCII.GetString(byteString.ToArray()).Trim('\0'));
                }

                // find model dirs
                List<int> textureDirOffsets = new List<int>();
                for (int i = 0; i < textureDirCount; i++)
                {
                    mdl.Seek(textureDirOffset + (4 * i), SeekOrigin.Begin);
                    int offset = reader.ReadInt32();
                    mdl.Seek(offset, SeekOrigin.Begin);
                    List<byte> byteString = new List<byte>();
                    byte b;
                    do
                    {
                        b = reader.ReadByte();
                        byteString.Add(b);
                    } while (b != '\0');
                    modelDirs.Add(Encoding.ASCII.GetString(byteString.ToArray()).Trim('\0'));
                }

                // warning: reading the skin table in mdl is really freaking dodgy.
                // all documentation is unreliable or incomplete so this code is 
                // based on my own interpretation of the data.

                // what needs to be known is that the skin table is larger than
                // what actually gets used and is padded with bogus info, what
                // follows is an attempt at filtering crap.

                /*
                Console.WriteLine("refcount " + skinreferenceCount); //width of which we only take an undefined amount?
                Console.WriteLine("famcount " + skinrfamilyCount); //height 
                Console.WriteLine("skinoffs " + skinreferenceIndex);
                Console.WriteLine("bodyparts " + bodypart_count);

                mdl.Seek(skinreferenceIndex, SeekOrigin.Begin);
                int skintablesize = skinreferenceCount * skinrfamilyCount;
                //variantMap
                short[,] skintable = new short[skinrfamilyCount,skinreferenceCount];
                for (int i = 0; i < skinrfamilyCount; i++)
                    for (int j = 0; j < skinreferenceCount; j++)
                        skintable[i, j] = reader.ReadInt16();

                for (int i = 0; i < skinrfamilyCount; i++)
                    for (int j = 0; j < skinreferenceCount; j++)
                        Console.WriteLine(skintable[i, j]);
                */

                // build vmt paths
                for (int i = 0; i < modelVmts.Count; i++)
                {
                    for (int j = 0; j < modelDirs.Count; j++)
                    {
                        modelDirs[j] = modelDirs[j].TrimStart(new char[]{'/', '\\'});
                        materials.Add("materials/" + modelDirs[j] + modelVmts[i] + ".vmt");
                    }
                }
                mdl.Close();
            }
            return materials;
        }

        public static List<string> findMdlRefs(string path) {
            var references = new List<string>();

            var variations = new List<string> { ".dx80.vtx", ".dx90.vtx", ".phy", ".sw.vtx", ".vvd" };
            foreach (string variation in variations)
            {
                string variant = Path.ChangeExtension(path, variation);
                references.Add(variant);
            }
            return references;
        }

        public static List<string> findVmtTextures(string fullpath) {
            // finds vtfs files associated with vmt file
            
            List<string> vtfList = new List<string>();
            foreach (string line in File.ReadAllLines(fullpath))
            {
                string param = line.Replace("\"", " ").Replace("\t"," ").Trim();

                if (Keys.vmtTextureKeyWords.Any(key => param.StartsWith(key+" ")))
                    vtfList.Add("materials/" +
                        param.Split(new char[] { ' ' }, 2)[1].Trim() +".vtf");
            }
            return vtfList;
        }

        public static List<string> findVmtMaterials(string fullpath)
        {
            // finds vtfs files associated with vmt file

            List<string> vmtList = new List<string>();
            foreach (string line in File.ReadAllLines(fullpath))
            {
                string param = line.Replace("\"", " ").Replace("\t", " ").Trim();
                if (Keys.vmtMaterialKeyWords.Any(key => param.StartsWith(key + " ")))
                    vmtList.Add("materials/" +
                        param.Split(new char[] { ' ' }, 2)[1].Trim() + ".vmt");
            }
            return vmtList;
        }

        public static List<string> findSoundscapeSounds(string fullpath) {
            // finds audio files from soundscape file

            List<string> audioFiles = new List<string>();
            foreach (string line in File.ReadAllLines(fullpath))
            {
                if (line.ToLower().Contains("\"wave\""))
                {
                    string[] l = line.Split('"');
                    audioFiles.Add("sounds/" + l[l.Count() - 2]);
                }
            }
            return audioFiles;
        }

        public static List<string> findPcfMaterials(string path)
        {
            // holy mess batman

            List<string> materials = new List<string>();

            if (File.Exists(path))
            {

                FileStream pcf = new FileStream(path, FileMode.Open);
                BinaryReader reader = new BinaryReader(pcf);

                // read pcf strings
                pcf.Seek(45, SeekOrigin.Begin);

                uint nbstring = reader.ReadUInt16();
                string[] pcfStrings = new string[nbstring];
                for (int i = 0; i < nbstring; i++)
                {
                    List<byte> byteString = new List<byte>();
                    byte b;
                    do
                    {
                        b = reader.ReadByte();
                        if (b != '\0')
                            byteString.Add(b);
                    } while (b != '\0');
                    pcfStrings[i] = Encoding.ASCII.GetString(byteString.ToArray());
                }

                // skipping over pcf elements
                int nbElements = reader.ReadInt32();
                for (int i = 0; i < nbElements; i++)
                {
                    pcf.Seek(2, SeekOrigin.Current);
                    byte b;
                    do { b = reader.ReadByte(); } while (b != '\0');
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
                                List<byte> byteString = new List<byte>();
                                byte b;
                                do
                                {
                                    b = reader.ReadByte();
                                    byteString.Add(b);
                                } while (b != '\0');
                                string attributeValue = Encoding.ASCII.GetString(byteString.ToArray()).Trim('\0');

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

        public static List<string> findManifestPcfs(string fullpath) {
            // finds pcf files from the manifest file

            List<string> pcfs = new List<string>();
            foreach (string line in File.ReadAllLines(fullpath))
            {
                if (line.ToLower().Contains("file")) {
                    string[] l = line.Split('"');
                    pcfs.Add(l[l.Count() - 2].TrimStart('!'));
                }
            }
            return pcfs;
        }

        public static void findBspUtilityFiles(BSP bsp, List<string> sourceDirectories)
        { 
            // Utility files are other files that are not assets
            // those are manifests, soundscapes, nav and detail files

            // Particles manifest
            string internalPath = "particles/" + bsp.file.Name.Replace(".bsp", "_manifest.txt");
            foreach (string source in sourceDirectories)
            {
                string externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.particleManifest = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                }

                internalPath = "maps/" + bsp.file.Name.Replace(".bsp", "_particles.txt");
                externalPath = source + "/" + internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.particleManifest = new KeyValuePair<string, string>(internalPath, externalPath);
                    break;
                }
            }

            // Soundscape file
            internalPath = "scripts/soundscapes_" + bsp.file.Name.Replace(".bsp", ".txt");
            foreach (string source in sourceDirectories)
            {
                string externalPath = source +"/"+ internalPath;

                if (File.Exists(externalPath))
                {
                    bsp.soundscape = new KeyValuePair<string, string>(internalPath, externalPath);
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
            internalPath = bsp.entityList.First(item => item["classname"] == "worldspawn")["detailvbsp"];
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
    }
}
