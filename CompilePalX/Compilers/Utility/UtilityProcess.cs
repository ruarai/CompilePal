using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using CompilePalX.Compilers.BSPPack;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers.UtilityProcess
{
    class UtilityProcess : CompileProcess
    {
        public UtilityProcess() 
            : base ("UTILITY")
        {
            
        }

        private static bool genParticleManifest;
        private static bool incParticleManifest;
        private static bool incSoundscape;
        private static bool incLevelSounds;
        private static bool excludeDir;
	    private static bool excludeFile;

        private static string gameFolder;
        private static string bspPath;
        private const string keysFolder = "Keys";

        private List<string> sourceDirectories = [];
        private List<string> excludedDirectories = [];
        private List<string> excludedFiles = [];

        public override void Run(CompileContext context, CancellationToken cancellationToken)
        {
            CompileErrors = [];

            if (!CanRun(context)) return;

            genParticleManifest = GetParameterString().Contains("-particlemanifest");
            incParticleManifest = GetParameterString().Contains("-incparticlemanifest");
            incSoundscape = GetParameterString().Contains("-incsoundscape");
            incLevelSounds = GetParameterString().Contains("-inclevelsounds");
            excludeDir = GetParameterString().Contains("-excludedir");
            excludeFile = GetParameterString().Contains("-excludefile");

            //TODO try to find a way to cut down on duplicate processes between utility and pack steps
            try
            {
                CompilePalLogger.LogLine("\nCompilePal - Utilities");

                Keys.vmtTextureKeyWords =
                    File.ReadAllLines(System.IO.Path.Combine(keysFolder, "texturekeys.txt")).ToList();
                Keys.vmtMaterialKeyWords =
                    File.ReadAllLines(System.IO.Path.Combine(keysFolder, "materialkeys.txt")).ToList();
                Keys.vmfSoundKeys = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "vmfsoundkeys.txt")).ToList();
                Keys.vmfMaterialKeys = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "vmfmaterialkeys.txt"))
                    .ToList();
                Keys.vmfModelKeys = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "vmfmodelkeys.txt")).ToList();

                excludedDirectories = [];
                excludedFiles = [];


                CompilePalLogger.LogLine("Finding sources of game content...");
                gameFolder = context.Configuration.GameFolder;
                sourceDirectories = BSPPack.BSPPack.GetSourceDirectories(gameFolder);

                bspPath = context.CopyLocation;

                //Parse parameters to get ignore directories
                if (excludeDir)
                {
                    char[] paramChars = GetParameterString().ToCharArray();
                    List<string> parameters = ParseParameters(paramChars);

                    //Get excluded directories from parameter list
                    foreach (string parameter in parameters)
                    {
                        if (parameter.Contains("excludedir"))
                        {
                            var @dirPath = parameter.Replace("\"", "").Replace("excludedir ", "").TrimEnd(' ');
                            //Test that directory exists
                            if (Directory.Exists(dirPath))
                                excludedDirectories.Add(dirPath);
                            else
                                CompilePalLogger.LogCompileError($"Could not find directory: {dirPath}\n",
                                    new Error($"Could not find directory: {dirPath}\n", ErrorSeverity.Warning));
                        }
                    }
                }

                if (excludeFile)
                {
                    char[] paramChars = GetParameterString().ToCharArray();
                    List<string> parameters = ParseParameters(paramChars);

                    //Get excluded files from parameter list
                    foreach (string parameter in parameters)
                    {
                        if (parameter.Contains("excludefile"))
                        {
                            var @filePath = parameter.Replace("\"", "").Replace("excludefile ", "").Replace('/', '\\')
                                .ToLower().TrimEnd(' ');
                            //Test that file exists
                            if (File.Exists(filePath))
                                excludedFiles.Add(filePath);
                            else
                                CompilePalLogger.LogCompileError($"Could not find file: {filePath}\n",
                                    new Error($"Could not find file: {filePath}\n", ErrorSeverity.Warning));
                        }
                    }
                }

                if (genParticleManifest)
                {
                    if (!File.Exists(bspPath))
                    {
                        throw new FileNotFoundException();
                    }

                    CompilePalLogger.LogLine("Reading BSP...");
                    BSP map = new BSP(new FileInfo(bspPath));

                    ParticleManifest manifest = new ParticleManifest(sourceDirectories, excludedDirectories,
                        excludedFiles, map, bspPath, gameFolder);

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    //Set fields in bsppack so manifest gets detected correctly
                    BSPPack.BSPPack.genParticleManifest = true;
                    BSPPack.BSPPack.particleManifest = manifest.particleManifest;
                }

                if (incParticleManifest)
                {
                    CompilePalLogger.LogLine("Attempting to update particle manifest");
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    bool success = UpdateManifest("_particles.txt");

                    if (!success)
                        CompilePalLogger.LogCompileError("Could not update manifest!\n",
                            new Error("Could not update manifest!", ErrorSeverity.Error));
                }

                if (incLevelSounds)
                {
                    CompilePalLogger.LogLine("Attempting to update level sounds");

                    bool success = UpdateManifest("_level_sounds.txt");

                    if (!success)
                        CompilePalLogger.LogCompileError("Could not update level sounds!\n",
                            new Error("Could not update level sounds!", ErrorSeverity.Error));
                }

                if (incSoundscape)
                {
                    CompilePalLogger.LogLine("Attempting to update soundscape");

                    //Get all script directories
                    List<string> directories = [];

                    foreach (string directory in sourceDirectories)
                        if (Directory.Exists(directory + "\\scripts\\"))
                            directories.Add(directory + "\\scripts\\");

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    bool success = UpdateManifest("soundscapes_", directories, true);

                    if (!success)
                        CompilePalLogger.LogCompileError("Could not update soundscape!\n",
                            new Error("Could not update soundscape!", ErrorSeverity.Error));
                }

            }
            catch (FileNotFoundException)
            {
                CompilePalLogger.LogCompileError($"Could not find {context.CopyLocation}\n",
                    new Error($"Could not find {context.CopyLocation}", ErrorSeverity.Error));
            }
            catch (ThreadAbortException)
            {

            }
            catch (Exception e)
            {
                CompilePalLogger.LogLine("Something broke:");
                CompilePalLogger.LogCompileError($"{e}\n", new Error(e.ToString(), "CompilePal Internal Error", ErrorSeverity.FatalError));
            }
        }

        private static bool UpdateManifest(string manifestType, List<string> directories = null, bool manifestIsAtFrontOfFilename = false)
        {
            bool successfullyIncremented = true;

            //Get name of bsp file
            string bspName = bspPath.Split('\\').Last();

            //Get map directory
            List<string> mapDir;

            if (directories == null)
                mapDir = [bspPath.Replace(bspName, "")];
            else
            {
                mapDir = directories;
            }

            //remove .bsp from end
            bspName = bspName.Remove(bspName.Length - 4, 4);

            //Get mapname without version
            string mapName = bspName.Replace(bspName.Split('_').Last(), "");

            //Get version (ex a, b, or rc)
            string versionIdent = Regex.Split(bspName.Split('_').Last(), @"\d").First();

            int versionNum;
            Int32.TryParse(Regex.Split(bspName, @"\D").Last(), out versionNum);

            //If version num is one move back a version rc->beta, beta->alpha
            if (versionNum == 1)
            {
                bool version0Exists = false;

                //There might be a version 0 of map, so try that first
                versionNum -= 1;

                string oldManifestPathV0 = FindPreviousVersion(mapDir, mapName + versionIdent + versionNum, manifestType, manifestIsAtFrontOfFilename);

                if (File.Exists(oldManifestPathV0))
                {
                    version0Exists = true;

                    //Incremented file should be in same directory as the base file, get directory by removing filename from filepath
                    string directory = oldManifestPathV0.Replace(oldManifestPathV0.Split('\\').Last(), "");

                    if (manifestIsAtFrontOfFilename)
                        File.Copy(oldManifestPathV0, directory + manifestType + bspName + ".txt", true);
                    else
                        File.Copy(oldManifestPathV0, directory + bspName + manifestType, true);

                    CompilePalLogger.LogLine(oldManifestPathV0 + " was used as base file");
                }

                if (!version0Exists)
                {
                    //Release candidate -> beta
                    if (versionIdent.ToLower() == "rc")
                    {
                        versionIdent = "b";

                        //Try to find old manifest name
                        string oldManifestPath = FindPreviousVersion(mapDir, mapName + versionIdent, manifestType, manifestIsAtFrontOfFilename);

                        //Duplicate older manifest with new name
                        if (oldManifestPath != null)
                        {
                            //Incremented file should be in same directory as the base file, get directory by removing filename from filepath
                            string directory = oldManifestPath.Replace(oldManifestPath.Split('\\').Last(), "");

                            if (manifestIsAtFrontOfFilename)
                                File.Copy(oldManifestPath, directory + manifestType + bspName + ".txt", true);
                            else
                                File.Copy(oldManifestPath, directory + bspName + manifestType, true);

                            CompilePalLogger.LogLine(oldManifestPath + " was used as base file");
                        }
                    }
                    //Beta -> alpha
                    else if (versionIdent.ToLower() == "b")
                    {
                        versionIdent = "a";

                        //Try to find old manifest name
                        string oldManifestPath = FindPreviousVersion(mapDir, mapName + versionIdent, manifestType, manifestIsAtFrontOfFilename);

                        //Duplicate older manifest with new name
                        if (oldManifestPath != null)
                        {
                            //Incremented file should be in same directory as the base file, get directory by removing filename from filepath
                            string directory = oldManifestPath.Replace(oldManifestPath.Split('\\').Last(), "");

                            if (manifestIsAtFrontOfFilename)
                                File.Copy(oldManifestPath, directory + manifestType + bspName + ".txt", true);
                            else
                                File.Copy(oldManifestPath, directory + bspName + manifestType, true);

                            CompilePalLogger.LogLine(oldManifestPath + " was used as base file");
                        }

                    }
                    //Alpha
                    else if (versionIdent.ToLower() == "a")
                    {
                        successfullyIncremented = false;
                    }
                }
            }
            else
            {
                //Try to find older version number by just decreasing versionnum
                versionNum -= 1;

                string oldManifestPath = FindPreviousVersion(mapDir, mapName + versionIdent + versionNum, manifestType, manifestIsAtFrontOfFilename);

                if (File.Exists(oldManifestPath))
                {
                    //Incremented file should be in same directory as the base file, get directory by removing filename from filepath
                    string directory = oldManifestPath.Replace(oldManifestPath.Split('\\').Last(), "");

                    if (manifestIsAtFrontOfFilename)
                        File.Copy(oldManifestPath, directory + manifestType + bspName + ".txt", true);
                    else
                        File.Copy(oldManifestPath, directory + bspName + manifestType, true);

                    CompilePalLogger.LogLine(oldManifestPath + " was used as base file");
                }
                else
                {
                    successfullyIncremented = false;
                }
            }

            return successfullyIncremented;
        }


        //Finds previous versions of files, and returns the highest version. Takes into account subversions such as a1b, a1c, etc
        private static string FindPreviousVersion(List<string> mapDirs, string searchString, string manifestType, bool manifestIsAtFrontOfFilename)
        {
            List<BspFileName> candidateFiles = [];

            foreach (string mapDir in mapDirs)
            {
                //Use regex to find candidate files
                string[] files = Directory.GetFiles(mapDir);

                foreach (string file in files)
                {
                    //Get filename
                    string fileName = file.Split('\\').Last();

                    //TODO come up with a regex that detects both manifests with names that come at front and end of file
                    if (manifestIsAtFrontOfFilename)
                    {
                        //\B only works for soundscapes, which come at front of filename, and \b works for everything else, which come after filename. Don't know enough regex to fix so i just split them with a if statement
                        //Regex search for files containing (mapname)_ and is a file that contains a manifest type
                        if (Regex.IsMatch(fileName, $@"\B({searchString})") && fileName.Contains(manifestType))
                        {
                            BspFileName bspFileName = new BspFileName();

                            //Use regex to get versionNum
                            string versionNumString = Regex.Replace(fileName.Replace(manifestType, ""), @"[^1-9]", "");

                            //Get subversion Ex. a1c, or rc2b
                            string subVersion = Regex.Split(fileName.Replace(manifestType, ""), @"(?<=[0-9])").Last();

                            int versionNum;
                            Int32.TryParse(versionNumString, out versionNum);

                            bspFileName.versionNum = versionNum;
                            bspFileName.subVersion = subVersion;
                            bspFileName.file = file;

                            //Store version number and file
                            candidateFiles.Add(bspFileName);
                        }
                    }
                    else
                    {
                        //Regex search for files containing (mapname)_ and is a file that contains a manifest type
                        if (Regex.IsMatch(fileName, $@"\b({searchString})") && fileName.Contains(manifestType))
                        {
                            BspFileName bspFileName = new BspFileName();

                            //Use regex to get versionNum
                            string versionNumString = Regex.Replace(fileName.Replace(manifestType, ""), @"[^1-9]", "");

                            //Get subversion Ex. a1c, or rc2b
                            string subVersion = Regex.Split(fileName.Replace(manifestType, ""), @"(?<=[0-9])").Last();

                            int versionNum;
                            Int32.TryParse(versionNumString, out versionNum);

                            bspFileName.versionNum = versionNum;
                            bspFileName.subVersion = subVersion;
                            bspFileName.file = file;

                            //Store version number and file
                            candidateFiles.Add(bspFileName);
                        }                        
                    }

                }
            }


            //If there are no candidate files return null
            if (candidateFiles.Count == 0)
                return null;

            //Find highest version number of file. Default first file as highest
            BspFileName highestVersion = candidateFiles[0];

            foreach (BspFileName candidateFile in candidateFiles)
            {
                if (candidateFile.versionNum == highestVersion.versionNum)
                {
                    if (String.Compare(candidateFile.subVersion, highestVersion.subVersion) > 0)
                        highestVersion = candidateFile;                    
                }
                else if (candidateFile.versionNum > highestVersion.versionNum)
                {
                    highestVersion = candidateFile;
                }
                    
            }

            if (highestVersion.file != "")
                return (highestVersion.file);

            //Return null if something went wrong
            return null;
        }

		// parses parameters that can contain '-' in their values. Ex. filepaths
		private static List<string> ParseParameters(char[] paramChars)
		{
			List<string> parameters = [];
			bool inQuote = false;
			StringBuilder tempParam = new StringBuilder();

			foreach (var pChar in paramChars)
			{
				if (pChar == '\"')
					inQuote = !inQuote;
				else if (!inQuote && pChar == '-')
				{
					parameters.Add(tempParam.ToString());
					tempParam.Clear();
				}
				else
					tempParam.Append(pChar);

			}

			return parameters;
		}


		private struct BspFileName
        {
            public string file;
            public string subVersion;
            public int versionNum;
        }
    }
}
