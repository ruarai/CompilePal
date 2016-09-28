using CompilePalX.Compiling;
using System;
using System.Collections.Generic;
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
        public NavProcess() : base("Parameters\\BuiltIn\\nav.meta") { }

        static string mapname;
        static string mapnav;
        static string mapcfg;
        static string mapCFGBackup;

        public override void Run(CompileContext context)
        {

            CompilePalLogger.LogLine("\nCompilePal - Nav Generator");
            mapname = System.IO.Path.GetFileName(context.BSPFile).Replace(".bsp", "");
            mapnav = context.CopyLocation.Replace(".bsp", ".nav");
            mapcfg = context.Configuration.GameFolder + "/cfg/" + mapname + ".cfg";
            mapCFGBackup = context.Configuration.GameFolder + "/cfg/" + mapname + "_cpalbackup.cfg";

            string args = "-game \"" + context.Configuration.GameFolder + "\" -windowed -novid +sv_cheats 1 +map " + mapname;
            var startInfo = new ProcessStartInfo(context.Configuration.GameEXE, args);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = false;

            var p = new Process { StartInfo = startInfo };
            p.Start();
            CompilePalLogger.LogLine("Generating...");
            if (File.Exists(mapcfg))
            {
                if (File.Exists(mapCFGBackup))
                    System.IO.File.Delete(mapCFGBackup);
                System.IO.File.Move(mapcfg, mapCFGBackup);
            }

            System.IO.File.Create(mapcfg).Dispose();
            TextWriter tw = new StreamWriter(mapcfg);
            tw.WriteLine("nav_generate");
            tw.Close();

            FileSystemWatcher fw = new FileSystemWatcher();
            fw.Path = System.IO.Path.GetDirectoryName(mapnav);
            fw.Filter = "*.nav";
            fw.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            fw.Changed += new FileSystemEventHandler(fileSystemWatcher_NavCreated);
            fw.Created += new FileSystemEventHandler(fileSystemWatcher_NavCreated);
            fw.EnableRaisingEvents = true;

            p.WaitForExit();
            fw.Dispose();

            if (File.Exists(mapcfg))
                File.Delete(mapcfg);
            if (File.Exists(mapCFGBackup))
                System.IO.File.Move(mapCFGBackup, mapcfg);
            CompilePalLogger.LogLine("nav file complete!");
        }

        static void fileSystemWatcher_NavCreated(object sender, FileSystemEventArgs e)
        {
            File.WriteAllText(mapcfg, "exit");
        }
    }
}
