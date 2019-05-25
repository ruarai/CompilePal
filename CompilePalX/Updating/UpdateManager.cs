using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CompilePalX.Compiling;

namespace CompilePalX
{
    internal delegate void UpdateFound();
    static class UpdateManager
    {
        public static event UpdateFound OnUpdateFound;

        public static float CurrentVersion;
        public static float CurrentPrereleaseVersion;
        public static float LatestVersion;
        public static float LatestPrereleaseVersion;

        private const string UpdateURL = "https://raw.githubusercontent.com/ruarai/CompilePal/master/CompilePalX/version.txt";

        public static void CheckVersion()
        {
            string currentVersion = File.ReadAllText(CompilePalPath.Directory + "version.txt");
            string currentPrereleaseVersion = File.ReadAllText(CompilePalPath.Directory + "version_prerelease.txt");
            CurrentVersion = float.Parse(currentVersion);
            CurrentPrereleaseVersion = float.Parse(currentPrereleaseVersion);

            if (CurrentPrereleaseVersion > CurrentVersion)
	            CurrentVersion = CurrentPrereleaseVersion;

            Thread updaterThread = new Thread(ThreadedCheck);
            updaterThread.Start();
        }

        static void ThreadedCheck()
        {
            try
            {
                CompilePalLogger.LogLine("Fetching update information...");

                var c = new WebClient();
                string newVersion = c.DownloadString(new Uri(UpdateURL));

                LatestVersion = int.Parse(newVersion);

                if (CurrentVersion < LatestVersion)
                {
                    MainWindow.ActiveDispatcher.Invoke(OnUpdateFound);

                    CompilePalLogger.LogLine("Updater found that Compile Pal is outdated.");
                }
                else
                {
                    CompilePalLogger.LogLine("Updater found that Compile Pal is up to date.");
                }

                ProgressManager.SetProgress(ProgressManager.Progress);
            }
            catch (WebException e)
            {
                CompilePalLogger.LogLine("Failed to find update information as an error was returned:");
                CompilePalLogger.LogLine(e.ToString());
            }
        }
    }
}
