using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
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

        private static string runningDirectory = ".";

        public override void Run(CompileContext c, CancellationToken cancellationToken)
        {
            CompileErrors = new List<Error>();

            if (!CanRun(c)) return;

            // listen for cancellations
            cancellationToken.Register(() =>
            {
                try
                {
                    if (Metadata.ReadOutput)
                    {
                        Process.OutputDataReceived -= ProcessOnOutputDataReceived;
                        Process.ErrorDataReceived -= ProcessOnOutputDataReceived;

                    }
                    Cancel();
                }
                catch (InvalidOperationException) { }
                catch (Exception e) { ExceptionHandler.LogException(e); }
            });

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

            Process.StartInfo.FileName = GameConfigurationManager.SubstituteValues(Metadata.Path);
            Process.StartInfo.Arguments = string.Join(" ", args);
            Process.StartInfo.WorkingDirectory = runningDirectory;

            CompilePalLogger.LogLineDebug($"Running '{Process.StartInfo.FileName}' with args '{Process.StartInfo.Arguments}'");

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    CompilePalLogger.LogDebug($"Cancelled {this.Metadata.Name}");
                    return;
                }
                Process.Start();
            }
            catch (Exception e)
            {
                CompilePalLogger.LogDebug(e.ToString());
                CompilePalLogger.LogCompileError($"Failed to run executable: {Process.StartInfo.FileName}\n", new Error($"Failed to run executable: {Process.StartInfo.FileName}", ErrorSeverity.FatalError));
                return;
            }
            Process.PriorityClass = ProcessPriorityClass.BelowNormal;

            if (Metadata.ReadOutput)
            { 
                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();
                Process.OutputDataReceived += ProcessOnOutputDataReceived;
                Process.ErrorDataReceived += ProcessOnOutputDataReceived;

                Process.WaitForExitAsync(cancellationToken).Wait(cancellationToken);

                if (Metadata.CheckExitCode && Process.ExitCode != 0)
                    CompilePalLogger.LogCompileError($"{Name} exited with code: {Process.ExitCode}\n", new Error($"{Name} exited with code: {Process.ExitCode}", ErrorSeverity.Warning));
            }
        }

        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;

            // listen for variable updates for plugins
            if (e.Data.StartsWith("COMPILE_PAL_SET"))
                GameConfigurationManager.ModifyCurrentContext(e.Data);
            else
                CompilePalLogger.LogLineChecked(e.Data);
        }
    }
}
