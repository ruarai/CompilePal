using System;
using System.IO;
using System.Net;

namespace CompilePal
{
    class VersionChecker
    {
        private const string VersionURL = "https://raw.githubusercontent.com/ruarai/CompilePal/master/CompilePal/version.txt";

        public static void CheckVersion()
        {
            try
            {
                var c = new WebClient();
                c.DownloadStringCompleted += c_DownloadStringCompleted;
                c.DownloadStringAsync(new Uri(VersionURL));
            }
            catch { }
        }

        static void c_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {

            if (e.Error == null)
            {
                string currentVersion = File.ReadAllText("version.txt");

                Analytics.Version(currentVersion);

                string newVersion = e.Result;

                if (currentVersion != newVersion)
                {
                    var updateNeededWindow = new UpdateNeededWindow(newVersion,currentVersion);
                    updateNeededWindow.Show();
                }
            }
        }
    }
}
