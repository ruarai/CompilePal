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

        private static string runningDirectory = "./CompileLogs";

        public override void Run(CompileContext c)
        {
            CompileErrors = new List<Error>();
            Process = new Process();

            if (Metadata.ReadOutput)
            {
                Process.StartInfo = new ProcessStartInfo
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
            }

            var args = GameConfigurationManager.SubstituteValues(GetParameterString(), c.MapFile);
            var filename = GameConfigurationManager.SubstituteValues(Metadata.Path);
            if (!File.Exists(filename.Replace("\"", "")))
            {
                CompilePalLogger.LogCompileError($"Failed to find executable: {filename}\n", new Error("Failed to find executable: {filename}", ErrorSeverity.FatalError));
                return;
            }

            Process.StartInfo.FileName = filename;
            Process.StartInfo.Arguments = string.Join(" ", args);
            Process.StartInfo.WorkingDirectory = runningDirectory;

            Process.Start();
            Process.PriorityClass = ProcessPriorityClass.BelowNormal;

            if (Metadata.ReadOutput)
                readOutput();
        }

        private void readOutput()
        {
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

                        CompilePalLogger.ProgressiveLog(text);

                        read = null; // ok, this task completed so we need to create a new one
                        continue;
                    }

                    // got -1, process ended
                    break;
                }
            }
            Process.WaitForExit();
        }

    }
}
