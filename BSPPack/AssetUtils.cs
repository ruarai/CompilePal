using System;
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
                        materials.Add(modelDirs[j] + modelVmts[i] + ".vmt");
                    }
                }
            }
            return materials;
        }

        public static List<string> findMdlRefs(string path) {
            var references = new List<string>();

            var variations = new List<string> { ".dx80.vtx", ".dx90.vtx", ".phy", ".sw.vtx", ".sw.vtx" };
            foreach (string variation in variations)
            {
                string variant = Path.ChangeExtension(path, variation);
                Console.WriteLine(variant);
                references.Add(variant);
            }
            return references;
        }

        public static List<string> findVmtTextures(string path) { return new List<string>(); }

        public static List<string> findManifestPcfs(string path) { return new List<string>(); }

        public static List<string> findSoundscapeSounds(string path) { return new List<string>(); }

        public static List<string> findPcfMaterials(string path) { return new List<string>(); }

        public static void findBspUtilityFiles(BSP bsp, List<string> sourceDirectories)
        { 
            // Utility files are other files that are not assets
            // those are manifests, soundscapes, nav and detail files

            bsp.particleManifest = "";
            bsp.soundscape = "";
            bsp.nav = "";
            bsp.detail = "";

            // Particles manifest
            foreach (string source in sourceDirectories)
            {
                string guess = source + "\\particles\\" +
                    bsp.file.Name.Replace(".bsp", "_manifest.txt");
                if (File.Exists(guess))
                {
                    bsp.particleManifest = guess;
                    break;
                }
                guess = source + "\\maps\\" +
                    bsp.file.Name.Replace(".bsp", "_particles.txt");
                if (File.Exists(guess))
                {
                    bsp.particleManifest = guess;
                    break;
                }
            }

            // Soundscape file
            foreach (string source in sourceDirectories)
            {
                string guess = source + "\\scripts\\soundscapes_" +
                    bsp.file.Name.Replace(".bsp", ".txt");
                if (File.Exists(guess))
                {
                    bsp.soundscape = guess;
                    break;
                }
            }

            // Nav file (.nav)
            foreach (string source in sourceDirectories)
            {
                string guess = source + "\\maps\\" + bsp.file.Name.Replace(".bsp", ".nav");
                if (File.Exists(guess))
                {
                    bsp.nav = guess;
                    break;
                }
            }

            // todo: detail file (.vbsp)

            // maybe can be read form bsp directly?
            // bsp.detail = ??

        }
    }
}
