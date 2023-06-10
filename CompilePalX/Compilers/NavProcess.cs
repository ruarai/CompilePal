using CompilePalX.Compiling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CompilePalX.Compilers
{
    class NavProcess : CompileProcess
    {
        public NavProcess() : base("NAV") { }

        static string mapname;
        static string mapnav;
        static string mapcfg;
        static string mapCFGBackup;
        private string mapLogPath;

        bool hidden;

        public override void Run(CompileContext context, CancellationToken cancellationToken)
        {
            CompileErrors = new List<Error>();

            if (!CanRun(context)) return;

            try
            {
                CompilePalLogger.LogLine("\nCompilePal - Nav Generator");

                if (!File.Exists(context.CopyLocation))
                {
                    throw new FileNotFoundException();
                }

                mapname = System.IO.Path.GetFileName(context.CopyLocation).Replace(".bsp", "");
                mapnav = context.CopyLocation.Replace(".bsp", ".nav");
                mapcfg = context.Configuration.GameFolder + "/cfg/" + mapname + ".cfg";
                mapCFGBackup = context.Configuration.GameFolder + "/cfg/" + mapname + "_cpalbackup.cfg";
                string mapLog = mapname + "_nav.log";
                mapLogPath = Path.Combine(context.Configuration.GameFolder, mapLog);

                if (cancellationToken.IsCancellationRequested) return;
                DeleteNav(mapname, context.Configuration.GameFolder);

                hidden = GetParameterString().Contains("-hidden");

                var addtionalParameters = Regex.Replace(GetParameterString(), "\b-hidden\b", "");

                string args =
                    $"-steam -game \"{context.Configuration.GameFolder}\" -windowed -insecure -novid -nosound +log 0 +sv_logflush 1 +sv_cheats 1 +map {mapname} {addtionalParameters}";

                if (hidden)
                    args += " -noborder -x 4000 -y 2000";

                var startInfo = new ProcessStartInfo(context.Configuration.GameEXE, args);
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = false;

                CompilePalLogger.LogLine("Generating...");
                if (File.Exists(mapcfg))
                {
                    if (File.Exists(mapCFGBackup))
                        System.IO.File.Delete(mapCFGBackup);
                    System.IO.File.Move(mapcfg, mapCFGBackup);
                }

                if (File.Exists(mapLogPath))
                    File.Delete(mapLogPath);

                System.IO.File.Create(mapcfg).Dispose();
                TextWriter tw = new StreamWriter(mapcfg);
                tw.WriteLine("con_logfile " + mapLog);
                tw.WriteLine("nav_generate");
                tw.Close();

                using (TextReader tr = new StreamReader(File.Open(mapLogPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite)))
                {
                    Process = new Process { StartInfo = startInfo };
                    Process.Exited += new EventHandler(Process_Exited);
                    Process.EnableRaisingEvents = true;
                    Process.Start();
                
                    string line;
                    do
                    {
                        Thread.Sleep(100);
                        line = tr.ReadLine();
                    } while ((line == null || !line.Contains(".nav' saved.")) && !cancellationToken.IsCancellationRequested);
                }
                
                ExitClient();

                if (cancellationToken.IsCancellationRequested) return;
                CompilePalLogger.LogLine("nav file complete!");
            }
            catch (FileNotFoundException)
            {
                CompilePalLogger.LogCompileError($"Could not find {context.CopyLocation}\n", new Error($"Could not find {context.CopyLocation}", "Nav failed", ErrorSeverity.Error));
            }
            catch (Exception exception)
            {
                CompilePalLogger.LogLine("Something broke:");
                CompilePalLogger.LogCompileError($"{exception}\n", new Error(exception.ToString(), "CompilePal Internal Error", ErrorSeverity.FatalError));
            }
        }

        private static void DeleteNav(string mapname, string gamefolder)
        {
            List<string> navdirs = BSPPack.BSPPack.GetSourceDirectories(gamefolder, false);
            foreach (string source in navdirs)
            {
                string externalPath = source + "/maps/" + mapname + ".nav";

                if (File.Exists(externalPath))
                {
                    CompilePalLogger.LogLine("Deleting existing nav file.");
                    File.Delete(externalPath);
                }
            }
        }

        private void ExitClient()
        {
            if (Process != null && !Process.HasExited)
                try
                {
                    this.Process.Kill();
                }
                catch (Win32Exception) { }
            else
                CleanUp();
        }
        private void CleanUp()
        {
            // give time for process to release file handles
            Thread.Sleep(500);
            try
            {
                if (File.Exists(mapcfg))
                    File.Delete(mapcfg);
                if (File.Exists(mapCFGBackup))
                    System.IO.File.Move(mapCFGBackup, mapcfg);
                if (File.Exists(mapLogPath))
                    File.Delete(mapLogPath);
            }
            catch (Exception e)
            {
                CompilePalLogger.LogCompileError($"Failed to cleanup temporary file: {e}\n", new Error($"Failed to cleanup temporary file: {e}\n", "CompilePal Internal Error", ErrorSeverity.Info));
            }
        }

        public override void Cancel()
        {
            ExitClient();
        }

        void Process_Exited(object sender, EventArgs e)
        {
            CleanUp();
        }
    }
}
