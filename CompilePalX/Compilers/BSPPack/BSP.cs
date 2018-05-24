using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompilePalX.Compilers.BSPPack
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

        public List<int>[] modelSkinList { get; private set; }

        public List<string> ModelList { get; private set; } 

        public List<string> EntModelList { get; private set; }

        public List<string> ParticleList { get; private set; }

        public List<string> TextureList { get; private set; }
        public List<string> EntTextureList { get; private set; }

        public List<string> EntSoundList { get; private set; }

        // key/values as internalPath/externalPath
        public KeyValuePair<string, string> particleManifest { get; set; }
        public KeyValuePair<string, string> soundscript { get; set; }
        public KeyValuePair<string, string> soundscape { get; set; }
        public KeyValuePair<string, string> detail { get; set; }
        public KeyValuePair<string, string> nav { get; set; }
        public KeyValuePair<string, string> res { get; set; }
        public KeyValuePair<string, string> kv { get; set; }
        public KeyValuePair<string, string> txt { get; set; }
        public KeyValuePair<string, string> jpg { get; set; }
        public KeyValuePair<string, string> radartxt { get; set; }
        public List<KeyValuePair<string, string>> radardds { get; set; }
        public List<KeyValuePair<string, string>> languages { get; set; }
        public List<KeyValuePair<string, string>> VehicleScriptList { get; set; }

        public FileInfo file { get; private set; }

        public BSP(FileInfo file)
        {
            this.file = file;

            offsets = new KeyValuePair<int, int>[64];
            bsp = new FileStream(file.FullName, FileMode.Open);
            reader = new BinaryReader(bsp);

            bsp.Seek(4, SeekOrigin.Begin); //skip header
            int bspVer = reader.ReadInt32();

            if (bspVer == 21 && reader.ReadInt32() != 0)
                bsp.Seek(8, SeekOrigin.Begin);

            //gathers an array of offsets (where things are located in the bsp)
            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                offsets[i] = new KeyValuePair<int, int>(reader.ReadInt32(), reader.ReadInt32());
                bsp.Seek(8, SeekOrigin.Current); //skip id and version
            }

            buildEntityList();

            buildEntModelList();
            buildModelList();

            buildParticleList();

            buildEntTextureList();
            buildTextureList();

            buildEntSoundList();

            reader.Close();
            bsp.Close();
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
                            if (!entity.ContainsKey(c[1]))
                                entity.Add(c[1], c[3]);

                            //everything after the hammerid is input/outputs
                            //if (c[1] == "hammerid")
                            //    break;
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
                TextureList[i] = "materials/" + TextureList[i] + ".vmt";

            // find skybox materials
            Dictionary<string, string> worldspawn = entityList.First(item => item["classname"] == "worldspawn");
            if (worldspawn.ContainsKey("skyname"))
                foreach (string s in new string[] { "bk", "dn", "ft", "lf", "rt", "up" })
                {
                    TextureList.Add("materials/skybox/" + worldspawn["skyname"] + s + ".vmt");
                    TextureList.Add("materials/skybox/" + worldspawn["skyname"] + "_hdr" + s + ".vmt");
                }

            // find detail materials
            if (worldspawn.ContainsKey("detailmaterial"))
                TextureList.Add("materials/" + worldspawn["detailmaterial"] + ".vmt");

            // find menu photos
            TextureList.Add("materials/vgui/maps/menu_photos_" + mapname + ".vmt");
        }

        public void buildEntTextureList()
        {
            // builds the list of textures referenced in entities

            EntTextureList = new List<string>();
            foreach (Dictionary<string, string> ent in entityList)
            {
                List<string> materials = new List<string>();
                foreach (KeyValuePair<string, string> prop in ent)
                {
                    //Console.WriteLine(prop.Key + ": " + prop.Value);
                    if (Keys.vmfMaterialKeys.Contains(prop.Key.ToLower()))
                    {
                        materials.Add(prop.Value);
                        if (prop.Key.ToLower().StartsWith("team_icon"))
                            materials.Add(prop.Value + "_locked");
                    }
                }


                // special condition for sprites
                if (ent["classname"].Contains("sprite") && ent.ContainsKey("model"))
                    materials.Add(ent["model"]);

                // special condition for item_teamflag
                if (ent["classname"].Contains("item_teamflag"))
                {
                    if (ent.ContainsKey("flag_trail"))
                    {
                        materials.Add("effects/" + ent["flag_trail"]);
                        materials.Add("effects/" + ent["flag_trail"] + "_red");
                        materials.Add("effects/" + ent["flag_trail"] + "_blu");
                    }
                    if (ent.ContainsKey("flag_icon"))
                    {
                        materials.Add("vgui/" + ent["flag_icon"]);
                        materials.Add("vgui/" + ent["flag_icon"] + "_red");
                        materials.Add("vgui/" + ent["flag_icon"] + "_blu");
                    }
                }

                // format and add materials
                foreach (string material in materials)
                {
                    string materialpath = material;
                    if (!material.EndsWith(".vmt"))
                        materialpath += ".vmt";

                    EntTextureList.Add("materials/" + materialpath);
                }
            }
        }

        public void buildModelList()
        {
            // builds the list of models that are from prop_static

            ModelList = new List<string>();
            // getting information on the gamelump
            int propStaticId = 0;
            bsp.Seek(offsets[35].Key, SeekOrigin.Begin);
            KeyValuePair<int, int>[] GameLumpOffsets = new KeyValuePair<int, int>[reader.ReadInt32()]; // offset/length
            for (int i = 0; i < GameLumpOffsets.Length; i++)
            {
                if (reader.ReadInt32() == 1936749168)
                    propStaticId = i;
                bsp.Seek(4, SeekOrigin.Current); //skip flags and version
                GameLumpOffsets[i] = new KeyValuePair<int, int>(reader.ReadInt32(), reader.ReadInt32());
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

            //dont bother if there's no props, avoid a dividebyzero exception.
            if (propCount <= 0)
                return;

            long propOffset = bsp.Position;
            int byteLength = GameLumpOffsets[propStaticId].Key + GameLumpOffsets[propStaticId].Value - (int)propOffset;
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
                        !ent["classname"].Contains("sprite") &&
                        Keys.vmfModelKeys.Contains(prop.Key))
                        EntModelList.Add(prop.Value);
        }

        public void buildEntSoundList()
        {
            // builds the list of sounds referenced in entities
            char[] special_caracters = new char[] { '*', '#', '@', '>', '<', '^', '(', ')', '}', '$', '!', '?', ' ' };
            EntSoundList = new List<string>();
			foreach (Dictionary<string, string> ent in entityList)
				foreach (KeyValuePair<string, string> prop in ent)
				{
					if (Keys.vmfSoundKeys.Contains(prop.Key))
						EntSoundList.Add("sound/" + prop.Value.Trim(special_caracters));
					//Pack I/O triggered sounds
					else if (prop.Value.Contains("PlayVO"))
					{
						//Parameter value following PlayVO is always either a sound path or an empty string
						List<string> io = prop.Value.Split(',').ToList();
						if (!string.IsNullOrWhiteSpace(io[io.IndexOf("PlayVO") + 1]))
							EntSoundList.Add("sound/" + io[io.IndexOf("PlayVO") + 1].Trim(special_caracters));
					}
					else if (prop.Value.Contains("playgamesound"))
					{
						List<string> io = prop.Value.Split(',').ToList();
						if (!string.IsNullOrWhiteSpace(io[io.IndexOf("playgamesound") + 1]))
							EntSoundList.Add("sound/" + io[io.IndexOf("playgamesound") + 1].Trim(special_caracters));
					}

				}


        }

        public void buildParticleList()
        {
            ParticleList = new List<string>();
            foreach (Dictionary<string, string> ent in entityList)
                foreach (KeyValuePair<string, string> particle in ent)
                     if (particle.Key.ToLower() == "effect_name")
                        ParticleList.Add(particle.Value);
        }
    }
}