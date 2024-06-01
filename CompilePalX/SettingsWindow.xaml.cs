using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CompilePalX.Configuration;

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public SettingsWindow()
        {
            this.DataContext = ConfigurationManager.Settings.Clone();
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigurationManager.SaveSettings((Settings) this.DataContext);
            Close();
        }

        private readonly Regex numberRegex = new Regex("[^0-9]+");
        private void ErrorCacheDurationDays_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = numberRegex.IsMatch(e.Text);
        }
    }
}
