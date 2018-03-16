using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CompilePalX.Compiling;
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for ThemeWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //Find themes from theme folder
            string[] files = Directory.GetFiles(CompilePalPath.Directory + "Themes");
            List<string> themes = new List<string>();
            
            //All themes should end in xaml
            foreach (string file in files)
                if (file.EndsWith(".xaml"))
                {
                    try
                    {
                        //Read and extract data from theme resource dicts
                        using (FileStream fs = new FileStream(file, FileMode.Open))
                        {
                            if (XamlReader.Load(fs) is ResourceDictionary content)
                            {
                                //Get name
                                TextBox nameBox = content["Name"] as TextBox;
                                TextBox themeBox = content["Theme"] as TextBox;

                                Theme theme;
                                if (themeBox.Text.ToLower() == "dark")
                                    theme = Theme.Dark;
                                else
                                    theme = Theme.Light;

                                ThemeItem item = new ThemeItem()
                                {
                                    Name = nameBox.Text,
                                    ThemeType = theme,
                                    ThemeStr = Enum.GetName(typeof(Theme), theme)
                                };
                                SelectControl.Items.Add(item);
                                themes.Add(file);
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        CompilePalLogger.LogLine("Error: " + ex.Message);
                    }


                }
                    
        }
    }

    public class ThemeItem
    {
        public string Name { get; set; }
        public Theme ThemeType { get; set; }
        public string ThemeStr { get; set; }
        public List<Accent> Accents { get; set; }
    }
}
