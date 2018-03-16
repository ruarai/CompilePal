using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using CompilePalX.Compiling;
using MahApps.Metro;

namespace CompilePalX
{
    //Note: Must be added to all local filepaths, otherwise
    //command line args fail because it looks in folder of calling programs
    static class CompilePalPath
    {
        //Get compile pal location
        private static string fullPath = Assembly.GetExecutingAssembly().Location;
        //Remove filename
        public static string Directory { get; } = Path.GetDirectoryName(fullPath)+"\\";
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //Setup selected theme
        private void OnStartup(object sender, StartupEventArgs e)
        {
            //Read current theme from saved file
            if (File.Exists(CompilePalPath.Directory + "Themes/Theme.txt"))
            {
                using (FileStream fs = new FileStream(CompilePalPath.Directory + "Themes/Theme.txt", FileMode.Open))
                {
                    StreamReader sr = new StreamReader(fs);
                    string themePath = sr.ReadLine();

                    //If selected file cannot be found, fallback to default theme
                    if (!File.Exists(themePath))
                        themePath = CompilePalPath.Directory + "Themes/CompilePalTheme.xaml";


                    if (!String.IsNullOrEmpty(themePath))
                    {
                        ThemeManager.AddAccent("theme", new Uri(themePath));

                        ThemeManager.ChangeAppStyle(this, 
                            ThemeManager.GetAccent("theme"), 
                            ThemeManager.GetAppTheme("BaseLight"));
                    }

                }
            }
            else
            {
                CompilePalLogger.LogLine($"Error: Could not locate {CompilePalPath.Directory}Themes/Theme.txt");
            }
        }
    }
}
