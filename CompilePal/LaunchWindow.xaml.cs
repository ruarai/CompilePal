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
using System.Windows.Shapes;
using Microsoft.Win32;
using SharpConfig;

namespace CompilePal
{
    /// <summary>
    /// Interaction logic for LaunchWindow.xaml
    /// </summary>
    public partial class LaunchWindow
    {
        List<GameInfo> gameInfos = new List<GameInfo>();
        public LaunchWindow()
        {
            InitializeComponent();

            //Loading the last used configurations for hammer
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Hammer\General");

            string BinFolder = (string)rk.GetValue("Directory");
            string gameData = System.IO.Path.Combine(BinFolder, "GameConfig.txt");

            gameInfos = GameConfigs.Parse(gameData);

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
            MainWindow c = new MainWindow(gameInfo);
            c.Show();

            Close();
        }
    }
}
