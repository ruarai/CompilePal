using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SharpConfig;

namespace CompilePal
{
    /// <summary>
    /// Interaction logic for LaunchWindow.xaml
    /// </summary>
    public partial class LaunchWindow
    {
        static Config launchConfig = new Config("launch",true,true);
        public LaunchWindow()
        {
            InitializeComponent();

            //Loading the last used configurations for hammer
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Hammer\General");

            string BinFolder = (string)rk.GetValue("Directory");
            string gameData = Path.Combine(BinFolder, "GameConfig.txt");

            List<GameInfo> gameInfos = GameConfigs.Parse(gameData);

            if(launchConfig.Values.ContainsKey("infos"))
                gameInfos.AddRange(launchConfig["infos"].ToObject<List<GameInfo>>());

            //remove duplicates
            gameInfos = gameInfos.GroupBy(g => g.Name).Select(grp => grp.First()).ToList();

            launchConfig["infos"] = gameInfos;


            if (gameInfos.Count == 1)
                Launch(gameInfos[0]);

            GameGrid.ItemsSource = gameInfos;
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            Launch((GameInfo)GameGrid.SelectedItem);
        }

        private void Launch(GameInfo gameInfo)
        {
            var c = new MainWindow(gameInfo);
            c.Show();

            Close();
        }
    }
}
