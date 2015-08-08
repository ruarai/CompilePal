using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BSPPack
{
    static class Keys
    {
        public static List<string> vmfSoundKeys;
        public static List<string> vmfModelKeys;
        public static List<string> vmfMaterialKeys;
        public static List<string> vmtTextureKeyWords;
        public static List<string> vmtMaterialKeyWords;
    }

    class Program
    {
        private static string bspZip;
        private static string gameFolder;
        private static string bspPath;
        private static string keysFolder;

        static void Main(string[] args)
        {
            try
            {

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-game")
                    {
                        i++;
                        gameFolder = args[i];
                    }

                    if (args[i] == "-bsp")
                    {
                        i++;
                        bspPath = args[i];
                    }

                    if (args[i] == "-bspZip")
                    {
                        i++;
                        bspZip = args[i];
                    }

                    if (args[i] == "-keys")
                    {
                        i++;
                        keysFolder = args[i];
                    }
                }
                if (gameFolder != null && bspPath != null && bspZip != null && keysFolder != null)
                {

                    Keys.vmtTextureKeyWords = File.ReadAllLines(Path.Combine(keysFolder, "texturekeys.txt")).ToList();
                    Keys.vmtMaterialKeyWords = File.ReadAllLines(Path.Combine(keysFolder, "materialkeys.txt")).ToList();
                    Keys.vmfSoundKeys = File.ReadAllLines(Path.Combine(keysFolder, "vmfsoundkeys.txt")).ToList();
                    Keys.vmfMaterialKeys = File.ReadAllLines(Path.Combine(keysFolder, "vmfmaterialkeys.txt")).ToList();
                    Keys.vmfModelKeys = File.ReadAllLines(Path.Combine(keysFolder, "vmfmodelkeys.txt")).ToList();

                    Console.WriteLine("Finding sources of game content...");
                    GetSourceDirectories(gameFolder);

                    Console.WriteLine("Reading BSP...");
                    BSP map = new BSP(new FileInfo(bspPath));
                    AssetUtils.findBspUtilityFiles(map, sourceDirectories);

                    Console.WriteLine("Initializing pak file...");
                    PakFile pakfile = new PakFile(map , sourceDirectories);

                    Console.WriteLine("Writing file list...");
                    pakfile.OutputToFile();

                    Console.WriteLine("Running bspzip...");
                    PackBSP();

                    Console.WriteLine("Finished packing!");

                    Console.WriteLine("---------------------");
                    Console.WriteLine(pakfile.vmtcount + " materials added");
                    Console.WriteLine(pakfile.mdlcount + " models added");
                    Console.WriteLine(pakfile.pcfcount + " particle files added");
                    Console.WriteLine(pakfile.sndcount + " sounds added");
                    Console.WriteLine("Nav file: " + (map.nav.Key != default(string) ? "yes" : "no"));
                    Console.WriteLine("Soundscape: " + (map.soundscape.Key != default(string) ? "yes" : "no"));
                    Console.WriteLine("Soundscript: " + (map.soundscript.Key != default(string) ? "yes" : "no"));
                    Console.WriteLine("Detail File: " + (map.detail.Key != default(string) ? "yes" : "no"));
                    Console.WriteLine("Particle Manifest: " + (map.particleManifest.Key != default(string) ? "yes" : "no"));
                    Console.WriteLine("---------------------");

                }
                else
                {
                    Console.WriteLine("Missing required arguments.");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Something broke:");
                Console.WriteLine(exception.ToString());
            }
        }

        static void PackBSP()
        {
            string arguments = "-game \"$game\" -addlist \"$bspnew\"  \"$list\" \"$bspold\"";
            arguments = arguments.Replace("$bspnew", bspPath);
            arguments = arguments.Replace("$bspold", bspPath);
            arguments = arguments.Replace("$list", "files.txt");
            arguments = arguments.Replace("$game", gameFolder);

            var p = new Process { StartInfo = { Arguments = arguments, FileName = bspZip, UseShellExecute = false, RedirectStandardOutput = true } };

            p.OutputDataReceived += p_OutputDataReceived;

            p.Start();
        }

        static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private static List<string> sourceDirectories = new List<string>();

        static void GetSourceDirectories(string gamePath)
        {
            string gameInfo = Path.Combine(gamePath, "gameinfo.txt");

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

                                string fullPath = Path.GetFullPath(rootPath + "\\" + newPath.TrimEnd('\\'));

                                Console.WriteLine("Found wildcard path: {0}", fullPath);

                                var directories = Directory.GetDirectories(fullPath);

                                sourceDirectories.AddRange(directories);

                            }
                            else
                            {
                                string fullPath = Path.GetFullPath(rootPath + "\\" + path.TrimEnd('\\'));

                                Console.WriteLine("Found search path: {0}", fullPath);

                                sourceDirectories.Add(fullPath);
                            }
                        }
                    }
                    else
                    {
                        if (line.Contains("SearchPaths"))
                        {
                            Console.WriteLine("Found search paths...");
                            foundSearchPaths = true;
                            i++;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Couldn't find gameinfo.txt at {0}", gameInfo);
            }
        }
        static private string GetInfoValue(string line)
        {
            return line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries)[1];
        }


    }
}
