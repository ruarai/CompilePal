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

                mdl.Seek(76, SeekOrigin.Begin);
                int datalength = reader.ReadInt32();
                mdl.Seek(124, SeekOrigin.Current);

                int textureCount = reader.ReadInt32();
                int textureOffset = reader.ReadInt32();

                int texturedirCount = reader.ReadInt32();
                int texturedirOffset = reader.ReadInt32();

                int	skinreferenceCount = reader.ReadInt32();
	            int	skinrfamilyCount = reader.ReadInt32();
	            int skinreferenceIndex = reader.ReadInt32();

                mdl.Seek(textureOffset, SeekOrigin.Begin);
                int[] textureNameOffsets = new int[textureCount];
                for (int i = 0; i < textureCount; i++)
                {
                    textureNameOffsets[i] = reader.ReadInt32();
                    mdl.Seek(60, SeekOrigin.Current);
                }
                Console.WriteLine("endede at " + mdl.Position);

                for (int i = 0; i < textureCount; i++)
                {
                    mdl.Seek((64 * i) + textureOffset + textureNameOffsets[i], SeekOrigin.Begin);
                    List<byte> byteString= new List<byte>();
                    byte b;
                    do
                    {
                        b = reader.ReadByte();
                        byteString.Add(b);
                    } while (b != '\0');

                    string modelname = Encoding.ASCII.GetString(byteString.ToArray()).Trim('\0');
                }

                // todo figure out used skins

                /*
                mdl.Seek(skinreferenceIndex, SeekOrigin.Begin);
                Console.WriteLine(reader.ReadInt16());
                Console.WriteLine(reader.ReadInt16());
                Console.WriteLine(reader.ReadInt16());
                Console.WriteLine(reader.ReadInt16());
                Console.WriteLine(reader.ReadInt16());
                Console.WriteLine(reader.ReadInt16());*/
                
                
            }
            return materials;
        }

        public static void findMdlparticles(string path) { }

        public static void findPcfMaterials(string path) { }

    }
}
