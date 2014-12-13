using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for LaunchWindow.xaml
    /// </summary>
    public partial class LaunchWindow
    {
        public LaunchWindow()
        {
            string gameConfigurationFolder = "GameConfiguration";
            string gameConfigurationsPath = Path.Combine(gameConfigurationFolder, "gameConfigs.json");

            InitializeComponent();

            if (!Directory.Exists(gameConfigurationFolder))
                Directory.CreateDirectory(gameConfigurationFolder);

            //Loading the last used configurations for hammer
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Hammer\General");

            string BinFolder = (string)rk.GetValue("Directory");


            string gameData = Path.Combine(BinFolder, "GameConfig.txt");

            List<GameConfiguration> gameConfigs = GameConfigurationParser.Parse(gameData);

            if (File.Exists(gameConfigurationsPath))
            {
                //load
                string jsonLoadText = File.ReadAllText(gameConfigurationsPath);
                gameConfigs.AddRange(JsonConvert.DeserializeObject<List<GameConfiguration>>(jsonLoadText));
            }

            //remove duplicates
            gameConfigs = gameConfigs.GroupBy(g => g.Name).Select(grp => grp.First()).ToList();

            //save
            string jsonSaveText = JsonConvert.SerializeObject(gameConfigs,Formatting.Indented);
            File.WriteAllText(gameConfigurationsPath, jsonSaveText);

            if (gameConfigs.Count == 1)
                Launch(gameConfigs.First());


            GameGrid.ItemsSource = gameConfigs;
        }

        private void Launch(GameConfiguration config)
        {
            GameConfigurationManager.GameConfiguration = config;
            var c = new MainWindow();
            c.Show();

            Close();
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (GameConfiguration) GameGrid.SelectedItem;

            if(selectedItem!=null)
                Launch(selectedItem);
        }
    }
}
