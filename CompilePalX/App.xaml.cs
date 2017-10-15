using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

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
        
    }
}
