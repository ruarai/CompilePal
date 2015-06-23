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
        static List<string> vmfSoundkeys = File.ReadAllLines(Path.Combine("..//..//..//Keys//", "vmfsoundkeys.txt")).ToList();
        
        private FileStream bsp;
        private BinaryReader reader;
        private KeyValuePair<int, int>[] offsets; // offset/length

        private List<Dictionary<string, string>> entityList = new List<Dictionary<string,string>>();
        
        private List<int>[] modelSkinList;
        private List<string> rawModelList = new List<string>();
        private List<string> rawModelListDyn = new List<string>();
        private List<string> rawTextureList = new List<string>();
        private List<string> rawSoundList = new List<string>();

        // key/values as internalPath/externalPath
        public KeyValuePair<string, string> particleManifest { get; set; }
        public KeyValuePair<string, string> soundscape { get; set; }
        public KeyValuePair<string, string> detail {get; set; }
        public KeyValuePair<string, string> nav {get; set; }

        public FileInfo file { get; private set; }

        public BSP(FileInfo file)
        {
            this.file = file;

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
            getSoundList();
        }

        public List<string> getTextureList()
        {
            if (rawTextureList.Count == 0)
            {
                bsp.Seek(offsets[43].Key, SeekOrigin.Begin);
                rawTextureList = new List<string>(Encoding.ASCII.GetString(reader.ReadBytes(offsets[43].Value)).Split('\0'));
                for (int i = 0; i < rawTextureList.Count; i++)
                    rawTextureList[i] = "materials/" + rawTextureList[i]+ ".vmt";
            }
            return rawTextureList;
        }

        public List<string> getModelListDyn()
        {
            // gets the list of models that are not from prop_static
            if (rawModelList.Count == 0)
            {
                foreach (Dictionary<string, string> ent in entityList)
                {
                    // todo: there are more entities with custom models
                    if (ent["classname"].StartsWith("prop_") && ent["model"].Length != 0)
                        rawModelListDyn.Add(ent["model"]);
                }
                //for these we want to add all skins
            }
            
            return rawModelListDyn;
        }

        public List<string> getModelList()
        {
            // gets the list of models that are from prop_static
            if (rawModelList.Count == 0)
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
                        rawModelList.Add(model);
                }

                // from now on we have models, now we want to know what skins they use

                // skipping leaf lump
                int leafCount = reader.ReadInt32();
                bsp.Seek(leafCount * 2, SeekOrigin.Current);

                // reading staticprop lump
                
                int propCount = reader.ReadInt32();
                long propOffset = bsp.Position;
                int byteLength = GameLumpOffsets[1].Key - (int)propOffset;
                int propLength = byteLength / propCount;

                modelSkinList = new List<int>[modelCount]; // stores the ids of used skins

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
            return rawModelList;
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

        public List<string> getSoundList()
        {
            foreach (Dictionary<string, string> ent in entityList)
            {
                foreach (KeyValuePair<string, string> k in ent){
                    if (vmfSoundkeys.Contains(k.Key))
                        rawSoundList.Add("sound/"+ k.Value);
                }
            }
            return rawSoundList;
        }
    }
}