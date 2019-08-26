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

        public override void Run(CompileContext context)
        {
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

                deleteNav(mapname, context.Configuration.GameFolder);

            hidden = GetParameterString().Contains("-hidden");
            bool textmode = GetParameterString().Contains("-textmode");

            string args = "-steam -game \"" + context.Configuration.GameFolder + "\" -windowed -novid -nosound +log 0 +sv_logflush 1 +sv_cheats 1 +map " + mapname;

            if (hidden)
                args += " -noborder -x 4000 -y 2000";

            if (textmode)
                args += " -textmode";

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
                } while (line == null || !line.Contains(".nav' saved."));
                
                exitClient();
            }

                CompilePalLogger.LogLine("nav file complete!");
            }
            catch (FileNotFoundException)
            {
                CompilePalLogger.LogLine("FAILED - Could not find " + context.CopyLocation);
            }
            catch (Exception exception)
            {
                CompilePalLogger.LogLine("Something broke:");
                CompilePalLogger.LogLine(exception.ToString());
            }
        }

        private void deleteNav(string mapname, string gamefolder)
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

        private void exitClient()
        {
            if (Process != null && !Process.HasExited)
                try
                {
                    this.Process.Kill();
                }
                catch (Win32Exception) { }
            else
                cleanUp();
        }
        private void cleanUp()
        {
            if (File.Exists(mapcfg))
                File.Delete(mapcfg);
            if (File.Exists(mapCFGBackup))
                System.IO.File.Move(mapCFGBackup, mapcfg);
            if (File.Exists(mapLogPath))
                File.Delete(mapLogPath);
        }

        public override void Cancel()
        {
            exitClient();
        }

        void Process_Exited(object sender, EventArgs e)
        {
            cleanUp();
        }
    }
}
