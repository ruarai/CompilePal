using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CompilePalX
{
    internal delegate void UpdateFound();
    static class UpdateManager
    {
        public static event UpdateFound OnUpdateFound;

        public static int CurrentVersion;
        public static int LatestVersion;

        private const string UpdateURL = "https://raw.githubusercontent.com/ruarai/CompilePal/compilepalx/CompilePalX/version.txt";

        public static void CheckVersion()
        {
            try
            {
                var c = new WebClient();
                c.DownloadStringCompleted += c_DownloadStringCompleted;
                c.DownloadStringAsync(new Uri(UpdateURL));
            }
            catch { }
        }

        static void c_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {

            if (e.Error == null)
            {
                string currentVersion = File.ReadAllText("version.txt");
                CurrentVersion = int.Parse(currentVersion);

                string newVersion = e.Result;
                LatestVersion = int.Parse(newVersion);

                if (CurrentVersion < LatestVersion)
                {
                    MainWindow.ActiveDispatcher.Invoke(OnUpdateFound);
                }
            }
        }
    }
}
