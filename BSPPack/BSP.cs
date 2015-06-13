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

        private List<Dictionary<string, string>> entityList = new List<Dictionary<string,string>>();
        private List<string> textureList = new List<string>();
        private List<string> modelList = new List<string>();
        private List<int>[] modelSkinList;
        private List<string> modelListDyn = new List<string>();

        public BSP(FileInfo file)
        {
            offsets = new KeyValuePair<int, int>[64];
            bsp = new FileStream(file.FullName, FileMode.Open);
            reader = new BinaryReader(bsp);

            //gathers an array of of where things are located in the bsp
            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                bsp.Seek(8, SeekOrigin.Current); //skip id and version
                offsets[i] = new KeyValuePair<int, int>(reader.ReadInt32(), reader.ReadInt32());
            }
            getEntityList();
            getModelListDyn();
            getModelList();
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

        public List<string> getModelListDyn()
        {
            // gets the list of models that are not from prop_static
            if (modelList.Count == 0)
            {
                foreach (Dictionary<string, string> ent in entityList)
                {
                    if (ent["classname"].StartsWith("prop_") && ent["model"].Length != 0)
                        modelListDyn.Add(ent["model"]);
                }
                //for these we want to add all skins
            }
            
            return modelListDyn;
        }

        public List<string> getModelList()
        {
            // gets the list of models that are from prop_static
            if (modelList.Count == 0)
            {
                // getting information on the gamelump
                int propStaticId = 0;
                bsp.Seek(offsets[35].Key, SeekOrigin.Begin);
                KeyValuePair<int, int>[] GameLumpOffsets = new KeyValuePair<int,int>[reader.ReadInt32()]; // offset/length
                for (int i = 0; i < GameLumpOffsets.Length; i++)
                {
                    if (reader.ReadInt32() == 1936749168)
                        propStaticId = i;
                    bsp.Seek(4, SeekOrigin.Current); //skip flags and version
                    GameLumpOffsets[i] = new KeyValuePair<int,int>(reader.ReadInt32(), reader.ReadInt32());
                }

                // reading model names from game lump
                bsp.Seek(GameLumpOffsets[propStaticId].Key, SeekOrigin.Begin);
                int modelCount = reader.ReadInt32();
                for (int i = 0; i < modelCount; i++)
                {
                    string model = Encoding.ASCII.GetString(reader.ReadBytes(128)).Trim('\0');
                    if (model.Length != 0)
                        modelList.Add(model);
                }

                // skipping leaf lump
                int leafCount = reader.ReadInt32();
                bsp.Seek(leafCount * 2, SeekOrigin.Current);

                // reading staticprop lump
                
                int propCount = reader.ReadInt32();
                long propOffset = bsp.Position;
                int byteLength = GameLumpOffsets[1].Key - (int)propOffset;
                int propLength = byteLength / propCount;

                modelSkinList = new List<int>[modelCount];
                for (int i = 0; i < modelCount; i++)
                    modelSkinList[i] = new List<int>();

                for (int i = 0; i < propCount; i++)
                {
                    bsp.Seek(i * propLength + propOffset + 24, SeekOrigin.Begin); // 24 skips origin and angles
                    int modelId = reader.ReadUInt16();
                    bsp.Seek(6, SeekOrigin.Current);
                    int skin = reader.ReadInt32();

                    if (modelSkinList[modelId].IndexOf(skin) == -1)
                        modelSkinList[modelId].Add(skin);
                }
            }
            return modelList;
        }

        public List<Dictionary<string, string>> getEntityList()
        {
            if (entityList.Count == 0)
            {
                bsp.Seek(offsets[0].Key, SeekOrigin.Begin);
                byte[] ent = reader.ReadBytes(offsets[0].Value);
                List<byte> ents = new List<byte>();
                
                for (int i = 0; i < ent.Length; i++)
                {
                    if (ent[i] != 123 && ent[i] != 125)
                        ents.Add(ent[i]);

                    else if (ent[i] == 125)
                    {

                        string rawent = Encoding.ASCII.GetString(ents.ToArray());
                        Dictionary<string, string> entity = new Dictionary<string, string>();
                        foreach (string s in rawent.Split('\n')){   
                            if (s.Count() != 0){
                                string[] c = s.Split('"');
                                entity.Add(c[1], c[3]);

                                //everything after the hammerid is input/outputs
                                if (c[1] == "hammerid")
                                    break;
                            }
                        }
                        entityList.Add(entity);
                        ents = new List<byte>();
                    }
                }
            }
            return entityList;
        }
    }
}