using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BSPPack
{
    class Program
    {
        private static string bspZip;
        private static string gameFolder;
        private static string bspPath;
        private static string vmfPath;
        private static string keysFolder;

        private static List<string> vmfSoundKeys;
        private static List<string> vmfMaterialKeys;
        private static List<string> vmfModelKeys;

        private static List<string> vmfAllKeys = new List<string>();

        private static List<string> vmtTexturekeyWords;
        private static List<string> vmtMaterialkeyWords;

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

                    if (args[i] == "-vmf")
                    {
                        i++;
                        vmfPath = args[i];
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
                if (gameFolder != null && bspPath != null && vmfPath != null && bspZip != null && keysFolder != null)
                {

                    vmtTexturekeyWords = File.ReadAllLines(Path.Combine(keysFolder, "texturekeys.txt")).ToList();
                    vmtMaterialkeyWords = File.ReadAllLines(Path.Combine(keysFolder, "materialkeys.txt")).ToList();

                    vmfSoundKeys = File.ReadAllLines(Path.Combine(keysFolder, "vmfsoundkeys.txt")).ToList();
                    vmfMaterialKeys = File.ReadAllLines(Path.Combine(keysFolder, "vmfmaterialkeys.txt")).ToList();
                    vmfModelKeys = File.ReadAllLines(Path.Combine(keysFolder, "vmfmodelkeys.txt")).ToList();

                    vmfAllKeys.AddRange(vmfSoundKeys);
                    vmfAllKeys.AddRange(vmfMaterialKeys);
                    vmfAllKeys.AddRange(vmfModelKeys);

                    Console.WriteLine("Finding sources of game content...");
                    GetSourceDirectories(gameFolder);

                    Console.WriteLine("Reading BSP...");
                    BSP map = new BSP(new FileInfo(bspPath));

                    Console.WriteLine("Searching complementary files...");
                    AssetUtils.findBspUtilityFiles(map, sourceDirectories);

                    Console.WriteLine("Initializing pak file...");
                    PakFile pak = new PakFile(map , sourceDirectories);

                    //GenerateFileList();                  

                    Console.WriteLine("Running bspzip...");

                    //PackBSP();

                    Console.WriteLine("Finished packing!");


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

        static void GenerateFileList()
        {
            var fileList = new List<string>();

            var vmfContent = GetContentFromVMF();

            fileList.AddRange(vmfContent);


            Console.WriteLine("Searching through materials...");
            foreach (var content in vmfContent)
            {
                if (Path.GetExtension(content) == ".vmt")
                {
                    fileList.AddRange(GetMaterialReferences(content));
                }

                if (Path.GetExtension(content) == ".mdl")
                {
                    fileList.AddRange(GetModelReferences(content));
                }
            }

            fileList = fileList.Distinct().ToList();

            Console.WriteLine("Writing file list...");

            var outputLines = new List<string>();
            foreach (var f in fileList)
            {
                string file = f.Replace("/", "\\");

                string trimmedPath = file;

                //remove all stuff we dont want, very hacky
                foreach (string directory in sourceDirectories.OrderByDescending(dir=>dir.Length))
                {
                    trimmedPath = trimmedPath.Replace(directory, "");
                }
                trimmedPath = trimmedPath.Replace(gameFolder, "").TrimStart('\\');

                outputLines.Add(trimmedPath);
                outputLines.Add(file);

                Console.WriteLine(file.Replace(gameFolder, ""));
            }
            if (File.Exists("files.txt"))
                File.Delete("files.txt");
            File.WriteAllLines("files.txt", outputLines);



            Console.WriteLine("BSP packed.");
        }

        static void PackBSP()
        {
            string arguments = "-addlist \"$bspnew\"  \"$list\" \"$bspold\" -game \"$game\"";
            arguments = arguments.Replace("$bspnew", bspPath);
            arguments = arguments.Replace("$bspold", bspPath);
            arguments = arguments.Replace("$list", "files.txt");
            arguments = arguments.Replace("$game", gameFolder);

            Console.WriteLine("Running");

            var p = new Process { StartInfo = { Arguments = arguments, FileName = bspZip, UseShellExecute = false, RedirectStandardOutput = true } };

            p.OutputDataReceived += p_OutputDataReceived;

            p.Start();
        }

        static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        static IEnumerable<string> GetModelReferences(string mdlPath)
        {
            var references = new List<string>();

            var variations = new List<string> { ".dx80.vtx", ".dx90.vtx", ".phy", ".sw.vtx", ".sw.vtx" };
            foreach (string variation in variations)
            {
                string variant = Path.ChangeExtension(mdlPath, variation);
                if (SourceFileExists(gameFolder, variant))
                    references.Add(GetSourceFile(gameFolder, variant));
            }

            string materialFile = mdlPath.Replace(@"\models", @"\materials\models").Replace(".mdl", ".vmt");
            if (SourceFileExists(gameFolder, materialFile))
            {
                references.Add(GetSourceFile(gameFolder, materialFile));
                references.AddRange(GetMaterialReferences(materialFile));
            }

            return references;
        }


        static IEnumerable<string> GetMaterialReferences(string vmtpath)
        {
            try
            {
                var vmtLines = File.ReadAllLines(GetSourceFile(gameFolder, vmtpath));

                var textureLines = vmtLines.Where(l => vmtTexturekeyWords.Any(t => l.ToLower().Contains(t.ToLower())));


                var contentFiles = new List<string>();

                foreach (string line in textureLines)
                {
                    if (IsValidFilename(GetValue(line)))
                    {
                        string path;
                        if (GetValue(line).EndsWith(".vtf"))
                        {
                            path = (Path.Combine("materials", GetValue(line))).Replace("/", "\\");
                        }
                        else
                        {
                            path = (Path.Combine("materials", GetValue(line)) + ".vtf").Replace("/", "\\");
                        }
                        if (SourceFileExists(gameFolder, path))
                            contentFiles.Add(GetSourceFile(gameFolder, path));
                    }
                }

                var materialLines = vmtLines.Where(l => vmtMaterialkeyWords.Any(l.Contains));


                foreach (string line in materialLines)
                {
                    string path = DeterminePath(GetKey(line), GetValue(line));

                    if (IsValidFilename(path))
                        if (SourceFileExists(gameFolder, path))
                            contentFiles.AddRange(GetMaterialReferences(path));
                }
                return contentFiles.Distinct().ToList();

            }
            catch
            {
                return new List<string>(0);
            }
        }

        static string GetSourceFile(string gamePath, string filePath)
        {
            string naive = Path.Combine(gamePath, filePath);
            if (File.Exists(naive))
                return naive;

            var subDirs = sourceDirectories;
            foreach (string subDir in subDirs)
            {
                string guess = Path.Combine(subDir, filePath);
                if (File.Exists(guess))
                    return guess;
            }

            return "";
        }

        static bool SourceFileExists(string gamePath, string filePath)
        {

            if (File.Exists(Path.Combine(gamePath, filePath)))
                return true;

            var subDirs = sourceDirectories;
            foreach (string subDir in subDirs)
            {
                if (File.Exists(Path.Combine(subDir, filePath)))
                    return true;
            }

            return false;
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


        static List<string> GetContentFromVMF()
        {
            var vmfLines = File.ReadAllLines(vmfPath);

            var contentLines = vmfLines.Where(l => vmfAllKeys.Any(l.Contains));


            var contentFiles = new List<string>();

            foreach (string line in contentLines)
            {
                string path = DeterminePath(GetKey(line), GetValue(line));
                if (SourceFileExists(gameFolder, path))
                    contentFiles.Add(GetSourceFile(gameFolder, path));
            }

            return contentFiles.Distinct().ToList();

        }

        static string DeterminePath(string key, string value)
        {
            string contentPath = "";

            if (vmfModelKeys.Contains(key))
                contentPath = value;

            if (vmfMaterialKeys.Contains(key))
                contentPath = Path.Combine("materials", value) + ".vmt";

            if (vmfSoundKeys.Contains(key))
                contentPath = Path.Combine("sound", value);


            return contentPath;
        }


        static string GetValue(string line)
        {
            return line.Split(' ').Last().Replace("\"", "").Trim();
        }

        static string GetKey(string line)
        {
            return line.Split(' ').First().Replace("\"", "").Trim();
        }

        static bool IsValidFilename(string testName)
        {
            Regex containsABadCharacter = new Regex("["
                  + Regex.Escape(new string(System.IO.Path.GetInvalidPathChars())) + "]");
            if (containsABadCharacter.IsMatch(testName)) { return false; };

            // other checks for UNC, drive-path format, etc

            return true;
        }
    }
}
