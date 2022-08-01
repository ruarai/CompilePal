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
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for LaunchWindow.xaml
    /// </summary>
    public partial class LaunchWindow
    {
	    public static LaunchWindow? Instance;
        public LaunchWindow()
        {	
            try
            {
                InitializeComponent();

                GameConfigurationManager.LoadGameConfigurations();

                if (GameConfigurationManager.GameConfigurations.Any())
                {
                    if (GameConfigurationManager.GameConfigurations.Count == 1)
                        Launch(GameConfigurationManager.GameConfigurations.First());

                    GameGrid.ItemsSource = GameConfigurationManager.GameConfigurations;
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

                            foreach (GameConfiguration config in GameConfigurationManager.GameConfigurations)
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
            var selectedItem = (GameConfiguration?)GameGrid.SelectedItem;

            if (selectedItem != null)
                Launch(selectedItem);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
	        Instance = null;
            GameConfigurationWindow.Instance?.Close();
	        base.OnClosing(e);
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            GameConfigurationWindow.Instance.Open();
        }

        public void RefreshGameConfigurationList()
        {
            this.GameGrid.Items.Refresh();
        }

        private void EditButton_OnClick(object sender, RoutedEventArgs e)
        {
            var configuration = (GameConfiguration)((Button)sender).DataContext;
            int configIndex = GameConfigurationManager.GameConfigurations.IndexOf(configuration);
            GameConfigurationWindow.Instance.Open(configuration.Clone() as GameConfiguration, configIndex);
        }

        private async void DeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            var configuration = (GameConfiguration)((Button)sender).DataContext;

            var dialogSettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Delete",
                NegativeButtonText = "Cancel",
                AnimateHide = false,
                AnimateShow = false,
                DefaultButtonFocus = MessageDialogResult.Affirmative,
            };

            var result = await this.ShowMessageAsync($"Delete Configuration", $"Are you sure you want to delete {configuration.Name}?",
                MessageDialogStyle.AffirmativeAndNegative, dialogSettings);

            if (result != MessageDialogResult.Affirmative)
                return;

            GameConfigurationManager.GameConfigurations.Remove(configuration);
            GameConfigurationManager.SaveGameConfigurations();
            RefreshGameConfigurationList();
        }
    }
}
