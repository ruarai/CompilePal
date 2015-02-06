using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CompilePalX.Compiling;

namespace CompilePalX
{
    internal delegate void UpdateFound();
    static class UpdateManager
    {
        public static event UpdateFound OnUpdateFound;

        public static int CurrentVersion;
        public static int LatestVersion;

        private const string UpdateURL = "https://raw.githubusercontent.com/ruarai/CompilePal/master/CompilePalX/version.txt";

        public static void CheckVersion()
        {
            string currentVersion = File.ReadAllText("version.txt");
            CurrentVersion = int.Parse(currentVersion);

            try
            {
                Logger.LogLine("Downloading update information.");

                var c = new WebClient();
                c.DownloadStringCompleted += c_DownloadStringCompleted;
                c.DownloadStringAsync(new Uri(UpdateURL));
            }
            catch (Exception e)
            {
                Logger.LogLine("Failed to find update information due to following exception:");
                Logger.LogLine(e.ToString());
            }
        }

        static void c_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {

            if (e.Error == null)
            {

                string newVersion = e.Result;
                LatestVersion = int.Parse(newVersion);

                Logger.LogLine("Updater found latest version is:{0}, current is {1}.",LatestVersion,CurrentVersion);

                if (CurrentVersion < LatestVersion)
                {
                    MainWindow.ActiveDispatcher.Invoke(OnUpdateFound);

                    Logger.LogLine("Updater found that Compile Pal is outdated.");
                }
                else
                {
                    Logger.LogLine("Updater found that Compile Pal is up to date.");
                }

                ProgressManager.SetProgress(ProgressManager.Progress);
            }
            else
            {
                Logger.LogLine("Failed to find update information as an error was returned:");
                Logger.LogLine(e.Error.ToString());
            }
        }
    }
}
