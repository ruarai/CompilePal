using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for LaunchWindow.xaml
    /// </summary>
    public partial class LaunchWindow
    {
	    public static LaunchWindow Instance;
        public LaunchWindow()
        {	
            try
            {
                string gameConfigurationFolder = "./GameConfiguration";
                string gameConfigurationsPath = Path.Combine(gameConfigurationFolder, "gameConfigs.json");

                InitializeComponent();

                if (!Directory.Exists(gameConfigurationFolder))
                    Directory.CreateDirectory(gameConfigurationFolder);

                //Loading the last used configurations for hammer
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Hammer\General");

                var configs = new List<GameConfiguration>();

                //try loading json
                if (File.Exists(gameConfigurationsPath))
                {
                    string jsonLoadText = File.ReadAllText(gameConfigurationsPath);
                    configs.AddRange(JsonConvert.DeserializeObject<List<GameConfiguration>>(jsonLoadText));
                }

                //try loading from registry
                if (rk != null)
                {
                    string BinFolder = (string)rk.GetValue("Directory");


                    string gameData = Path.Combine(BinFolder, "GameConfig.txt");
                    try
                    {
	                    configs.AddRange(GameConfigurationParser.Parse(gameData));
                    }
                    catch (Exception e)
                    {
						ExceptionHandler.LogException(e);
                    }
                    
                }

                //finalise config loading
                if (configs.Any())
                {
                    //remove duplicates
                    configs = configs.GroupBy(g => g.Name).Select(grp => grp.First()).ToList();

                    //save
                    string jsonSaveText = JsonConvert.SerializeObject(configs, Formatting.Indented);
                    File.WriteAllText(gameConfigurationsPath, jsonSaveText);
                    GameConfigurationManager.GameConfigurations = configs;

                    if (configs.Count == 1)
                        Launch(configs.First());


                    GameGrid.ItemsSource = configs;
                }
                else//oh noes
                {
                    LaunchButton.IsEnabled = false;
                    WarningLabel.Content = "No Hammer configurations found. Cannot launch.";
                }

                //Handle command line args for game configs
                string[] commandLineArgs = Environment.GetCommandLineArgs();
                for (int i = 0; i < commandLineArgs.Length; i++)
                {
	                var arg = commandLineArgs[i];
                    try
                    {
                        // look for game args
                        if (arg == "--game")
                        {
							// make sure args don't go out of bounds
	                        if (i + 1 > commandLineArgs.Length)
		                        break;

	                        string argGameConfig = commandLineArgs[i + 1].ToLower();

                            foreach (GameConfiguration config in configs)
                            {
	                            string configName = config.Name.ToLower();

                                if (argGameConfig == configName)
	                                Launch(config);
                            }
                        }
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        //Ignore error
                    }
                }
            }
            catch (Exception e) { ExceptionHandler.LogException(e); }

            Instance = this;
        }

        private void Launch(GameConfiguration config)
        {
            GameConfigurationManager.GameConfiguration = config;
			// if main window already exists update title
            if (MainWindow.Instance == null)
            {
				var c = new MainWindow();
				c.Show();
            }
            else
            {
				MainWindow.Instance.Title = $"Compile Pal {UpdateManager.CurrentVersion}X {GameConfigurationManager.GameConfiguration.Name}";
            }

            Instance = null;
            Close();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (GameConfiguration)GameGrid.SelectedItem;

            if (selectedItem != null)
                Launch(selectedItem);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
	        Instance = null;
	        base.OnClosing(e);
        }
    }
}
