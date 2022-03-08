using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers.BSPPack
{
    static class Keys
    {
        public static List<string> vmfSoundKeys;
        public static List<string> vmfModelKeys;
        public static List<string> vmfMaterialKeys;
        public static List<string> vmtTextureKeyWords;
        public static List<string> vmtMaterialKeyWords;
    }

    class BSPPack : CompileProcess
    {
        public BSPPack()
            : base("PACK")
        {

        }

        private static string bspZip;
        private static string vpk;
        private static string gameFolder;
        private static string bspPath;

        private const string keysFolder = "Keys";

        private static bool verbose;
        private static bool dryrun;
        private static bool renamenav;
        private static bool include;
        private static bool includeDir;
        private static bool exclude;
        private static bool excludeDir;
        private static bool excludevpk;
        private static bool packvpk;
        private static bool includefilelist;
        private static bool usefilelist;
        public static bool genParticleManifest;

        public static KeyValuePair<string, string> particleManifest;

        private List<string> sourceDirectories = new List<string>();
        private string outputFile = "BSPZipFiles\\files.txt";

        public override void Run(CompileContext context)
        {
            CompileErrors = new List<Error>();

            verbose = GetParameterString().Contains("-verbose");
            dryrun = GetParameterString().Contains("-dryrun");
            renamenav = GetParameterString().Contains("-renamenav");
            include = Regex.IsMatch(GetParameterString(), @"-include\b"); // ensures it doesnt match -includedir
            includeDir = GetParameterString().Contains("-includedir");
            exclude = Regex.IsMatch(GetParameterString(), @"-exclude\b"); // ensures it doesnt match -excludedir
            excludeDir = GetParameterString().Contains("-excludedir");
            excludevpk = GetParameterString().Contains("-excludevpk");
            packvpk = GetParameterString().Contains("-vpk");
            includefilelist = GetParameterString().Contains("-includefilelist");
            usefilelist = GetParameterString().Contains("-usefilelist");

            char[] paramChars = GetParameterString().ToCharArray();
            List<string> parameters = ParseParameters(paramChars);

            List<string> includeFiles = new List<string>();
            List<string> excludeFiles = new List<string>();
            List<string> excludeDirs = new List<string>();
            List<string> excludedVpkFiles = new List<string>();

            try
            {
                CompilePalLogger.LogLine("\nCompilePal - Automated Packaging");
                bspZip = context.Configuration.BSPZip;
                vpk = context.Configuration.VPK;
                gameFolder = context.Configuration.GameFolder;
                bspPath = context.CopyLocation;

                if (!File.Exists(bspPath))
                {
                    throw new FileNotFoundException();
                }

                // manually passing in a file list
                if (usefilelist)
                {
                    var fileListParam = parameters.First(p => p.StartsWith("usefilelist")).Split(new[]{" "}, 2, StringSplitOptions.None);
                    if (fileListParam.Length > 1 && !string.IsNullOrWhiteSpace(fileListParam[1]))
                    {
                        outputFile = fileListParam[1];
                        if (!File.Exists(outputFile))
                        {
                            CompilePalLogger.LogCompileError($"Could not find file list {outputFile}, exiting pack step\n", new Error($"Could not find file list {outputFile}, exiting pack step\n", ErrorSeverity.Error));
                            return;
                        }

                        CompilePalLogger.LogLine($"Using file list {outputFile}");
                        PackFileList(context, outputFile);
                        return;
                    }

                    CompilePalLogger.LogCompileError("No file list set, exiting pack step\n", new Error("No file list set, exiting  pack step", ErrorSeverity.Error));
                    return;
                }

                outputFile = "BSPZipFiles\\files.txt";

                Keys.vmtTextureKeyWords = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "texturekeys.txt")).ToList();
                Keys.vmtMaterialKeyWords = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "materialkeys.txt")).ToList();
                Keys.vmfSoundKeys = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "vmfsoundkeys.txt")).ToList();
                Keys.vmfMaterialKeys = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "vmfmaterialkeys.txt")).ToList();
                Keys.vmfModelKeys = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "vmfmodelkeys.txt")).ToList();

                // get manually included files
                if (include)
                {
                    //Get included files from parameter list
                    foreach (string parameter in parameters)
                    {
                        if (parameter.Contains("include"))
                        {
                            var filePath = parameter.Replace("\"", "").Replace("include ", "").TrimEnd(' ');
                            //Test that file exists
                            if (File.Exists(filePath))
                                includeFiles.Add(filePath);
                            else
                                CompilePalLogger.LogCompileError($"Could not find file: {filePath}\n", new Error($"Could not find file: {filePath}",$"Could not find file: {filePath}", ErrorSeverity.Caution));
                        }
                    }
                }

                // get manually included files in directories
                if (includeDir)
                {
                    //Get included files from parameter list
                    foreach (string parameter in parameters)
                    {
                        if (parameter.Contains("includedir"))
                        {
                            var folderPath = parameter.Replace("\"", "").Replace("includedir ", "").TrimEnd(' ');
                            //Test that folder exists
                            if (Directory.Exists(folderPath))
                            {
                                var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                                foreach (var file in files)
                                    includeFiles.Add(file);
                            }
                            else
                                CompilePalLogger.LogCompileError($"Could not find folder: {folderPath}\n", new Error($"Could not find folder: {folderPath}", ErrorSeverity.Caution));
                        }
                    }
                }

                if (exclude)
                {
                    //Get excluded files from parameter list
                    foreach (string parameter in parameters)
                    {
                        if (Regex.IsMatch(parameter, @"^exclude\b"))
                        {
                            var filePath = parameter.Replace("\"", "")
                                .Replace("exclude ", "")
                                .Replace('/', '\\')
                                .ToLower().TrimEnd(' ');
                            //Test that file exists
                            if (File.Exists(filePath))
                                excludeFiles.Add(filePath);
                            else
                                CompilePalLogger.LogCompileError($"Could not find file: {filePath}\n", new Error($"Could not find file: {filePath}", ErrorSeverity.Caution));
                        }
                    }
                }

                if (excludeDir)
                {
                    //Get excluded directories from parameter list
                    foreach (string parameter in parameters)
                    {
                        if (Regex.IsMatch(parameter, @"^excludedir\b"))
                        {
                            var path = parameter.Replace("\"", "")
                                .Replace("excludedir ", "")
                                .Replace('/', '\\')
                                .ToLower().TrimEnd(' ');
                            //Test that dir exists
                            if (Directory.Exists(path))
                                excludeDirs.Add(path);
                            else
                                CompilePalLogger.LogCompileError($"Could not find folder: {path}\n", new Error($"Could not find folder: {path}", ErrorSeverity.Caution));
                        }
                    }
                }

                // exclude files that are in the specified vpk.
                if (excludevpk)
                {
                    foreach (string parameter in parameters)
                    {
                        if (parameter.Contains("excludevpk"))
                        {
                            var vpkPath = parameter.Replace("\"", "").Replace("excludevpk ", "").TrimEnd(' ');

                            string[] vpkFileList = GetVPKFileList(vpkPath);
                            
                            foreach (string file in vpkFileList)
                            {
                                excludedVpkFiles.Add(file.ToLower());
                            }
                        }
                    }
                }

                CompilePalLogger.LogLine("Finding sources of game content...");
                sourceDirectories = GetSourceDirectories(gameFolder);
                if (verbose)
                {
                    CompilePalLogger.LogLine("Source directories:");
                    foreach (var sourceDirectory in sourceDirectories)
                        CompilePalLogger.LogLine(sourceDirectory);
                }

                CompilePalLogger.LogLine("Reading BSP...");
                BSP map = new BSP(new FileInfo(bspPath));
                AssetUtils.findBspUtilityFiles(map, sourceDirectories, renamenav, genParticleManifest);

                // give files unique names based on map so they dont get overwritten
                if (dryrun)
                    outputFile = $"BSPZipFiles\\{Path.GetFileNameWithoutExtension(map.file.FullName)}_files.txt";

                //Set map particle manifest
                if (genParticleManifest)
                    map.particleManifest = particleManifest;

                string unpackDir = System.IO.Path.GetTempPath() + Guid.NewGuid();
                UnpackBSP(unpackDir);
                AssetUtils.findBspPakDependencies(map, unpackDir);

                CompilePalLogger.LogLine("Initializing pak file...");
                PakFile pakfile = new PakFile(map, sourceDirectories, includeFiles, excludeFiles, excludeDirs, excludedVpkFiles, outputFile);

                if (includefilelist)
                {
                    var fileListParams = parameters.Where(p => p.StartsWith("includefilelist")).Select(f => f.Split(new[]{" "}, 2, StringSplitOptions.None));
                    foreach (var fileListParam in fileListParams)
                    {
                        if (fileListParam.Length <= 1 || string.IsNullOrWhiteSpace(fileListParam[1]))
                        {
                            CompilePalLogger.LogCompileError("No file list parameter set\n",
                                new Error("No file list parameter set", ErrorSeverity.Error));
                            continue;
                        }

                        var inputFile = fileListParam[1];
                        if (!File.Exists(inputFile))
                        {
                            CompilePalLogger.LogCompileError($"Could not find file list {inputFile}\n", new Error($"Could not find file list {inputFile}\n", ErrorSeverity.Error));
                            continue;
                        }

                        CompilePalLogger.LogDebug($"Adding files from file list {inputFile}");
                        var filelist = File.ReadAllLines(inputFile);

                        // file list format is internal path, newline, external path
                        for (int i = 0; i < filelist.Length - 1; i += 2)
                        {
                            var internalPath = filelist[i];
                            var externalPath = filelist[i + 1];
                            if (!pakfile.AddInternalFile(internalPath, externalPath))
                            {
                                CompilePalLogger.LogCompileError($"Failed to pack ${externalPath}\n", new Error($"Failed to pack ${externalPath}\n", ErrorSeverity.Error));
                            }
                        }

                        CompilePalLogger.LogLine($"Added {filelist.Length / 2} files from ${inputFile}");
                    }
                }

                if (packvpk)
                {
                    string vpkName = context.BSPFile.Replace(".bsp", ".vpk");
                    if (File.Exists(vpkName))
                    {
                        File.Delete(vpkName);
                    }

                    var responseFile = pakfile.GetResponseFile();

                    if (File.Exists(bspPath))
                    {
                        // Add bsp to the vpk
                        responseFile.Add(bspPath.Replace(gameFolder + "\\", ""), gameFolder);
                    }

                    if (GetParameterString().Contains("-ainfo"))
                    {
                        foreach (string parameter in parameters)
                        {
                            if (parameter.Contains("ainfo"))
                            {
                                var @filePath = parameter.Replace("\"", "").Replace("ainfo ", "").TrimEnd(' ');
                                //Test that file exists
                                if (File.Exists(filePath))
                                {
                                    File.Copy(filePath, Path.Combine(gameFolder, "addoninfo.txt"), true);
                                    responseFile.Add("addoninfo.txt", gameFolder);
                                }
                            }
                        }
                    }

                    CompilePalLogger.LogLine("Running VPK...");
                    foreach (var path in sourceDirectories)
                    {
                        var testedFiles = "";
                        foreach (var entry in responseFile)
                        {
                            if (entry.Value.Contains(path) || path.Contains(entry.Value))
                            {
                                testedFiles += entry.Key + "\n";
                            }
                        }

                        var combinedPath = Path.Combine(path, "_tempResponseFile.txt");
                        File.WriteAllText(combinedPath, testedFiles);

                        PackVPK(vpkName, combinedPath, path);

                        File.Delete(combinedPath);
                    }

                    File.Delete("_tempResponseFile.txt");

                    if (GetParameterString().Contains("-ainfo"))
                    {
                        File.Delete(Path.Combine(gameFolder, "addoninfo.txt"));
                    }
                }
                else
                {
                    CompilePalLogger.LogLine("Writing file list...");
                    pakfile.OutputToFile();

                    if (dryrun)
                    {
                        CompilePalLogger.LogLine("File list saved as " + Environment.CurrentDirectory + outputFile);
                    }
                    else
                    {
                        PackFileList(context, outputFile);
                    }

                }

                CompilePalLogger.LogLine("Finished!");

                CompilePalLogger.LogLine("---------------------");
                CompilePalLogger.LogLine(pakfile.vmtcount + " materials found");
                CompilePalLogger.LogLine(pakfile.mdlcount + " models found");
                CompilePalLogger.LogLine(pakfile.pcfcount + " particle files found");
                CompilePalLogger.LogLine(pakfile.sndcount + " sounds found");
                if (pakfile.vehiclescriptcount != 0)
                    CompilePalLogger.LogLine(pakfile.vehiclescriptcount + " vehicle scripts found");
                if (pakfile.effectscriptcount != 0)
                    CompilePalLogger.LogLine(pakfile.effectscriptcount + " effect scripts found");
                if (pakfile.vscriptcount != 0)
                    CompilePalLogger.LogLine(pakfile.vscriptcount + " vscripts found");
                if (pakfile.panoramaMapIconCount != 0)
                    CompilePalLogger.LogLine(pakfile.panoramaMapIconCount + " panorama map icons found");
                string additionalFiles =
                    (map.nav.Key != default(string) ? "\n-nav file" : "") +
                    (map.soundscape.Key != default(string) ? "\n-soundscape" : "") +
                    (map.soundscript.Key != default(string) ? "\n-soundscript" : "") +
                    (map.detail.Key != default(string) ? "\n-detail file" : "") +
                    (map.particleManifest.Key != default(string) ? "\n-particle manifest" : "") +
                    (map.radartxt.Key != default(string) ? "\n-radar files" : "") +
                    (map.txt.Key != default(string) ? "\n-loading screen text" : "") +
                    (map.jpg.Key != default(string) ? "\n-loading screen image" : "") +
                    (map.kv.Key != default(string) ? "\n-kv file" : "") +
                    (map.res.Key != default(string) ? "\n-res file" : "");

                if (additionalFiles != "")
                    CompilePalLogger.LogLine("additional files: " + additionalFiles);
                CompilePalLogger.LogLine("---------------------");

            }
            catch (FileNotFoundException)
            {
                CompilePalLogger.LogCompileError($"Could not find {bspPath}\n", new Error($"Could not find {bspPath}", ErrorSeverity.Error));
            }
            catch (Exception exception)
            {
                CompilePalLogger.LogLine("Something broke:");
                CompilePalLogger.LogCompileError($"{exception}\n", new Error(exception.ToString(), "CompilePal Internal Error", ErrorSeverity.FatalError));
            }
        }

        static void PackFileList(CompileContext context, string outputFile)
        {
            if (File.Exists(context.BSPFile))
            {
                if (File.Exists(context.BSPFile + ".unpacked"))
                {
                    CompilePalLogger.LogLineDebug($"Deleting: {context.BSPFile}.unpacked");
                    File.Delete(context.BSPFile + ".unpacked");
                }

                CompilePalLogger.LogLineDebug($"Copying {context.BSPFile} to {context.BSPFile}.unpacked");
                File.Copy(context.BSPFile, context.BSPFile + ".unpacked");
            }

            CompilePalLogger.LogLine("Running bspzip...");
            PackBSP(outputFile);

            // don't copy if vmf directory is also the output directory
            if (bspPath != context.BSPFile)
            {
                if (File.Exists(context.BSPFile))
                {
                    CompilePalLogger.LogLineDebug($"Deleting: {context.BSPFile}");
                    File.Delete(context.BSPFile);
                }

                CompilePalLogger.LogLine("Copying packed bsp to vmf folder...");
                CompilePalLogger.LogLineDebug($"Copying {bspPath} to {context.BSPFile}");
                File.Copy(bspPath, context.BSPFile);
            }
        }

        static void UnpackBSP(string unpackDir)
        {
            // unpacks the pak file and extracts it to a temp location

            /* info: vbsp.exe creates files in the pak file that may have
             * dependencies that are not listed anywhere else, as is the
             * case for water materials. We use this method to extract the
             * pak file to a temp folder and read the dependencies of its files. */

            string arguments = "-extractfiles \"$bspold\" \"$dir\"";
            arguments = arguments.Replace("$bspold", bspPath);
            arguments = arguments.Replace("$dir", unpackDir);

            var startInfo = new ProcessStartInfo(bspZip, arguments);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.EnvironmentVariables["VPROJECT"] = gameFolder;

            var p = new Process { StartInfo = startInfo };
            p.Start();
            string output = p.StandardOutput.ReadToEnd();

            p.WaitForExit();

        }

        static void PackBSP(string outputFile)
        {
            string arguments = "-addlist \"$bspnew\"  \"$list\" \"$bspold\"";
            arguments = arguments.Replace("$bspnew", bspPath);
            arguments = arguments.Replace("$bspold", bspPath);
            arguments = arguments.Replace("$list", outputFile);

            var startInfo = new ProcessStartInfo(bspZip, arguments);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.EnvironmentVariables["VPROJECT"] = gameFolder;

            var p = new Process { StartInfo = startInfo };

            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            if (verbose)
                CompilePalLogger.Log(output);
            p.WaitForExit();
        }

        static void PackVPK(string targetVPK, string responseFile, string searchPath)
        {
            string arguments = $"a \"{targetVPK}\" \"@{responseFile}\"";

            var p = new Process
            {
                StartInfo = new ProcessStartInfo

                {
                    WorkingDirectory = searchPath,
                    FileName = vpk,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };

            //p.StartInfo.EnvironmentVariables["VPROJECT"] = gameFolder;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            string errOutput = p.StandardError.ReadToEnd();
            if (verbose)
            {
                CompilePalLogger.Log(output);
                CompilePalLogger.Log(errOutput);
            }


            p.WaitForExit();
        }

        static string[] GetVPKFileList(string VPKPath)
		{
            string arguments = $"l \"{VPKPath}\"";

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = Path.GetDirectoryName(vpk),
                    FileName = vpk,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };
            
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            string errOutput = p.StandardError.ReadToEnd();
            if (verbose)
            {
                CompilePalLogger.Log(errOutput);
            }

            p.WaitForExit();

            char[] delims = new[] { '\r', '\n' };
            return output.Split(delims, StringSplitOptions.RemoveEmptyEntries);
        }

        static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            CompilePalLogger.LogLine(e.Data);
        }

        public static List<string> GetSourceDirectories(string gamePath, bool verbose = true)
        {
            List<string> sourceDirectories = new List<string>();
            string gameInfo = System.IO.Path.Combine(gamePath, "gameinfo.txt");

            string rootPath = Directory.GetParent(gamePath).ToString();

            if (File.Exists(gameInfo))
            {
                var lines = File.ReadAllLines(gameInfo);

                bool foundSearchPaths = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    if (foundSearchPaths)
                    {
                        if (line.Contains("}"))
                            break;

                        if (line.Contains("//") || string.IsNullOrWhiteSpace(line))
                            continue;

                        string path = GetInfoValue(line).Replace("\"", "");

                        if (path.Contains("|") && !path.Contains("|gameinfo_path|") || path.Contains(".vpk")) continue;

                        if (path.Contains("*"))
                        {
							string fullPath = path;
							if (fullPath.Contains(("|gameinfo_path|")))
	                        {
		                        string newPath = path.Replace("*", "").Replace("|gameinfo_path|", "");

		                        fullPath = System.IO.Path.GetFullPath(gamePath + "\\" + newPath.TrimEnd('\\'));
	                        }
                            if (Path.IsPathRooted(fullPath.Replace("*", "")))
                            {
                                fullPath = fullPath.Replace("*", "");
                            }
	                        else
	                        {
		                        string newPath = fullPath.Replace("*", "");

		                        fullPath = System.IO.Path.GetFullPath(rootPath + "\\" + newPath.TrimEnd('\\'));
	                        }

	                        if (verbose)
		                        CompilePalLogger.LogLine("Found wildcard path: {0}", fullPath);

	                        try
	                        {
		                        var directories = Directory.GetDirectories(fullPath);
		                        sourceDirectories.AddRange(directories);
	                        }
	                        catch { }
                        }
                        else if (path.Contains("|gameinfo_path|"))
                        {
	                        string fullPath = gamePath;

	                        if (verbose)
		                        CompilePalLogger.LogLine("Found search path: {0}", fullPath);

	                        sourceDirectories.Add(fullPath);
                        }
                        else if (Directory.Exists(path))
                        {
	                        if (verbose)
		                        CompilePalLogger.LogLine("Found search path: {0}", path);

	                        sourceDirectories.Add(path);

                        }
                        else
                        {
                            try
                            {
                                string fullPath = System.IO.Path.GetFullPath(rootPath + "\\" + path.TrimEnd('\\'));

                                if (verbose)
                                    CompilePalLogger.LogLine("Found search path: {0}", fullPath);

                                sourceDirectories.Add(fullPath);
                            }
                            catch (Exception e)
                            {
                                CompilePalLogger.LogDebug("Failed to find search path: " + e);
                                CompilePalLogger.LogCompileError($"Search path invalid: {rootPath + "\\" + path.TrimEnd('\\')}", new Error($"Search path invalid: {rootPath + "\\" + path.TrimEnd('\\')}", ErrorSeverity.Caution));
                            }
                        }
                    }
                    else
                    {
                        if (line.Contains("SearchPaths"))
                        {
                            if (verbose)
                                CompilePalLogger.LogLine("Found search paths...");
                            foundSearchPaths = true;
                            i++;
                        }
                    }
                }
            }
            else
            {
                CompilePalLogger.LogCompileError($"Couldn't find gameinfo.txt at {gameInfo}", new Error($"Couldn't find gameinfo.txt at {gameInfo}", ErrorSeverity.Caution));
            }
            return sourceDirectories.Distinct().ToList();
        }
        private static string GetInfoValue(string line)
        {
			// filepath info values
			if (line.Contains('"'))
				return Regex.Split( line, @"[^\s""']+|""([^""]*)""|'([^']*)'") // splits on whitespace outside of quotes
					.First((l) => !string.IsNullOrWhiteSpace(l)); // ignore empty entries

			// new char[0] = split on whitespace
            return line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries)[1];
        }

        // parses parameters that can contain '-' in their values. Ex. filepaths
        private static List<string> ParseParameters(char[] paramChars)
        {
            List<string> parameters = new List<string>();
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
    }
}
