using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers
{
    class NavProcess : CompileProcess
    {

        private static string mapname;
        private static string mapnav;
        private static string mapcfg;
        private static string mapCFGBackup;

        private bool hidden;
        private string mapLogPath;
        public NavProcess() : base("NAV") { }

        public override void Run(CompileContext context, CancellationToken cancellationToken)
        {
            CompileErrors = new List<Error>();

            if (!CanRun(context))
            {
                return;
            }

            try
            {
                CompilePalLogger.LogLine("\nCompilePal - Nav Generator");

                if (!File.Exists(context.CopyLocation))
                {
                    throw new FileNotFoundException();
                }

                mapname = Path.GetFileName(context.CopyLocation).Replace(".bsp", "");
                mapnav = context.CopyLocation.Replace(".bsp", ".nav");
                mapcfg = context.Configuration.GameFolder + "/cfg/" + mapname + ".cfg";
                mapCFGBackup = context.Configuration.GameFolder + "/cfg/" + mapname + "_cpalbackup.cfg";
                var mapLog = mapname + "_nav.log";
                mapLogPath = Path.Combine(context.Configuration.GameFolder, mapLog);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                DeleteNav(mapname, context.Configuration.GameFolder);

                hidden = GetParameterString().Contains("-hidden");

                var addtionalParameters = Regex.Replace(GetParameterString(), "\b-hidden\b", "");

                var args =
                    $"-steam -game \"{context.Configuration.GameFolder}\" -windowed -novid -nosound +log 0 +sv_logflush 1 +sv_cheats 1 +map {mapname} {addtionalParameters}";

                if (hidden)
                {
                    args += " -noborder -x 4000 -y 2000";
                }

                var startInfo = new ProcessStartInfo(context.Configuration.GameEXE, args);
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = false;

                CompilePalLogger.LogLine("Generating...");
                if (File.Exists(mapcfg))
                {
                    if (File.Exists(mapCFGBackup))
                    {
                        File.Delete(mapCFGBackup);
                    }
                    File.Move(mapcfg, mapCFGBackup);
                }

                if (File.Exists(mapLogPath))
                {
                    File.Delete(mapLogPath);
                }

                File.Create(mapcfg).Dispose();
                TextWriter tw = new StreamWriter(mapcfg);
                tw.WriteLine("con_logfile " + mapLog);
                tw.WriteLine("nav_generate");
                tw.Close();

                using (TextReader tr = new StreamReader(File.Open(mapLogPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite)))
                {
                    Process = new Process
                    {
                        StartInfo = startInfo
                    };
                    Process.Exited += Process_Exited;
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

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
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
            var navdirs = BSPPack.BSPPack.GetSourceDirectories(gamefolder, false);
            foreach (var source in navdirs)
            {
                var externalPath = source + "/maps/" + mapname + ".nav";

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
            {
                try
                {
                    Process.Kill();
                }
                catch (Win32Exception) { }
            }
            else
            {
                CleanUp();
            }
        }
        private void CleanUp()
        {
            // give time for process to release file handles
            try
            {
                if (File.Exists(mapcfg))
                {
                    File.Delete(mapcfg);
                }
                if (File.Exists(mapCFGBackup))
                {
                    File.Move(mapCFGBackup, mapcfg);
                }
                if (File.Exists(mapLogPath))
                {
                    File.Delete(mapLogPath);
                }
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

        private void Process_Exited(object sender, EventArgs e)
        {
            CleanUp();
        }
    }
}
