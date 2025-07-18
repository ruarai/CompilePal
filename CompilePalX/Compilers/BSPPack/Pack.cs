﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using CompilePalX.Compiling;
using CompilePalX.KV;
using GlobExpressions;
using Microsoft.Win32;
using ValveKeyValue;

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

        private static KVSerializer KVSerializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

        private static string bspZip;
        private static string vpk;
        private static string gameFolder;
        private static string bspPath;

        private const string keysFolder = "Keys";

        private static bool verbose;
        public static bool genParticleManifest;

        public static KeyValuePair<string, string> particleManifest;

        private List<string> sourceDirectories = [];
        private string outputFile = "BSPZipFiles\\files.txt";

        public override void Run(CompileContext context, CancellationToken cancellationToken)
        {
            CompileErrors = [];

            if (!CanRun(context)) return;

            verbose = GetParameterString().Contains("-verbose");
            bool dryrun = GetParameterString().Contains("-dryrun");
            bool renamenav = GetParameterString().Contains("-renamenav");
            bool include = Regex.IsMatch(GetParameterString(), @"-include\b"); // ensures it doesnt match -includedir
            bool includeDir = GetParameterString().Contains("-includedir");
            bool exclude = Regex.IsMatch(GetParameterString(), @"-exclude\b"); // ensures it doesnt match -excludedir
            bool excludeDir = GetParameterString().Contains("-excludedir");
            bool excludevpk = GetParameterString().Contains("-excludevpk");
            bool packvpk = GetParameterString().Contains("-vpk");
            bool includefilelist = GetParameterString().Contains("-includefilelist");
            bool addSourceDirectory = GetParameterString().Contains("-sourcedirectory");
            bool noswvtx = GetParameterString().Contains("-noswvtx");

            char[] paramChars = GetParameterString().ToCharArray();
            List<string> parameters = ParseParameters(paramChars);

            List<string> includeFiles = [];
            List<string> excludeFiles = [];
            List<string> excludeDirs = [];
            List<string> excludedVpkFiles = [];

            try
            {
                CompilePalLogger.LogLine("\nCompilePal - Automated Packaging");
                bspZip = context.Configuration.BSPZip;
                vpk = context.Configuration.VPK;
                gameFolder = context.Configuration.GameFolder;
                bspPath = context.CopyLocation;

                if (!File.Exists(bspPath))
                {
                    throw new FileNotFoundException("Could not find BSP", bspPath);
                }

                outputFile = "BSPZipFiles\\files.txt";

                Keys.vmtTextureKeyWords =
                    File.ReadAllLines(System.IO.Path.Combine(keysFolder, "texturekeys.txt")).ToList();
                Keys.vmtMaterialKeyWords =
                    File.ReadAllLines(System.IO.Path.Combine(keysFolder, "materialkeys.txt")).ToList();
                Keys.vmfSoundKeys = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "vmfsoundkeys.txt")).ToList();
                Keys.vmfMaterialKeys = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "vmfmaterialkeys.txt"))
                    .ToList();
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
                                CompilePalLogger.LogCompileError($"Could not find file: {filePath}\n",
                                    new Error($"Could not find file: {filePath}", $"Could not find file: {filePath}",
                                        ErrorSeverity.Caution));
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
                                CompilePalLogger.LogCompileError($"Could not find folder: {folderPath}\n",
                                    new Error($"Could not find folder: {folderPath}", ErrorSeverity.Caution));
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
                                CompilePalLogger.LogCompileError($"Could not find file: {filePath}\n",
                                    new Error($"Could not find file: {filePath}", ErrorSeverity.Caution));
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
                                CompilePalLogger.LogCompileError($"Could not find folder: {path}\n",
                                    new Error($"Could not find folder: {path}", ErrorSeverity.Caution));
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

                if (addSourceDirectory)
                {
                    foreach (string parameter in parameters)
                    {
                        if (!parameter.Contains("sourcedirectory"))
                        {
                            continue;
                        }

                        var glob = parameter.Replace("\"", "")
                            .Replace("sourcedirectory ", "")
                            .Replace('/', '\\')
                            .ToLower().TrimEnd(' ');

                        string root = Directory.GetDirectoryRoot(glob);

                        var globResults = Glob.Directories(root, glob.Substring(root.Length), GlobOptions.CaseInsensitive);
                        if (globResults.Count() == 0)
                        {
                            CompilePalLogger.LogCompileError($"Found no matching folders for: {glob}\n",
                                new Error($"Found no matching folders for: {glob}", ErrorSeverity.Caution));
                            continue;
                        }

                        foreach (string path in globResults)
                            sourceDirectories.Add(root + path);
                    }
                }

                if (verbose)
                {
                    CompilePalLogger.LogLine("Source directories:");
                    foreach (var sourceDirectory in sourceDirectories)
                        CompilePalLogger.LogLine(sourceDirectory);
                }

                CompilePalLogger.LogLine("Reading BSP...");

                BSP map;
                try
                {
                    map = new BSP(new FileInfo(bspPath));
                } catch (CompressedBSPException)
                {
                    CompilePalLogger.LogLineCompileError($"BSPZIP does not support compressed BSPs", new Error($"BSPZIP does not support compressed BSPs", ErrorSeverity.FatalError));
                    return;
                }

                AssetUtils.FindBspUtilityFiles(map, sourceDirectories, renamenav, genParticleManifest);

                // give files unique names based on map so they dont get overwritten
                if (dryrun)
                    outputFile = $"BSPZipFiles\\{Path.GetFileNameWithoutExtension(map.file.FullName)}_files.txt";

                //Set map particle manifest
                if (genParticleManifest)
                    map.particleManifest = particleManifest;

                if (cancellationToken.IsCancellationRequested)
                    return;

                string unpackDir = System.IO.Path.GetTempPath() + Guid.NewGuid();
                UnpackBSP(unpackDir);
                AssetUtils.FindBspPakDependencies(map, unpackDir);

                CompilePalLogger.LogLine("Initializing pak file...");
                PakFile pakfile = new PakFile(map, sourceDirectories, includeFiles, excludeFiles, excludeDirs,
                    excludedVpkFiles, outputFile, noswvtx);

                if (includefilelist)
                {
                    var fileListParams = parameters.Where(p => p.StartsWith("includefilelist"))
                        .Select(f => f.Split(new[] { " " }, 2, StringSplitOptions.None));
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
                            CompilePalLogger.LogCompileError($"Could not find file list {inputFile}\n",
                                new Error($"Could not find file list {inputFile}\n", ErrorSeverity.Error));
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
                                CompilePalLogger.LogCompileError($"Failed to pack ${externalPath}\n",
                                    new Error($"Failed to pack ${externalPath}\n", ErrorSeverity.Error));
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


                    if (cancellationToken.IsCancellationRequested)
                        return;

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
                    if (cancellationToken.IsCancellationRequested)
                        return;
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
                CompilePalLogger.LogLine(pakfile.soundcount + " sounds found");
                if (pakfile.vehiclescriptcount != 0)
                    CompilePalLogger.LogLine(pakfile.vehiclescriptcount + " vehicle scripts found");
                if (pakfile.effectscriptcount != 0)
                    CompilePalLogger.LogLine(pakfile.effectscriptcount + " effect scripts found");
                if (pakfile.vscriptcount != 0)
                    CompilePalLogger.LogLine(pakfile.vscriptcount + " vscripts found");
                if (pakfile.PanoramaMapBackgroundCount != 0)
                    CompilePalLogger.LogLine(pakfile.PanoramaMapBackgroundCount + " Panorama map backgrounds found");
                if (map.res.Count != 0)
                    CompilePalLogger.LogLine(map.res.Count + " res files found");
                string additionalFiles =
                    (map.nav.Key != default(string) ? "\n-Nav file" : "") +
                    (map.soundscape.Key != default(string) ? "\n-Soundscape" : "") +
                    (map.soundscript.Key != default(string) ? "\n-Soundscript" : "") +
                    (map.detail.Key != default(string) ? "\n-Detail file" : "") +
                    (map.particleManifest.Key != default(string) ? "\n-Particle manifest" : "") +
                    (map.radartxt.Key != default(string) ? "\n-Radar files" : "") +
                    (map.RadarTablet.Key != default(string) ? "\n-Radar tablet" : "") +
                    (map.txt.Key != default(string) ? "\n-Loading screen text" : "") +
                    (map.jpg.Key != default(string) ? "\n-Loading screen image" : "") +
                    (map.PanoramaMapIcon.Key != default(string) ? "\n-Panorama map icon" : "") +
                    (map.kv.Key != default(string) ? "\n-KV file" : "");

                if (additionalFiles != "")
                    CompilePalLogger.LogLine("Additional Files: " + additionalFiles);
                CompilePalLogger.LogLine("---------------------");
            }
            catch (FileNotFoundException e)
            {
                CompilePalLogger.LogCompileError($"Could not find {e.FileName}\n",
                    new Error($"Could not find {e.FileName}", ErrorSeverity.Error));
            }
            catch (ThreadAbortException)
            {
                // this happens when we cancel the compile or can't run bspzip. Rethrow so it can get properly caught in the CompilingManager
                throw;
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
            if (!Path.GetFullPath(bspPath).Equals(Path.GetFullPath(context.BSPFile), StringComparison.OrdinalIgnoreCase))
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

            var startInfo = new ProcessStartInfo(bspZip, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                EnvironmentVariables =
                {
                    ["VPROJECT"] = gameFolder
                }
            };

            var p = new Process { StartInfo = startInfo };

            try
            {
                p.Start();
            }
            catch (Exception e)
            {
                CompilePalLogger.LogDebug(e.ToString());
                CompilePalLogger.LogCompileError($"Failed to run executable: {p.StartInfo.FileName}\n", new Error($"Failed to find executable: {p.StartInfo.FileName}", ErrorSeverity.FatalError));
                return;
            }

            string output = p.StandardOutput.ReadToEnd();
            if (verbose)
                CompilePalLogger.Log(output);
            else
                CompilePalLogger.LogDebug(output);

            p.WaitForExit();
            if (p.ExitCode != 0) {
                // this indicates an access violation. BSPZIP may have crashed because of too many files being packed
                if (p.ExitCode == -1073741819)
                    CompilePalLogger.LogCompileError($"BSPZIP exited with code: {p.ExitCode}, this might indicate that too many files are being packed\n", new Error($"BSPZIP exited with code: {p.ExitCode}, this might indicate that too many files are being packed\n", ErrorSeverity.FatalError));
                else
                    CompilePalLogger.LogCompileError($"BSPZIP exited with code: {p.ExitCode}\n", new Error($"BSPZIP exited with code: {p.ExitCode}\n", ErrorSeverity.Warning));
            }

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


            try
            {
                p.Start();
            }
            catch (Exception e)
            {
                CompilePalLogger.LogDebug(e.ToString());
                CompilePalLogger.LogCompileError($"Failed to run executable: {p.StartInfo.FileName}\n", new Error($"Failed to run executable: {p.StartInfo.FileName}", ErrorSeverity.FatalError));
                return;
            }

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
            List<string> sourceDirectories = [];
            string gameInfoPath = System.IO.Path.Combine(gamePath, "gameinfo.txt");
            string rootPath = Directory.GetParent(gamePath).ToString();

            if (!File.Exists(gameInfoPath))
            {
                CompilePalLogger.LogCompileError($"Couldn't find gameinfo.txt at {gameInfoPath}", new Error($"Couldn't find gameinfo.txt at {gameInfoPath}", ErrorSeverity.Error));
                return [];
            }

            using (var gameInfoFile = File.OpenRead(gameInfoPath))
            {
                var gameInfo = KVSerializer.Deserialize(gameInfoFile);
                if (gameInfo is null)
                {
                    CompilePalLogger.LogLineDebug($"Failed to parse GameInfo: {gameInfo}");
                    CompilePalLogger.LogCompileError($"Failed to parse GameInfo", new Error($"Failed to parse GameInfo", ErrorSeverity.Error));
                    return [];
                }

                if (gameInfo.Name != "GameInfo")
                {
                    CompilePalLogger.LogLineDebug($"Failed to parse GameInfo: {gameInfo}");
                    CompilePalLogger.LogCompileError($"Failed to parse GameInfo, did not find GameInfo block\n", new Error($"Failed to parse GameInfo, did not find GameInfo block", ErrorSeverity.Error));
                    return [];
                }

                var searchPaths = gameInfo["FileSystem"]?["SearchPaths"] as IEnumerable<KVObject>;
                if (searchPaths is null)
                {
                    CompilePalLogger.LogLineDebug($"Failed to parse GameInfo: {gameInfo}");
                    CompilePalLogger.LogCompileError($"Failed to parse GameInfo, did not find FileSystem.SearchPaths block\n", new Error($"Failed to parse GameInfo, did not find FileSystem.SearchPaths block", ErrorSeverity.Error));
                    return [];
                }

                foreach (var searchPathObject in searchPaths)
                {
                    var searchPath = searchPathObject.Value.ToString();
                    if (searchPath is null)
                        continue;

                    // ignore unsearchable paths. TODO: will need to remove .vpk from this check if we add support for packing from assets within vpk files
                    if (searchPath.Contains("|") && !searchPath.Contains("|gameinfo_path|") || searchPath.Contains(".vpk")) continue;

                    // wildcard paths
                    if (searchPath.Contains("*"))
                    {
                        string fullPath = searchPath;
                        if (fullPath.Contains(("|gameinfo_path|")))
                        {
                            string newPath = searchPath.Replace("*", "").Replace("|gameinfo_path|", "");
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
                    else if (searchPath.Contains("|gameinfo_path|"))
                    {
                        string fullPath = gamePath;

                        if (verbose)
                            CompilePalLogger.LogLine("Found search path: {0}", fullPath);

                        sourceDirectories.Add(fullPath);
                    }
                    else if (Directory.Exists(searchPath))
                    {
                        if (verbose)
                            CompilePalLogger.LogLine("Found search path: {0}", searchPath);

                        sourceDirectories.Add(searchPath);
                    }
                    else
                    {
                        try
                        {
                            string fullPath = System.IO.Path.GetFullPath(rootPath + "\\" + searchPath.TrimEnd('\\'));

                            if (verbose)
                                CompilePalLogger.LogLine("Found search path: {0}", fullPath);

                            sourceDirectories.Add(fullPath);
                        }
                        catch (Exception e)
                        {
                            CompilePalLogger.LogDebug("Failed to find search path: " + e);
                            CompilePalLogger.LogCompileError($"Search path invalid: {rootPath + "\\" + searchPath.TrimEnd('\\')}", new Error($"Search path invalid: {rootPath + "\\" + searchPath.TrimEnd('\\')}", ErrorSeverity.Caution));
                        }
                    }
                }


                //find Chaos engine game mount paths
                var mountedDirectories = GetMountedGamesSourceDirectories(gameInfo, Path.Combine(gamePath, "cfg", "mounts.kv"));
                if (mountedDirectories != null)
                {
                    sourceDirectories.AddRange(mountedDirectories);
                    foreach (var directory in mountedDirectories)
                    {
                        CompilePalLogger.LogLine($"Found mounted search path: {directory}");
                    }
                }

            }

            return sourceDirectories.Distinct().ToList();
        }

        /// <summary>
        /// Finds additional source directories for Chaos Engine game mounts
        /// documentation from https://github.com/momentum-mod/game/pull/1150
        /// </summary>
        /// <param name="gameInfo">GameInfo KV document</param>
        /// <param name="mountsPath">Path to mounts.kv</param>
        /// <returns>A list of additional source directories to search</returns>
        private static List<string>? GetMountedGamesSourceDirectories(KVDocument gameInfo, string mountsPath)
        {
            CompilePalLogger.LogLineDebug("Looking for mounted games");

            // parse gameinfo.txt to find game mounts
            var mountBlock = gameInfo["mount"] as IEnumerable<KVObject>;
            if (mountBlock is null)
            {
                CompilePalLogger.LogLineDebug("No mounted games detected");
                return null;
            }

            var mounts = mountBlock.ToList();

            // parse mounts.kv to find additional game mounts
            if (File.Exists(mountsPath))
            {
                using (var mountsFile = File.OpenRead(mountsPath))
                {
                    var mountData = KVSerializer.Deserialize(mountsFile);
                    var additionalMounts = mountData.Children;

                    if (additionalMounts is not null && additionalMounts.Any())
                    {
                        CompilePalLogger.LogLineDebug("Found additional mounts in mounts.kv");
                        mounts.AddRange(additionalMounts);

                        // remove duplicates, prefer results from mounts.kv
                        mounts = mounts.GroupBy(g => g.Name).Select(grp => grp.Last()).ToList();
                    }
                    else
                    {
                        CompilePalLogger.LogLineDebug("No mounted games detected in mounts.kv");
                    }

                }
            }

            // get location of Steam folder to parse libraryfolders.vdf
            var rk = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (rk is null)
            {
                CompilePalLogger.LogLineDebug("Could not find Steam registry key");
                return null;
            }

            if (rk.GetValue("SteamPath") is not string steamPath)
            {
                CompilePalLogger.LogLineDebug("Could not find SteamPath registry value");
                return null;
            }
            string steamAppsPath = Path.Combine(steamPath, "steamapps");

            // get game installation locations
            var appLocations = GetAppInstallLocations(steamAppsPath);

            if (appLocations is null)
                return null;

            // parse mounted games
            var directories = new List<string>();
            foreach (var mount in mounts)
            {
                var mountDirectories = GetMountedGameSourceDirectories(mount, appLocations);
                directories.AddRange(mountDirectories);
            }

            return directories;
        }

        /// <summary>
        /// Finds the mounted directories for a game
        /// </summary>
        /// <param name="mount">GameInfo KVObject for mounted game</param>
        /// <param name="appLocations">Dictionary mapping from appId to installation location</param>
        /// <returns>List of mounted folders</returns>
        private static List<string> GetMountedGameSourceDirectories(KVObject mount, Dictionary<string, string> appLocations)
        {
            var directories = new List<string>();
            string gameId = mount.Name;
            if (!appLocations.TryGetValue(gameId, out var gameLocation))
            {
                CompilePalLogger.LogLineDebug($"Could not find location for game {gameId}");
                return directories;
            }

            CompilePalLogger.LogLineDebug($"Found mount for {gameId}: {gameLocation}");
            foreach (var gameFolder in mount.Children)
            {
                string folder = gameFolder.Name.Replace("\"", String.Empty);
                directories.Add(Path.Combine(gameLocation, folder));

                foreach (var subdirMount in gameFolder.Children)
                {
                    string mountType = subdirMount.Name.ToString();

                    // only dir mounts supported for now, might add vpk in the future
                    if (mountType != "dir")
                        continue;

                    string subdir = subdirMount.Value.ToString();
                    directories.Add(Path.Combine(gameLocation, folder, subdir));
                }
            }

            return directories;
        }

        /// <summary>
        /// Finds app base installation paths
        /// </summary>
        /// <param name="steamAppsPath">path to the steamapps folder</param>
        /// <returns>A dictionary containing gameId keys and installation folder values</returns>
        private static Dictionary<string, string>? GetAppInstallLocations(string steamAppsPath)
        {
            // get installation base path and Steam ID for all installed games
            var libPath = Path.Combine(steamAppsPath, "libraryfolders.vdf");
            if (!File.Exists(libPath))
            {
                CompilePalLogger.LogLineDebug($"Could not find {libPath}");
                return null;
            }

            var locations = new List<(string basePath, string steamId)>();
            using (var libFile = File.OpenRead(libPath))
            {
                var libraries = KVSerializer.Deserialize(libFile);
                if (libraries is null || libraries.Name != "libraryfolders")
                {
                    CompilePalLogger.LogLineDebug($"Failed to parse {libPath}");
                    return null;
                }

                // create list of steam ID's and their base path
                foreach (var folder in libraries.Children)
                {
                    var basePath = Path.Combine(folder["path"].ToString(), "steamapps");
                    var ids = folder["apps"] as IEnumerable<KVObject>;
                    if (ids == null) continue;

                    foreach (var id in ids)
                    {
                        locations.Add((basePath, id.Name));
                    }
                }
            }


            // find actual installation folder for games from their appmanifest
            var paths = new Dictionary<string, string>();
            foreach ((string basePath, string steamId) in locations)
            {
                var appManifestPath = Path.Combine(basePath, $"appmanifest_{steamId}.acf");
                if (!File.Exists(appManifestPath))
                {
                    CompilePalLogger.LogCompileError($"App Manifest {appManifestPath} does not exist, ignoring\n", new Error($"App Manifest does not exist, ignoring\n", ErrorSeverity.Warning));
                    continue;
                }

                using (var appManifestFile = File.OpenRead(appManifestPath))
                {
                    var appManifest = KVSerializer.Deserialize(appManifestFile);
                    if (appManifest is null)
                    {
                        CompilePalLogger.LogCompileError($"Failed to parse App Manifest {appManifestPath}, ignoring\n", new Error("Failed to parse App Manifest", ErrorSeverity.Warning));
                        continue;
                    }

                    paths[steamId] = Path.Combine(basePath, "common", appManifest["installdir"].ToString());
                }
            }

            return paths;
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
    }
}
