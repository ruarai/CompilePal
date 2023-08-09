using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CompilePalX.Compiling;

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
        private static readonly char[] SpecialCaracters = { '*', '#', '@', '>', '<', '^', '(', ')', '}', '$', '!', '?', ' ' };

        public List<Dictionary<string, string>> entityList { get; private set; }

        public List<List<Tuple<string, string>>> entityListArrayForm { get; private set; }

        public List<int>[] modelSkinList { get; private set; }

        public List<string> ModelList { get; private set; }

        public List<string> EntModelList { get; private set; }

        public List<string> ParticleList { get; private set; }

        public List<string> TextureList { get; private set; }
        public List<string> EntTextureList { get; private set; }

        public List<string> EntSoundList { get; private set; }

        public List<string> MiscList { get; private set; }

        // key/values as internalPath/externalPath
        public KeyValuePair<string, string> particleManifest { get; set; }
        public KeyValuePair<string, string> soundscript { get; set; }
        public KeyValuePair<string, string> soundscape { get; set; }
        public KeyValuePair<string, string> detail { get; set; }
        public KeyValuePair<string, string> nav { get; set; }
        public List<KeyValuePair<string, string>> res { get; } = new List<KeyValuePair<string, string>>();
        public KeyValuePair<string, string> kv { get; set; }
        public KeyValuePair<string, string> txt { get; set; }
        public KeyValuePair<string, string> jpg { get; set; }
        public KeyValuePair<string, string> radartxt { get; set; }
        public List<KeyValuePair<string, string>> radardds { get; set; }
        public KeyValuePair<string, string> RadarTablet { get; set; }
        public List<KeyValuePair<string, string>> languages { get; set; }
        public List<KeyValuePair<string, string>> VehicleScriptList { get; set; }
        public List<KeyValuePair<string, string>> EffectScriptList { get; set; }
        public List<string> vscriptList { get; set; }
        public List<KeyValuePair<string, string>> PanoramaMapBackgrounds { get; set; }
        public KeyValuePair<string, string> PanoramaMapIcon { get; set; }

        public FileInfo file { get; private set; }
        private bool isL4D2 = false;
        private int bspVersion;

        public BSP(FileInfo file)
        {
            this.file = file;

            offsets = new KeyValuePair<int, int>[64];
            using (bsp = new FileStream(file.FullName, FileMode.Open))
            using (reader = new BinaryReader(bsp))
            {
                bsp.Seek(4, SeekOrigin.Begin); //skip header
                this.bspVersion = reader.ReadInt32();

                //hack for detecting l4d2 maps
                if (reader.ReadInt32() == 0 && this.bspVersion == 21)
                        isL4D2 = true;

                // reset reader position
                bsp.Seek(-4, SeekOrigin.Current);

                //gathers an array of offsets (where things are located in the bsp)
                for (int i = 0; i < offsets.GetLength(0); i++)
                {
                    // l4d2 has different lump order
                    if (isL4D2)
                    {
                        bsp.Seek(4, SeekOrigin.Current); //skip version
                        offsets[i] = new KeyValuePair<int, int>(reader.ReadInt32(), reader.ReadInt32());
                        bsp.Seek(4, SeekOrigin.Current); //skip id
                    }
                    else
                    {
                        offsets[i] = new KeyValuePair<int, int>(reader.ReadInt32(), reader.ReadInt32());
                        bsp.Seek(8, SeekOrigin.Current); //skip id and version
                    }
                }

                buildEntityList();

                buildEntModelList();
                buildModelList();

                buildParticleList();

                buildEntTextureList();
                buildTextureList();

                buildEntSoundList();

                buildMiscList();
            }
        }

        public void buildEntityList()
        {
            entityList = new List<Dictionary<string, string>>();
            entityListArrayForm = new List<List<Tuple<string, string>>>();

            bsp.Seek(offsets[0].Key, SeekOrigin.Begin);
            byte[] ent = reader.ReadBytes(offsets[0].Value);
            List<byte> ents = new List<byte>();

	        const int LCURLY = 123;
	        const int RCURLY = 125;
	        const int NEWLINE = 10;

            for (int i = 0; i < ent.Length; i++)
            {
	            if (ent[i] == LCURLY && i + 1 < ent.Length)
	            {
		            // if curly isnt followed by newline assume its part of filename
		            if (ent[i + 1] != NEWLINE)
			            ents.Add(ent[i]);
	            }
                if (ent[i] != LCURLY && ent[i] != RCURLY)
					ents.Add(ent[i]);
                else if (ent[i] == RCURLY)
                {
					// if curly isnt followed by newline assume its part of filename
	                if (i + 1 < ent.Length && ent[i + 1] != NEWLINE)
	                {
						ents.Add(ent[i]);
						continue;
	                }


					string rawent = Encoding.ASCII.GetString(ents.ToArray());
                    Dictionary<string, string> entity = new Dictionary<string, string>();
                    var entityArrayFormat = new List<Tuple<string, string>>();
					// split on \n, ignore \n inside of quotes
                    foreach (string s in Regex.Split(rawent, "(?=(?:(?:[^\"]*\"){2})*[^\"]*$)\\n"))
                    {
                        if (s.Count() != 0)
                        {
                            string[] c = s.Split('"');
                            if (!entity.ContainsKey(c[1]))
                                entity.Add(c[1], c[3]);
                            entityArrayFormat.Add(Tuple.Create(c[1], c[3]));
                        }
                    }
                    entityList.Add(entity);
                    entityListArrayForm.Add(entityArrayFormat);
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
                if (TextureList[i].StartsWith("/")) // materials in root level material directory start with /
                    TextureList[i] = "materials" + TextureList[i] + ".vmt";
                else
                    TextureList[i] = "materials/" + TextureList[i] + ".vmt";
            }

            // find skybox materials
            Dictionary<string, string> worldspawn = entityList.First(item => item["classname"] == "worldspawn");
            if (worldspawn.ContainsKey("skyname"))
                foreach (string s in new string[] { "", "bk", "dn", "ft", "lf", "rt", "up" })
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

            List<string> materials = new List<string>();
            HashSet<string> skybox_swappers = new HashSet<string>();

            foreach (Dictionary<string, string> ent in entityList)
            {
                foreach (KeyValuePair<string, string> prop in ent)
                {
                    if (Keys.vmfMaterialKeys.Contains(prop.Key.ToLower()))
                    {
                        materials.Add(prop.Value);
                        if (prop.Key.ToLower().StartsWith("team_icon"))
                            materials.Add(prop.Value + "_locked");
                    }

                }

                if(ent["classname"].Contains("skybox_swapper") && ent.ContainsKey("SkyboxName") )
                {
                    if(ent.ContainsKey("targetname"))
                    {
                        skybox_swappers.Add(ent["targetname"].ToLower());
                    }
                    
                    foreach (string s in new string[] { "", "bk", "dn", "ft", "lf", "rt", "up" })
                    {
                        materials.Add("skybox/" + ent["SkyboxName"] + s + ".vmt");
                        materials.Add("skybox/" + ent["SkyboxName"] + "_hdr" + s + ".vmt");
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

				// special condition for env_funnel. Hardcoded to use sprites/flare6.vmt
				if (ent["classname"].Contains("env_funnel"))
					materials.Add("sprites/flare6.vmt");

				// special condition for env_embers. Hardcoded to use particle/fire.vmt
				if (ent["classname"].Contains("env_embers"))
					materials.Add("particle/fire.vmt");

                //special condition for func_dustcloud and func_dustmotes.  Hardcoded to use particle/sparkles.vmt
                if (ent["classname"].StartsWith("func_dust"))
                    materials.Add("particle/sparkles.vmt");

                // special condition for vgui_slideshow_display. directory paramater references all textures in a folder (does not include subfolders)
                if (ent["classname"].Contains("vgui_slideshow_display"))
	            {
		            if (ent.ContainsKey("directory"))
		            {
			            var directory = $"{GameConfigurationManager.GameConfiguration.GameFolder}/materials/vgui/{ent["directory"]}";
			            if (Directory.Exists(directory))
			            {
				            foreach (var file in Directory.GetFiles(directory))
				            {
					            if (file.EndsWith(".vmt"))
									materials.Add($"/vgui/{ent["directory"]}/{Path.GetFileName(file)}");
				            }
			            }
					}
	            }

            }

            // pack I/O referenced materials
            // need to use array form of entity because multiple outputs with same command can't be stored in dict
            foreach (var ent in entityListArrayForm)
            {
                foreach (var prop in ent)
                {
                    var io = ParseIO(prop.Item2);
                    if (io == null)
                        continue;
                    

                    var (target, command, parameter) = io;

                    switch (command) {
                        case "SetCountdownImage":
                            materials.Add($"vgui/{parameter}");
                            break;
                        case "Command":
                            // format of Command is <command> <parameter>
                            if(!parameter.Contains(' '))
                            {
                                continue;
                            }
                            (command, parameter) = parameter.Split(' ') switch { var param => (param[0], param[1])};
                            if (command == "r_screenoverlay")
                                materials.Add(parameter);
                            break;
                        case "AddOutput":
                            if(!parameter.Contains(' '))
                            {
                                continue;
                            }
                            string k, v;
                            (k,v) = parameter.Split(' ') switch { var a => (a[0], a[1])};

                            // support packing mats when using addoutput to change skybox_swappers
                            if(skybox_swappers.Contains(target.ToLower()) && k.ToLower() == "skyboxname")
                            {
                                foreach (string s in new string[] { "", "bk", "dn", "ft", "lf", "rt", "up" })
                                {
                                    materials.Add("skybox/" + v + s + ".vmt");
                                    materials.Add("skybox/" + v + "_hdr" + s + ".vmt");
                                }
                            }
                            break;
                    }
                }
            }

            // format and add materials
            EntTextureList = new List<string>();
            foreach (string material in materials)
            {
                string materialpath = material;
                if (!material.EndsWith(".vmt") && !materialpath.EndsWith(".spr"))
                    materialpath += ".vmt";

                EntTextureList.Add("materials/" + materialpath);
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

            // bsp v25 uses ints instead of shorts for leaf lump
            if (this.bspVersion == 25)
                bsp.Seek(leafCount * sizeof(int), SeekOrigin.Current);
            else
                bsp.Seek(leafCount * sizeof(short), SeekOrigin.Current);

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
	        {
				foreach (KeyValuePair<string, string> prop in ent)
				{
					if (ent["classname"].StartsWith("func"))
					{
						if (prop.Key == "gibmodel")
							EntModelList.Add(prop.Value);
					}
					else if (!ent["classname"].StartsWith("trigger") &&
						!ent["classname"].Contains("sprite"))
					{
						if (Keys.vmfModelKeys.Contains(prop.Key))
							EntModelList.Add(prop.Value);
						// item_sodacan is hardcoded to models/can.mdl
						// env_beverage spawns item_sodacans
						else if (prop.Value == "item_sodacan" || prop.Value == "env_beverage")
							EntModelList.Add("models/can.mdl");
						// tf_projectile_throwable is hardcoded to models/props_gameplay/small_loaf.mdl
						else if (prop.Value == "tf_projectile_throwable")
							EntModelList.Add("models/props_gameplay/small_loaf.mdl");
					}

				}
			}

            // pack I/O referenced models
            // need to use array form of entity because multiple outputs with same command can't be stored in dict
            foreach (var ent in entityListArrayForm)
            {
                foreach (var prop in ent)
                {
                    var io = ParseIO(prop.Item2);
                    if (io == null)
                        continue;

                    var (target, command, parameter) = io;

                    if (command == "SetModel")
                        EntModelList.Add(parameter);
                }
            }
        }

        public void buildEntSoundList()
        {
            // builds the list of sounds referenced in entities
            EntSoundList = new List<string>();
			foreach (Dictionary<string, string> ent in entityList)
				foreach (KeyValuePair<string, string> prop in ent)
				{
					if (Keys.vmfSoundKeys.Contains(prop.Key.ToLower()))
						EntSoundList.Add("sound/" + prop.Value.Trim(SpecialCaracters));
				}

            // pack I/O referenced sounds
            // need to use array form of entity because multiple outputs with same command can't be stored in dict
            foreach (var ent in entityListArrayForm)
            {
                foreach (var prop in ent)
                {
                    var io = ParseIO(prop.Item2);
                    if (io == null)
                        continue;

                    var (target, command, parameter) = io;
                    if (command == "PlayVO")
                    {
                        //Parameter value following PlayVO is always either a sound path or an empty string
                        if (!string.IsNullOrWhiteSpace(parameter)) 
                            EntSoundList.Add($"sound/{parameter}");
                    }
                    else if (command == "Command")
                    {
                        // format of Command is <command> <parameter>
                        if(!parameter.Contains(' '))
                            continue;
                        
                        (command, parameter) = parameter.Split(' ') switch { var param => (param[0], param[1])};

                        if (command == "play" || command == "playgamesound" )
                            EntSoundList.Add($"sound/{parameter}");
                    }
                }
            }
        }
        // color correction, etc.
        public void buildMiscList()
        {
            MiscList = new List<string>();

            // find color correction files
            foreach (Dictionary<string, string> cc in entityList.Where(item => item["classname"].StartsWith("color_correction")))
                if (cc.ContainsKey("filename"))
                    TextureList.Add(cc["filename"]);

            // pack I/O referenced TF2 upgrade files
            // need to use array form of entity because multiple outputs with same command can't be stored in dict
            foreach (var ent in entityListArrayForm)
            {
                foreach (var prop in ent)
                {
                    var io = ParseIO(prop.Item2);

                    if (io == null) continue;

                    var (target, command, parameter) = io;
                    if (command.ToLower() != "setcustomupgradesfile") continue;

                    MiscList.Add(parameter);

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

        /// <summary>
        /// Parses an IO string for the command and parameter. If the command is "AddOutput", it is parsed returns target, command, parameter 
        /// </summary>
        /// <param name="property">Entity property</param>
        /// <returns>Tuple containing (target, command, parameter)</returns>
        private Tuple<string, string, string>? ParseIO(string property)
        {
            // io is split by unicode escape char
            if (!property.Contains("\u001b"))
            {
                return null;
            }

            // format: <target>\u001b<target input>\u001b<parameter>\u001b<delay>\u001b<only once>
            var io = property.Split("\u001b");
            if (io.Length != 5)
            {
                CompilePalLogger.LogCompileError($"Failed to decode IO, ignoring: {property}\n", new Error($"Failed to decode IO, ignoring: {property}\n", ErrorSeverity.Warning));
                return null;
            }

            var targetInput = io[1];
            var parameter = io[2];

            // AddOutput dynamically adds I/O to other entities, parse it to get input/parameter
            if (targetInput == "AddOutput")
            {
                // AddOutput format: <output name> <target name>:<input name>:<parameter>:<delay>:<max times to fire> or simple form <key> <value>
                // only need to convert complex form into simple form
                if (parameter.Contains(':'))
                {
                    var splitIo = parameter.Split(':');
                    if (splitIo.Length < 3)
                    {
                        CompilePalLogger.LogCompileError($"Failed to decode AddOutput, format may be incorrect: {property}\n", new Error($"Failed to decode AddOutput, format may be incorrect: {property}\n", ErrorSeverity.Warning));
                        return null;
                    }

                    targetInput = splitIo[1];
                    parameter = splitIo[2];
                }
            }

            return new Tuple<string, string, string>(io[0], targetInput, parameter);
        }
    }
}
