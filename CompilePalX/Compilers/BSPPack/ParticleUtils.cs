using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers.BSPPack
{
    public class PCF
    {
        //Class to hold information about a pcf
        public string FilePath;

        public int BinaryVersion;
        public int PcfVersion;

        public int NumDictStrings;
        public List<string> StringDict = new List<string>();

        public List<string> ParticleNames = new List<string>();
        public List<string> MaterialNames = new List<string>();
        public List<string> ModelNames = new List<string>();


        public List<string> GetModelNames()
        {
            List<string> modelList = new List<string>();
            //All strings including model names are stored in string dict for binary 4+
            //TODO I think only binary 4+ support models, but if not we need to implement a method to read them for lower versions
            foreach (string s in StringDict)
            {
                if (s.EndsWith(".mdl"))
                {
                    modelList.Add(s);
                }
            }
            ModelNames = modelList;

            return modelList;
        }

        //All strings including materials are stored in string dict of binary v4 pcfs
        public List<string> GetMaterialNamesV4()
        {
            List<string> materialNames = new List<string>();

            foreach (string s in StringDict)
            {

                if (s.EndsWith(".vmt") || s.EndsWith(".vtf"))
                {
                    materialNames.Add(s);
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
            catch (FileNotFoundException e)
            {
                CompilePalLogger.LogLine($"ParticleUtils: {filePath} not found");
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
                int typeNameIndex = reader.ReadUInt16();
                string typeName = pcf.StringDict[typeNameIndex];

                string elementName = "";

                if (pcf.BinaryVersion != 4 && pcf.BinaryVersion != 5)
                {
                    elementName = ReadNullTerminatedString(fs, reader);
                    //Skip data signature
                    fs.Seek(16, SeekOrigin.Current);
                }
                else if (pcf.BinaryVersion == 4)
                {
                    int elementNameIndex = reader.ReadUInt16();
                    elementName = pcf.StringDict[elementNameIndex];
                    fs.Seek(16, SeekOrigin.Current);
                }
                else if (pcf.BinaryVersion == 5)
                {
                    int elementNameIndex = reader.ReadUInt16();
                    elementName = pcf.StringDict[elementNameIndex];
                    fs.Seek(20, SeekOrigin.Current);
                }
                
                //Get particle names
                if (typeName == "DmeParticleSystemDefinition")
                    pcf.ParticleNames.Add(elementName);

            }

            //Test to see if any particles match a target particle
            bool FindTargetParticle()
            {
                foreach (string particleName in pcf.ParticleNames)
                {
                    foreach (string targetParticle in targetParticles)
                    {
                        if (particleName == targetParticle)
                        {
                            return true;
                        }
                    }
                }

                //If target particle is not in pcf dont read it
                reader.Close();
                fs.Close();

                return false;
            }

            bool containsParticle = FindTargetParticle();
            if (!containsParticle)
                return null;

            reader.Close();
            fs.Close();

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
            catch (FileNotFoundException e)
            {
                CompilePalLogger.LogLine($"ParticleUtils: {filePath} not found");
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
                int typeNameIndex = reader.ReadUInt16();
                string typeName = pcf.StringDict[typeNameIndex];

                string elementName = "";

                if (pcf.BinaryVersion != 4 && pcf.BinaryVersion != 5)
                {
                    elementName = ReadNullTerminatedString(fs, reader);
                    //Skip data signature
                    fs.Seek(16, SeekOrigin.Current);
                }
                else if (pcf.BinaryVersion == 4)
                {
                    int elementNameIndex = reader.ReadUInt16();
                    elementName = pcf.StringDict[elementNameIndex];
                    fs.Seek(16, SeekOrigin.Current);
                }
                else if (pcf.BinaryVersion == 5)
                {
                    int elementNameIndex = reader.ReadUInt16();
                    elementName = pcf.StringDict[elementNameIndex];
                    fs.Seek(20, SeekOrigin.Current);
                }
                
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
                    pcf.MaterialNames.AddRange(pcf.MaterialNames);

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
                    int typeID = reader.ReadUInt16();
                    int attributeType = reader.ReadByte();
                    string attributeTypeName = pcf.StringDict[typeID];

                    int count = (attributeType > 14) ? reader.ReadInt32() : 1;
                    attributeType = (attributeType > 14) ? attributeType - 14 : attributeType;

                    int[] typelength = { 0, 4, 4, 4, 1, 1, 4, 4, 4, 8, 12, 16, 12, 16, 64 };

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
            List<byte> verString = new List<byte>();
            byte v;
            do
            {
                v = reader.ReadByte();
                verString.Add(v);
            } while (v != '\0' && fs.Position != fs.Length);

            return Encoding.ASCII.GetString(verString.ToArray()).Trim('\0');
        }

    }

    //TODO add parameter for updating manifest to next version and a manual override for particles
    class ParticleManifest
    {
        //Class responsible for holding information about particles
        private List<PCF> particles;
        private string internalPath = "particles/";
        private string filepath;
        private string baseDirectory;

        public ParticleManifest (List<string> sourceDirectories, BSP map, string bspPath, string gameFolder)
        {
            CompilePalLogger.LogLine("Generating Particle Manifest...");

            baseDirectory = gameFolder + "/";

            particles = new List<PCF>();

            //Search directories for pcf and find particles that match used particle names
            //TODO multithread this?
            foreach (string sourceDirectory in sourceDirectories)
            {
                string externalPath = sourceDirectory + "/" + internalPath;
                if (Directory.Exists(externalPath))

                    foreach (string file in Directory.GetFiles(externalPath))
                    {
                        if (file.EndsWith(".pcf"))
                        {
                            PCF pcf = ParticleUtils.IsTargetParticle(file, map.ParticleList);
                            if (pcf != null)
                                particles.Add(pcf);
                        }
                    }
            }

            if (particles == null || particles.Count == 0)
                return;

            //Check for pcfs that contain the same particle name
            List<ParticleConflict> conflictingParticles = new List<ParticleConflict>();
            if (particles.Count != 1)
            {
                for (int i = 0; i < particles.Count - 1; i++)
                {
                    for (int j = i + 1; j < particles.Count; j++)
                    {
                        ParticleConflict pc = new ParticleConflict(particles[i], particles[j]);

                        //Create a list of names that intersect between the 2 lists
                        List<string> conflictingNames = particles[i].ParticleNames.Intersect(particles[j].ParticleNames).ToList();

                        if (conflictingNames.Count != 0)
                        {
                            pc.conflictingNames = conflictingNames;
                            conflictingParticles.Add(pc);
                        }
                            
                    }
                }
            }

            //Sort conflicts so larger conflicts appear first which should reduce # of selections user makes


            //Solve conflicts
            if (conflictingParticles.Count != 0)
            {
                List<PCF> newParticles = new List<PCF>();

                //Remove particle if it is in a particle conflict, readd back when conflict is manually resolved
                //Cant edit the list as its being enumerated, so add everything but conflicting particles to new list and transfer it to old list
                foreach (ParticleConflict conflictParticle in conflictingParticles)
                {
                    foreach (PCF particle in particles)
                    {
                        if (particle.FilePath != conflictParticle.conflictingFiles.Item1 &&
                            particle.FilePath != conflictParticle.conflictingFiles.Item2)
                            newParticles.Add(particle);
                    }

                }

                particles = newParticles;
                newParticles.Clear();
                
                List<PCF> resolvedConflicts = new List<PCF>();

                //Bring up conflict window if conflicts exist
                //Have to run on STAthread
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    //Make taskbar icon red
                    ProgressManager.ErrorProgress();

                    //Create window
                    ConflictWindow cw = new ConflictWindow(conflictingParticles);
                    cw.ShowDialog();

                    //Get resolved conflicts
                    resolvedConflicts = cw.selectedPCFS;
                });

                //Add resolved conflicts back into particle list
                particles.AddRange(resolvedConflicts);
            }

            //Remove duplicates
            particles = particles.Distinct().ToList();

            //Generate manifest file
            filepath = bspPath.Remove(bspPath.Length - 4, 4) + "_particles.txt";

            //Write manifest
            using (StreamWriter sw = new StreamWriter(filepath))
            {
                sw.WriteLine("particles_manifest");
                sw.WriteLine("{");

                foreach (PCF particle in particles)
                {
                    string internalParticlePath = particle.FilePath.Replace(baseDirectory, "");
                    sw.WriteLine($"      \"file\"    !\"{internalParticlePath}\"");
                    CompilePalLogger.LogLine($"Particle added to manifest: {internalParticlePath}");
                }

                sw.WriteLine("}");
            }

            string internalDirectory = filepath.Replace(baseDirectory, "");
            map.particleManifest = new KeyValuePair<string, string>(internalDirectory, filepath);

        }
    }

    public class ParticleConflict
    {
        public Tuple<string, string> conflictingFiles;
        public Tuple<PCF, PCF> pcfs;
        public List<string> conflictingNames;

        //Used for sorting
        public int numConflicts;

        public ParticleConflict(PCF pcf1, PCF pcf2)
        {
            pcfs = new Tuple<PCF, PCF>(pcf1, pcf2);
            conflictingFiles = new Tuple<string, string>(pcf1.FilePath, pcf2.FilePath);
            conflictingNames = new List<string>();
        }
    }
}
