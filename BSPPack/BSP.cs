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
        private KeyValuePair<int, int>[] offsets; // offset/length

        public List<Dictionary<string, string>> entityList { get; private set; }
        
        private List<int>[] modelSkinList;

        public List<string> ModelList { get; private set; }
        public List<string> EntModelList { get; private set; }

        public List<string> TextureList { get; private set; }
        public List<string> EntTextureList { get; private set; }

        public List<string> EntSoundList { get; private set; }

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

            buildEntityList();

            buildEntModelList();
            buildModelList();

            buildEntTextureList();
            buildTextureList();

            buildEntSoundList();
        }

        public void buildEntityList()
        {
            entityList = new List<Dictionary<string, string>>();
            
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
                    foreach (string s in rawent.Split('\n'))
                    {
                        if (s.Count() != 0)
                        {
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

        public void buildTextureList()
        {
            // builds the list of textures applied to brushes

            string mapname = bsp.Name.Split('\\').Last().Split('.')[0];

            TextureList = new List<string>();
            bsp.Seek(offsets[43].Key, SeekOrigin.Begin);
            TextureList = new List<string>(Encoding.ASCII.GetString(reader.ReadBytes(offsets[43].Value)).Split('\0'));
            for (int i = 0; i < TextureList.Count; i++)
            {
                TextureList[i] = "materials/" + TextureList[i] + ".vmt";

                // in the special case where we are dealing with water materials
                if (TextureList[i].StartsWith("materials/maps/"+mapname+"/water/")){
                    string[] nameparts = TextureList[i].Split('/').Last().Split('_');
                    string filename = "";
                    for (int j = 0; j < nameparts.Count() - 3; j++)
                    {
                        filename += nameparts[j] + "_";
                    }
                    TextureList.Add("water/" + filename.TrimEnd('_'));
                }
            }

            // find skybox materials
            Dictionary<string, string> worldspawn = entityList.First(item => item["classname"] == "worldspawn");
            foreach (string s in new string[]{"bk","dn","ft","lf","rt","up"})
                TextureList.Add("materials/skybox/" + worldspawn["skyname"] + s + ".vmt");

            // find detail files
            TextureList.Add("materials/" + worldspawn["detailmaterial"] + ".vmt");

            // find menu photos
            TextureList.Add("materials/vgui/maps/menu_photos_" + mapname + ".vmt");
        }

        public void buildEntTextureList()
        {
            // builds the list of textures referenced in entities

            EntTextureList = new List<string>();
            foreach (Dictionary<string, string> ent in entityList)
                foreach (KeyValuePair<string, string> prop in ent)
                    if (Keys.vmfMaterialKeys.Contains(prop.Key))
                        EntTextureList.Add(prop.Value);
        }

        public void buildModelList()
        {
            // builds the list of models that are from prop_static

            ModelList = new List<string>();
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
                    ModelList.Add(model);
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

        public void buildEntModelList()
        {
            // builds the list of models referenced in entities

            EntModelList = new List<string>();
            foreach (Dictionary<string, string> ent in entityList)
                foreach (KeyValuePair<string, string> prop in ent)
                    if (!ent["classname"].StartsWith("func") &&
                        !ent["classname"].StartsWith("trigger") &&
                        Keys.vmfModelKeys.Contains(prop.Key))
                            EntModelList.Add(prop.Value);
        }

        public void buildEntSoundList()
        {
            // builds the list of sounds referenced in entities

            EntSoundList = new List<string>();
            foreach (Dictionary<string, string> ent in entityList)
                foreach (KeyValuePair<string, string> prop in ent)
                    if (Keys.vmfSoundKeys.Contains(prop.Key))
                        EntSoundList.Add("sound/"+ prop.Value);
        }
    }
}