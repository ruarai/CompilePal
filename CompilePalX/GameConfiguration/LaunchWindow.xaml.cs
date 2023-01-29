using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls.Dialogs;

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for LaunchWindow.xaml
    /// </summary>
    public partial class LaunchWindow
    {

        private bool GameConfigsEmpty => !GameConfigurationManager.GameConfigurations.Any();

        public static LaunchWindow? Instance { get; private set; }

        public LaunchWindow()
        {
            Instance = this;
            try
            {
                InitializeComponent();

                GameConfigurationManager.LoadGameConfigurations();

                // automatically launch if there is only 1 config and main window is not open  
                if (GameConfigurationManager.GameConfigurations.Count == 1 && MainWindow.Instance == null)
                    Launch(GameConfigurationManager.GameConfigurations.First());

                GameGrid.ItemsSource = GameConfigurationManager.GameConfigurations;

                RefreshGameConfigurationList();

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
        }

        private void Launch(GameConfiguration config)
        {
            GameConfigurationManager.GameConfiguration = config;
            Instance = null;

			// if main window already exists update title
            if (MainWindow.Instance == null)
            {
				var c = new MainWindow();
				c.Show();
            }
            else
            {
				MainWindow.Instance.Title = $"Compile Pal {UpdateManager.CurrentVersion}X {GameConfigurationManager.GameConfiguration.Name}";
                // refresh item sources to reevaluate process/parameter compatibility
                MainWindow.Instance.RefreshSources();
            }
            Close();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // walk up dependency tree to make sure click source was not edit/delete button
            DependencyObject? dep = e.OriginalSource as DependencyObject;
            while ((dep != null) && !(dep is Button) && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            // ignore if double click came from button
            if (dep is Button b && b.Name != "LaunchButton")
                return;

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

            // recalculate state
            if (GameConfigsEmpty)
            {
                // Empty State
                FilledState.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
            }
            else
            {
                // Filled State
                FilledState.Visibility = Visibility.Visible;
                EmptyState.Visibility = Visibility.Collapsed;
            }
        }

        private void EditButton_OnClick(object sender, RoutedEventArgs e)
        {
            var configuration = (GameConfiguration)((MenuItem)sender).DataContext;
            int configIndex = GameConfigurationManager.GameConfigurations.IndexOf(configuration);
            GameConfigurationWindow.Instance.Open(configuration.Clone() as GameConfiguration, configIndex);
        }

        private async void DeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            var configuration = (GameConfiguration)((MenuItem)sender).DataContext;

            var dialogSettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Delete",
                NegativeButtonText = "Cancel",
                AnimateHide = false,
                AnimateShow = false,
                DefaultButtonFocus = MessageDialogResult.Affirmative,
            };

            var result = await this.ShowMessageAsync($"Delete Game", $"Are you sure you want to delete {configuration.Name}?",
                MessageDialogStyle.AffirmativeAndNegative, dialogSettings);

            if (result != MessageDialogResult.Affirmative)
                return;

            GameConfigurationManager.GameConfigurations.Remove(configuration);
            GameConfigurationManager.SaveGameConfigurations();
            RefreshGameConfigurationList();

            // deselect deleted game config
            if (GameConfigurationManager.GameConfiguration != null && GameConfigurationManager.GameConfiguration.Equals(configuration))
            {
                GameConfigurationManager.GameConfiguration = null;
                // if game config is already opened in the main window, close it
                if (MainWindow.Instance != null)
                    MainWindow.Instance.Close();
            }
        }

        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            GameConfigurationManager.LoadGameConfigurations();
            GameGrid.ItemsSource = GameConfigurationManager.GameConfigurations;
            RefreshGameConfigurationList();
        }
        private void GameKebabButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.ContextMenu == null)
                return;

            // set placement of context menu to button instead of default behaviour of mouse position
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
            e.Handled = true;
        }
        private void GameKebabButton_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // block right click context menus
            e.Handled = true;
        }
    }
}
