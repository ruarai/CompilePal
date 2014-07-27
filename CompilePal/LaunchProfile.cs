using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CompilePal
{
    interface ILaunchProfile
    {
        GameConfiguration GetConfiguration();

        string Name { get; set; }
    }

    public class GameConfiguration
    {
        public string VVISPath;
        public string VBSPPath;
        public string VRADPath;

        public string GamePath;
        public string MapPath;
    }

    class CSGO : ILaunchProfile
    {
        public GameConfiguration GetConfiguration()
        {
            GameConfiguration config = new GameConfiguration();;

            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Hammer\General");

            string binFolder = (string)rk.GetValue("Directory");
            string gameData = Path.Combine(binFolder, "GameConfig.txt");

            var lines = File.ReadAllLines(gameData);


            //Lazy parsing
            config.VBSPPath = lines[17].Split('"')[3];
            config.VVISPath = lines[18].Split('"')[3];
            config.VRADPath = lines[19].Split('"')[3];

            config.GamePath = lines[6].Split('"')[3];
            config.MapPath = lines[22].Split('"')[3];

            return config;
        }

        public string Name {
            get { return "Counter Strike: Global Offensive"; }
            set { }
        }
    }

}
