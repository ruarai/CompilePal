using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.IO;

namespace BSPPack
{
    // this is the class that stores data about the bsp.
    // You can find information about the file format here
    // https://developer.valvesoftware.com/wiki/Source_BSP_File_Format#BSP_file_header

    class BSP
    {
        private FileStream bsp;
        private BinaryReader reader;
        public KeyValuePair<int, int>[] offsets { get; private set; } // offset/length

        private List<string> textureList = new List<string>();
        private List<string> modelList = new List<string>();

        public BSP(FileInfo file)
        {
            offsets = new KeyValuePair<int, int>[64];
            bsp = new FileStream(file.FullName, FileMode.Open);
            reader = new BinaryReader(bsp);

            //gathers an array of of where things are located in the bsp
            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                bsp.Seek(8, SeekOrigin.Current);
                offsets[i] = new KeyValuePair<int, int>(reader.ReadInt32(), reader.ReadInt32());
                    
            }
            getTextureList();
        }

        public List<string> getTextureList()
        {
            if (textureList.Count == 0)
            {
                bsp.Seek(offsets[43].Key, SeekOrigin.Begin);
                textureList = new List<string>(Encoding.ASCII.GetString(reader.ReadBytes(offsets[43].Value)).Split('\0'));
            }
            return textureList;
        }

        public List<string> getModelList()
        {
            if (modelList.Count == 0)
            {
                bsp.Seek(offsets[0].Key, SeekOrigin.Begin);
                //modelList = todo
            }
            return modelList;
        }
    }
}