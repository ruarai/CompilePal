using System.Collections.Generic;
using CompilePalX.Compiling;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CompilePalX.Compilers
{
    class ShutdownProcess : CompileProcess
    {
        public ShutdownProcess() : base("SHUTDOWN") { }

        public override void Run(CompileContext context, CancellationToken cancellationToken)
        {

            CompileErrors = [];
            if (!CanRun(context)) return;

            if (cancellationToken.IsCancellationRequested)
                return;

            // don't run unless it's the last map of the queue
            if (CompilingManager.MapFiles.Last().File == context.MapFile)
            {
                CompilePalLogger.LogLine("\nCompilePal - Shutdown");
                CompilePalLogger.LogLine("The system will shutdown soon.");
                CompilePalLogger.LogLine("You can cancel this shutdown by using the command \"shutdown -a\"");

                var startInfo = new ProcessStartInfo("shutdown", GetParameterString());
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                Process = new Process { StartInfo = startInfo };
                Process.Start();
            }
        }
    }
}
