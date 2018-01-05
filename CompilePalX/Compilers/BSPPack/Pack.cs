using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private static string gameFolder;
        private static string bspPath;

        private const string keysFolder = "Keys";

        private static bool verbose;
        private static bool dryrun;
        private static bool renamenav;
        public static bool genparticlemanifest;

        public static KeyValuePair<string, string> particleManifest;

        private List<string> sourceDirectories = new List<string>();

        public override void Run(CompileContext context)
        {
            verbose = GetParameterString().Contains("-verbose");
            dryrun = GetParameterString().Contains("-dryrun");
            renamenav = GetParameterString().Contains("-renamenav");

            try
            {
                CompilePalLogger.LogLine("\nCompilePal - Automated Packaging");
                bspZip = context.Configuration.BSPZip;
                gameFolder = context.Configuration.GameFolder;
                bspPath = context.CopyLocation;

                if (!File.Exists(bspPath))
                {
                    throw new FileNotFoundException();
                }

                Keys.vmtTextureKeyWords = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "texturekeys.txt")).ToList();
                Keys.vmtMaterialKeyWords = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "materialkeys.txt")).ToList();
                Keys.vmfSoundKeys = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "vmfsoundkeys.txt")).ToList();
                Keys.vmfMaterialKeys = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "vmfmaterialkeys.txt")).ToList();
                Keys.vmfModelKeys = File.ReadAllLines(System.IO.Path.Combine(keysFolder, "vmfmodelkeys.txt")).ToList();

                CompilePalLogger.LogLine("Finding sources of game content...");
                sourceDirectories = GetSourceDirectories(gameFolder);

                CompilePalLogger.LogLine("Reading BSP...");
                BSP map = new BSP(new FileInfo(bspPath));
                AssetUtils.findBspUtilityFiles(map, sourceDirectories, renamenav, genparticlemanifest);

                //Set map particle manifest
                if (genparticlemanifest)
                    map.particleManifest = particleManifest;

                string unpackDir = System.IO.Path.GetTempPath() + Guid.NewGuid();
                UnpackBSP(unpackDir);
                AssetUtils.findBspPakDependencies(map, unpackDir);

                CompilePalLogger.LogLine("Initializing pak file...");
                PakFile pakfile = new PakFile(map, sourceDirectories);

                CompilePalLogger.LogLine("Writing file list...");
                pakfile.OutputToFile();

                if (dryrun)
                {
                    CompilePalLogger.LogLine("File list saved as " + Environment.CurrentDirectory + "\\files.txt");
                }
                else
                {
                    CompilePalLogger.LogLine("Running bspzip...");
                    PackBSP();
                }
                
                CompilePalLogger.LogLine("Finished!");

                CompilePalLogger.LogLine("---------------------");
                CompilePalLogger.LogLine(pakfile.vmtcount + " materials found");
                CompilePalLogger.LogLine(pakfile.mdlcount + " models found");
                CompilePalLogger.LogLine(pakfile.pcfcount + " particle files found");
                CompilePalLogger.LogLine(pakfile.sndcount + " sounds found");
                if (pakfile.vehiclescriptcount != 0)
                    CompilePalLogger.LogLine(pakfile.vehiclescriptcount + " vehicle scripts found");
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
                CompilePalLogger.LogLine("FAILED - Could not find " + bspPath);
            }
            catch (Exception exception)
            {
                CompilePalLogger.LogLine("Something broke:");
                CompilePalLogger.LogLine(exception.ToString());
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

        static void PackBSP()
        {
            string arguments = "-addlist \"$bspnew\"  \"$list\" \"$bspold\"";
            arguments = arguments.Replace("$bspnew", bspPath);
            arguments = arguments.Replace("$bspold", bspPath);
            arguments = arguments.Replace("$list", "files.txt");

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

                        string path = GetInfoValue(line);

                        if (!(path.Contains("|") || path.Contains(".vpk")))
                        {
                            if (path.Contains("*"))
                            {
                                string newPath = path.Replace("*", "");

                                string fullPath = System.IO.Path.GetFullPath(rootPath + "\\" + newPath.TrimEnd('\\'));

                                if (verbose)
                                    CompilePalLogger.LogLine("Found wildcard path: {0}", fullPath);

                                try
                                {
                                    var directories = Directory.GetDirectories(fullPath);
                                    sourceDirectories.AddRange(directories);
                                }
                                catch { }
                            }
                            else
                            {
                                string fullPath = System.IO.Path.GetFullPath(rootPath + "\\" + path.TrimEnd('\\'));

                                if (verbose)
                                    CompilePalLogger.LogLine("Found search path: {0}", fullPath);

                                sourceDirectories.Add(fullPath);
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
                CompilePalLogger.LogLine("Couldn't find gameinfo.txt at {0}", gameInfo);
            }
            return sourceDirectories;
        }
        static private string GetInfoValue(string line)
        {
            return line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries)[1];
        }


    }
}
