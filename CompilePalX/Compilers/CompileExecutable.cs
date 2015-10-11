using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers
{
    class CompileExecutable : CompileProcess
    {
        public CompileExecutable(string metadata)
            : base(metadata)
        {
            if (!Directory.Exists(runningDirectory))
                Directory.CreateDirectory(runningDirectory);
        }


        private static string runningDirectory = "CompileLogs";

        public List<Error> CompileErrors;

        public override void Run(CompileContext c)
        {
            CompileErrors = new List<Error>();
            Process = new Process
            {
                StartInfo =
                {
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var args = GameConfigurationManager.SubstituteValues(GetParameterString(), c.MapFile);;

            Process.StartInfo.FileName = Path;
            Process.StartInfo.Arguments = string.Join(" ", args);
            Process.StartInfo.WorkingDirectory = runningDirectory;

            Process.Start();
            Process.PriorityClass = ProcessPriorityClass.BelowNormal;

            char[] buffer = new char[256];
            Task<int> read = null;

            while (true)
            {
                if (read == null)
                    read = Process.StandardOutput.ReadAsync(buffer, 0, buffer.Length);

                read.Wait(100); // an arbitray timeout

                if (read.IsCompleted)
                {
                    if (read.Result > 0)
                    {
                        string text = new string(buffer, 0, read.Result);

                        var error = GetError(text);

                        if (error != null)
                        {
                            if (error.Severity == 5)
                            {
                                CompilePalLogger.LogLineColor("An error cancelled the compile.", Brushes.Red);
                                ProgressManager.ErrorProgress();
                                return;
                            }

                            CompilePalLogger.LogCompileError(text, error);

                            CompileErrors.Add(error);
                        }
                        else
                            CompilePalLogger.Log(text);

                        read = null; // ok, this task completed so we need to create a new one
                        continue;
                    }

                    // got -1, process ended
                    break;
                }
            }

            Process.WaitForExit();
        }

        private static string lineBuffer = String.Empty;

        private static Error GetError(string text)
        {
            //The process of trying to sort the random spouts of letters back into lines. Hacky.

            if (text.Contains("\n"))
            {
                lineBuffer += text;

                List<string> lines = lineBuffer.Split('\n').ToList();

                lineBuffer = lines.Last();

                foreach (string line in lines)
                {
                    var error = ErrorFinder.GetError(line);

                    if (error != null)
                        return error;
                }
            }
            else
                lineBuffer += text;

            return null;
        }
    }
}
