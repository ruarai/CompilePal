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
using SharpConfig;

namespace CompilePal
{
    /// <summary>
    /// Interaction logic for Launch_Window.xaml
    /// </summary>
    public partial class Launch_Window
    {
        List<ILaunchProfile> launchProfiles = new List<ILaunchProfile>();

        public Config launcherConfig = new Config("launcherConfig", true, true);
        public Launch_Window()
        {
            InitializeComponent();

            launchProfiles.Add(new CSGO());

            GameProfilesListBox.ItemsSource = launchProfiles;

            if (launcherConfig.Values.ContainsKey("alwayslaunch"))
            {
                var possibleProfile = launchProfiles.FirstOrDefault(i => i.Name == launcherConfig["alwayslaunch"]);

                if (possibleProfile != null)
                {
                    MainWindow c = new MainWindow(possibleProfile.GetConfiguration());
                    c.Show();

                    Close();
                }
            }
        }

        private void Proceed()
        {
            MainWindow c = new MainWindow(((ILaunchProfile)GameProfilesListBox.SelectedItem).GetConfiguration());
            c.Show();

            if (AlwaysLaunchCheckBox.IsChecked.GetValueOrDefault())
                launcherConfig["alwayslaunch"] = ((ILaunchProfile) GameProfilesListBox.SelectedItem).Name;

            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Proceed();
        }

        private void GameProfilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Proceed();
        }

    }
}
