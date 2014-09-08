using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;

namespace CompilePal
{
    /// <summary>
    /// Interaction logic for LaunchWindow.xaml
    /// </summary>
    public partial class LaunchWindow
    {
        public LaunchWindow()
        {
            InitializeComponent();

            //Loading the last used configurations for hammer
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Hammer\General");

            string BinFolder = (string)rk.GetValue("Directory");
            string gameData = System.IO.Path.Combine(BinFolder, "GameConfig.txt");

            List<GameInfo> gameInfos = GameConfigs.Parse(gameData);

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
