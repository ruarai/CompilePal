﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CompilePalX.Compilers.BSPPack;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers.UtilityProcess
{
    public class PCF
    {
        //Class to hold information about a pcf
        public string FilePath;

        public int BinaryVersion;
        public int PcfVersion;

        public int NumDictStrings;
        public List<string> StringDict = [];

        public List<string> ParticleNames = [];
        public List<string> MaterialNames = [];
        public List<string> ModelNames = [];


        public List<string> GetModelNames()
        {
            List<string> modelList = [];
            //All strings including model names are stored in string dict for binary 4+
            //TODO I think only binary 4+ support models, but if not we need to implement a method to read them for lower versions
            foreach (string s in StringDict)
            {
                if (s.EndsWith(".mdl"))
                {
                    modelList.Add(Path.Combine("models", s));
                }
            }
            ModelNames = modelList;

            return modelList;
        }

        //All strings including materials are stored in string dict of binary v4 pcfs
        public List<string> GetMaterialNamesV4()
        {
            List<string> materialNames = [];

            foreach (string s in StringDict)
            {
                if (s.EndsWith(".vmt") || s.EndsWith(".vtf"))
                {
                    materialNames.Add(Path.Combine("materials", s));
                }
            }
            return materialNames;
        }
        
    }

    public static class ParticleUtils
    {
        //Partially reads particle to get particle name to determine if it is a target particle
        //Returns null if not target particle
        public static PCF IsTargetParticle(string filePath, List<string> targetParticles)
        {
            FileStream fs;
            try
            {
                fs = new FileStream(filePath, FileMode.Open);
            }
            catch (FileNotFoundException)
            {
                CompilePalLogger.LogCompileError($"Could not find {filePath}\n", new Error($"Could not find {filePath}", ErrorSeverity.Error));
                return null;
            }

            PCF pcf = new PCF();
            BinaryReader reader = new BinaryReader(fs);

            pcf.FilePath = filePath;

            //Get Magic String
            string magicString = ReadNullTerminatedString(fs, reader);

            //Throw away unneccesary info
            magicString = magicString.Replace("<!-- dmx encoding binary ", "");
            magicString = magicString.Replace(" -->", "");

            //Extract info from magic string
            string[] magicSplit = magicString.Split(' ');
            
            //Store binary and pcf versions
            Int32.TryParse(magicSplit[0], out pcf.BinaryVersion); 
            Int32.TryParse(magicSplit[3], out pcf.PcfVersion);

            //Different versions have different stringDict sizes
            if (pcf.BinaryVersion != 4 && pcf.BinaryVersion != 5)
            {
                pcf.NumDictStrings = reader.ReadInt16(); //Read as short
            }
            else
            {
                pcf.NumDictStrings = reader.ReadInt32(); //Read as int
            }

            //Add strings to string dict
            for (int i = 0; i < pcf.NumDictStrings; i++)
                pcf.StringDict.Add(ReadNullTerminatedString(fs, reader));

            //Read element dict for particle names
            int numElements = reader.ReadInt32();
            for (int i = 0; i < numElements; i++)
            {
                int typeNameIndex;
                if (pcf.BinaryVersion == 5)
                    typeNameIndex = (int)reader.ReadUInt32();
                else
                    typeNameIndex = reader.ReadUInt16();

                string typeName = pcf.StringDict[typeNameIndex];

                string elementName = "";

                if (pcf.BinaryVersion != 4 && pcf.BinaryVersion != 5)
                {
                    elementName = ReadNullTerminatedString(fs, reader);
                }
                else if (pcf.BinaryVersion == 4)
                {
                    int elementNameIndex = reader.ReadUInt16();
                    elementName = pcf.StringDict[elementNameIndex];
                }
                else if (pcf.BinaryVersion == 5)
                {
                    //Structure is either:
                    //ushort nameIndex
                    //ushort descIndex //All checked pcfs 5.2 had this set as 00 00 for all cases
                    //
                    //or
                    //uint nameIndex
                    //
                    //If it's the second option in order to use the last 2 bytes there would need to be over 65535 strings in the stringDict.
                    //Since it's unknown which structure is correct it's safer to read it as ushort and skip next 2 bytes
                    int elementNameIndex = reader.ReadUInt16();
                    elementName = pcf.StringDict[elementNameIndex];
                    fs.Seek(2, SeekOrigin.Current);
                }

                //Skip data signature
                fs.Seek(16, SeekOrigin.Current);

                //Get particle names
                if (typeName == "DmeParticleSystemDefinition")
                    pcf.ParticleNames.Add(elementName);

            }

            bool containsParticle = pcf.ParticleNames
                                    .Intersect(targetParticles, StringComparer.OrdinalIgnoreCase)
                                    .Any();

            //If target particle is not in pcf dont read it
            reader.Close();
            fs.Close();

            if (!containsParticle)
                return null;

            return pcf;
        }

        //Fully reads particle
        public static PCF ReadParticle(string filePath)
        {
            FileStream fs;
            try
            {
                fs = new FileStream(filePath, FileMode.Open);
            }
            catch (FileNotFoundException)
            {
                CompilePalLogger.LogCompileError($"Could not find {filePath}\n", new Error($"Could not find {filePath}", ErrorSeverity.Error));
                return null;
            }

            PCF pcf = new PCF();
            BinaryReader reader = new BinaryReader(fs);

            pcf.FilePath = filePath;

            //Get Magic String
            string magicString = ReadNullTerminatedString(fs, reader);

            //Throw away unneccesary info
            magicString = magicString.Replace("<!-- dmx encoding binary ", "");
            magicString = magicString.Replace(" -->", "");

            //Extract info from magic string
            string[] magicSplit = magicString.Split(' ');
            
            //Store binary and pcf versions
            Int32.TryParse(magicSplit[0], out pcf.BinaryVersion); 
            Int32.TryParse(magicSplit[3], out pcf.PcfVersion);

            //Different versions have different stringDict sizes
            if (pcf.BinaryVersion != 4 && pcf.BinaryVersion != 5)
            {
                pcf.NumDictStrings = reader.ReadInt16(); //Read as short
            }
            else
            {
                pcf.NumDictStrings = reader.ReadInt32(); //Read as int
            }

            //Add strings to string dict
            for (int i = 0; i < pcf.NumDictStrings; i++)
                pcf.StringDict.Add(ReadNullTerminatedString(fs, reader));

            //Read element dict for particle names
            int numElements = reader.ReadInt32();
            for (int i = 0; i < numElements; i++)
            {
                int typeNameIndex;
                if (pcf.BinaryVersion == 5)
                    typeNameIndex = (int)reader.ReadUInt32();
                else
                    typeNameIndex = reader.ReadUInt16();

                string typeName = pcf.StringDict[typeNameIndex];

                string elementName = "";

                if (pcf.BinaryVersion != 4 && pcf.BinaryVersion != 5)
                {
                    elementName = ReadNullTerminatedString(fs, reader);
                }
                else if (pcf.BinaryVersion == 4)
                {
                    int elementNameIndex = reader.ReadUInt16();
                    elementName = pcf.StringDict[elementNameIndex];
                }
                else if (pcf.BinaryVersion == 5)
                {
                    //Structure is either:
                    //ushort nameIndex
                    //ushort descIndex //All checked pcfs 5.2 had this set as 00 00 for all cases
                    //
                    //or
                    //uint nameIndex
                    //
                    //If it's the second option in order to use the last 2 bytes there would need to be over 65535 strings in the stringDict.
                    //Since it's unknown which structure is correct it's safer to read it as ushort and skip next 2 bytes
                    int elementNameIndex = reader.ReadUInt16();
                    elementName = pcf.StringDict[elementNameIndex];
                    fs.Seek(2, SeekOrigin.Current);
                }

                //Skip data signature
                fs.Seek(16, SeekOrigin.Current);

                //Get particle names
                if (typeName == "DmeParticleSystemDefinition")
                    pcf.ParticleNames.Add(elementName);

            }

            if (pcf.BinaryVersion == 4 || pcf.BinaryVersion == 5)
            {
                //Can extract all neccesary data from string dict

                //Add materials and models to the master list
                List<string> materialNames = pcf.GetMaterialNamesV4();
                if (materialNames != null && materialNames.Count != 0)
                    pcf.MaterialNames.AddRange(materialNames);

                List<string> modelNames = pcf.GetModelNames();
                if (modelNames != null && modelNames.Count != 0)
                    pcf.ModelNames.AddRange(modelNames);

                reader.Close();
                fs.Close();
                return pcf;
            }

            //Have to read element attributes to get materials for binary version under 4

            //Read Element Attributes
            for (int a = 0; a < numElements; a++)
            {
                int numElementAttribs = reader.ReadInt32();
                for (int n = 0; n < numElementAttribs; n++)
                {
                    int typeID;
                    if (pcf.BinaryVersion == 5)
                        typeID = (int)reader.ReadUInt32();
                    else
                        typeID = reader.ReadUInt16();

                    int attributeType = reader.ReadByte();
                    string attributeTypeName = pcf.StringDict[typeID];

                    int count = (attributeType > 14) ? reader.ReadInt32() : 1;
                    attributeType = (attributeType > 14) ? attributeType - 14 : attributeType;

                    int[] typelength = [0, 4, 4, 4, 1, 1, 4, 4, 4, 8, 12, 16, 12, 16, 64];

                    switch (attributeType)
                    {
                        case 5:
                            string material = ReadNullTerminatedString(fs, reader);
                            if (attributeTypeName == "material")
                                pcf.MaterialNames.Add("materials/" + material);
                            break;

                        case 6:
                            for (int i = 0; i < count; i++)
                            {
                                uint len = reader.ReadUInt32();
                                fs.Seek(len, SeekOrigin.Current);
                            }
                            break;

                        default:
                            fs.Seek(typelength[attributeType] * count, SeekOrigin.Current);
                            break;
                            
                    }
                }
            }

            reader.Close();
            fs.Close();

            return pcf;
        }


        private static string ReadNullTerminatedString(FileStream fs, BinaryReader reader)
        {
            List<byte> verString = [];
            byte v;
            do
            {
                v = reader.ReadByte();
                verString.Add(v);
            } while (v != '\0' && fs.Position != fs.Length);

            return Encoding.ASCII.GetString(verString.ToArray()).Trim('\0');
        }

    }

    class ParticleManifest
    {
        //Class responsible for holding information about particles
        private List<PCF> particles;
        private string internalPath = "particles\\";
        private string filepath;
        private string baseDirectory;

        public KeyValuePair<string, string> particleManifest { get; private set; }

        public ParticleManifest (List<string> sourceDirectories, List<string> ignoreDirectories, List<string> excludedFiles, BSP map, string bspPath, string gameFolder)
        {
            CompilePalLogger.LogLine("Generating Particle Manifest...");

            baseDirectory = gameFolder + "\\";

            particles = [];

            //Append /particles to all source directories
            //Remove those that don't exist
            //Add any directories that may be within remaining ones recursively
            List<string> particleDirectories = [.. sourceDirectories
                                                .Select(s => Path.Combine(s, internalPath))
                                                .Where(Directory.Exists)
                                                .SelectMany(path =>
                                                    new[] { path }
                                                    .Concat(Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                                                    )
                                                ];

            //Search directories for pcf and find particles that match used particle names
            //TODO multithread this?
            foreach (string dir in particleDirectories)
            {
                if (ignoreDirectories.Contains(dir.TrimEnd('/','\\'), StringComparer.OrdinalIgnoreCase))
                    continue;
    
                foreach (string file in Directory.GetFiles(dir))
                {
                    if (file.EndsWith(".pcf") && !excludedFiles.Contains(file.ToLower()))
                    {
                        PCF? pcf = ParticleUtils.IsTargetParticle(file, map.ParticleList);
                        if (pcf != null && !particles.Exists(p => p.FilePath == pcf.FilePath))
                            particles.Add(pcf);
                    }
                }
            }

            if (particles == null || particles.Count == 0)
            {
                CompilePalLogger.LogCompileError("Could not find any PCFs that contained used particles!\n", new Error("Could not find any PCFs that contained used particles!\n", ErrorSeverity.Warning));
                return;
            }
                

            //Check for pcfs that contain the same particle name
            //List<ParticleConflict> conflictingParticles = new List<ParticleConflict>();
            List<PCF> conflictingParticles = [];
            if (particles.Count > 1)
            {
                for (int i = 0; i < particles.Count - 1; i++)
                {
                    for (int j = i + 1; j < particles.Count; j++)
                    {

                        //Create a list of names that intersect between the 2 lists
                        List<string> conflictingNames = particles[i].ParticleNames.Intersect(particles[j].ParticleNames).ToList();

                        if (conflictingNames.Count != 0 && particles[i].FilePath != particles[j].FilePath)
                        {
                            //pc.conflictingNames = conflictingNames;
                            conflictingParticles.Add(particles[i]);
                            conflictingParticles.Add(particles[j]);
                        }
                            
                    }
                }
            }

            //Solve conflicts
            if (conflictingParticles.Count != 0)
            {

                //Remove particle if it is in a particle conflict, add back when conflict is manually resolved
                foreach (PCF conflictParticle in conflictingParticles)
                {
                    particles.Remove(conflictParticle);
                }
                
                List<PCF> resolvedConflicts = [];

                //Bring up conflict window
                //Have to run on STAthread
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    //Make taskbar icon red
                    ProgressManager.ErrorProgress();

                    //Create window
                    ConflictWindow cw = new ConflictWindow(conflictingParticles, map.ParticleList);
                    cw.ShowDialog();

                    //Get resolved conflicts
                    resolvedConflicts = cw.selectedPCFS;
                });

                //Add resolved conflicts back into particle list
                particles.AddRange(resolvedConflicts);
            }

            //Remove duplicates
            particles = particles.Distinct().ToList();

            //Dont create particle manifest if there is no particles
            if (particles.Count == 0)
                return;
            
            //Generate manifest file
            filepath = bspPath.Remove(bspPath.Length - 4, 4) + "_particles.txt";

            //Write manifest
            using (StreamWriter sw = new StreamWriter(filepath))
            {
                sw.WriteLine("particles_manifest");
                sw.WriteLine("{");

                foreach (string source in sourceDirectories)
                {
                    foreach (PCF particle in particles)
                    {
                        if (particle.FilePath.StartsWith(source + "\\" + internalPath))
                        {
                            // add 1 for the backslash between source dir and particle path
                            string internalParticlePath = particle.FilePath.Remove(0, source.Length + 1);

                            sw.WriteLine($"      \"file\"    \"!{internalParticlePath}\"");
                            CompilePalLogger.LogLine($"PCF added to manifest: {internalParticlePath}");
                        }
                    }
                }

                sw.WriteLine("}");
            }

            string internalDirectory = filepath;
            if(filepath.StartsWith(baseDirectory, StringComparison.CurrentCultureIgnoreCase))
            {
                internalDirectory = filepath[baseDirectory.Length..];
            }
            //Store internal/external dir so it can be packed
            particleManifest = new KeyValuePair<string, string>(internalDirectory, filepath);
        }
    }
}
