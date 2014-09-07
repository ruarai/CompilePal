using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BSPAutoPack
{
    class Program
    {
        private static string bspZip;
        private static string gameFolder;
        private static string bspPath;
        private static string vmfPath;

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
                vmtTexturekeyWords = File.ReadAllLines(Path.Combine("keys", "texturekeys.txt")).ToList();
                vmtMaterialkeyWords = File.ReadAllLines(Path.Combine("keys", "materialkeys.txt")).ToList();

                vmfSoundKeys = File.ReadAllLines(Path.Combine("keys", "vmfsoundkeys.txt")).ToList();
                vmfMaterialKeys = File.ReadAllLines(Path.Combine("keys", "vmfmaterialkeys.txt")).ToList();
                vmfModelKeys = File.ReadAllLines(Path.Combine("keys","vmfmodelkeys.txt")).ToList();

                vmfAllKeys.AddRange(vmfSoundKeys);
                vmfAllKeys.AddRange(vmfMaterialKeys);
                vmfAllKeys.AddRange(vmfModelKeys);

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

                    if (args[i] == "-bspzip")
                    {
                        i++;
                        bspZip = args[i];
                    }
                }
                if (gameFolder != null && bspPath != null && vmfPath != null && bspZip != null)
                {

                    Console.WriteLine("Discovering files...");

                    GenerateFileList();

                    Console.WriteLine("Running bspzip...");

                    PackBSP();

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

            Console.WriteLine("Writing file list...");

            var outputLines = new List<string>();
            foreach (var f in fileList)
            {
                string file = f.Replace("/", "\\");
                outputLines.Add(file.Replace(gameFolder, "").TrimStart('\\'));
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
            arguments = arguments.Replace("$list", @"C:\Users\Ruarai\Dropbox\My Documents\Visual Studio 2013\Projects\BSPAutoPack\BSPAutoPack\bin\Debug\" + "files.txt");
            arguments = arguments.Replace("$game", gameFolder);

            Process p = new Process();
            p.StartInfo.Arguments = arguments;
            p.StartInfo.FileName = bspZip;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;

            p.OutputDataReceived += p_OutputDataReceived;

            p.Start();
        }

        static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        static List<string> GetModelReferences(string mdlPath)
        {
            var references = new List<string>();

            var variations = new List<string>() {".dx80.vtx",".dx90.vtx",".phy",".sw.vtx",".sw.vtx"};
            foreach (string variation in variations)
            {
                string variant = mdlPath.Replace(".mdl", variation);
                if(File.Exists(variant))
                    references.Add(variant);
            }

            string materialFile = mdlPath.Replace(@"\models", @"\materials\models").Replace(".mdl",".vmt");
            if (File.Exists(materialFile))
            {
                references.Add(materialFile);
                references.AddRange(GetMaterialReferences(materialFile));
            }

            return references;
        }


        static List<string> GetMaterialReferences(string vmtpath)
        {
            var vmtLines = File.ReadAllLines(vmtpath);

            var textureLines = vmtLines.Where(l => vmtTexturekeyWords.Any(t => l.ToLower().Contains(t.ToLower())));


            var contentFiles = new List<string>();

            foreach (string line in textureLines)
            {
                string path;
                if (GetValue(line).EndsWith(".vtf"))
                {
                    path = (Path.Combine(gameFolder, "materials", GetValue(line))).Replace("/", "\\");
                }
                else
                {
                    path = (Path.Combine(gameFolder, "materials", GetValue(line)) + ".vtf").Replace("/", "\\");
                }
                if (File.Exists(path))
                    contentFiles.Add(path);
            }

            var materialLines = vmtLines.Where(l => vmtMaterialkeyWords.Any(l.Contains));


            foreach (string line in materialLines)
            {
                string path = DeterminePath(GetKey(line), GetValue(line));
                if (File.Exists(path))
                    contentFiles.AddRange(GetMaterialReferences(path));
            }
            return contentFiles.Distinct().ToList();

        }

        static List<string> GetContentFromVMF()
        {
            var vmfLines = File.ReadAllLines(vmfPath);

            var contentLines = vmfLines.Where(l => vmfAllKeys.Any(l.Contains));


            var contentFiles = new List<string>();

            foreach (string line in contentLines)
            {
                string path = DeterminePath(GetKey(line), GetValue(line));
                if (File.Exists(path))
                    contentFiles.Add(path);
            }

            return contentFiles.Distinct().ToList();

        }

        static string DeterminePath(string key, string value)
        {
            string contentPath = "";

            if (vmfModelKeys.Contains(key))
                contentPath = Path.Combine(gameFolder, value);

            if (vmfMaterialKeys.Contains(key))
                contentPath = Path.Combine(gameFolder, "materials", value) + ".vmt";

            if (vmfSoundKeys.Contains(key))
                contentPath = Path.Combine(gameFolder, "sound", value);


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
    }
}
