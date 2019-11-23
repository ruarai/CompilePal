using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using CompilePalX.Compiling;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Animation;
using System.Windows.Media.TextFormatting;
using CompilePalX.Compilers;
using CompilePalX.Configuration;

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static Dispatcher ActiveDispatcher;
        private ObservableCollection<CompileProcess> CompileProcessesSubList = new ObservableCollection<CompileProcess>();
	    private bool processModeEnabled;
		public static MainWindow Instance { get; private set; }

		public MainWindow()
        {
	        Instance = this;

			Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            InitializeComponent();

            ActiveDispatcher = Dispatcher;

            CompilePalLogger.OnWrite += Logger_OnWrite;
            CompilePalLogger.OnBacktrack += Logger_OnBacktrack;
            CompilePalLogger.OnErrorLog += CompilePalLogger_OnError;

            UpdateManager.OnUpdateFound += UpdateManager_OnUpdateFound;
            UpdateManager.CheckVersion();

            AnalyticsManager.Launch();
            PersistenceManager.Init();
            ErrorFinder.Init();

            ConfigurationManager.AssembleParameters();

            ProgressManager.TitleChange += ProgressManager_TitleChange;
            ProgressManager.ProgressChange += ProgressManager_ProgressChange;
            ProgressManager.Init(TaskbarItemInfo);


            SetSources();

            CompileProcessesListBox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Ordering", System.ComponentModel.ListSortDirection.Ascending));

            CompileProcessesListBox.SelectedIndex = 0;
            PresetConfigListBox.SelectedIndex = 0;

            UpdateConfigGrid();

            ConfigSelectButton.Visibility = GameConfigurationManager.GameConfigurations.Count > 1
                ? Visibility.Visible
                : Visibility.Collapsed;

            CompilingManager.OnClear += CompilingManager_OnClear;

            CompilingManager.OnStart += CompilingManager_OnStart;
            CompilingManager.OnFinish += CompilingManager_OnFinish;

			RowDragHelper.RowSwitched += RowDragHelperOnRowSwitched;

            HandleArgs();
        }

		public Task<MessageDialogResult> ShowModal(string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null)
		{
			return this.Dispatcher.Invoke(() => this.ShowMessageAsync(title, message, style, settings));
		}

	    private void HandleArgs(bool ignoreWipeArg = false)
        {
            //Handle command line args
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length; i++)
            {
	            var arg = commandLineArgs[i];
                try
                {
                    if (!ignoreWipeArg)
                    {
                        // wipes the map list
                        if (arg == "--wipe")
                        {
                            CompilingManager.MapFiles.Clear();
                            // recursive so that wipe doesn't clear maps added through the command line
                            HandleArgs(true);
                            break;
                        }
                    }

                    // adds map
                    if (arg == "--add")
                    {
                        if (i + 1 > commandLineArgs.Length)
	                        break;

                        var argPath = commandLineArgs[i + 1];

                        if (File.Exists(argPath))
                        {
                            if (argPath.EndsWith(".vmf") || argPath.EndsWith(".vmm") || argPath.EndsWith(".vmx"))
                                CompilingManager.MapFiles.Add(argPath);
                        }
                    }

                }
                catch (ArgumentOutOfRangeException e)
                {
                    //Ignore error
                }
            }
        }

        void CompilePalLogger_OnError(string errorText, Error e)
        {
            Dispatcher.Invoke(() =>
            {
                Hyperlink errorLink = new Hyperlink();

                Run text = new Run(errorText);

                text.Foreground = e.ErrorColor;

                errorLink.Inlines.Add(text);
                errorLink.TargetName = e.ID.ToString();
                errorLink.Click += errorLink_Click;

                var underline = new TextDecoration
                {
                    Location = TextDecorationLocation.Underline,
                    Pen = new Pen(e.ErrorColor, 1),
                    PenThicknessUnit = TextDecorationUnit.FontRecommended
                };

                errorLink.TextDecorations = new TextDecorationCollection(new[] { underline });

                OutputParagraph.Inlines.Add(errorLink);
                CompileOutputTextbox.ScrollToEnd();

            });
        }

        static void errorLink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var link = (Hyperlink)sender;
            int errorCode = int.Parse(link.TargetName);

            ErrorFinder.ShowErrorDialog(errorCode);
        }
        

        Run Logger_OnWrite(string s, Brush b = null)
        {
            return Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(s))
                    return null;

                Run textRun = new Run(s);

                if (b != null)
                    textRun.Foreground = b;

                OutputParagraph.Inlines.Add(textRun);

                CompileOutputTextbox.ScrollToEnd();
                return textRun;
            });
        }

        void Logger_OnBacktrack(List<Run> removals)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var run in removals)
                {
                    run.Text = "";
                }
            });
        }

        async void UpdateManager_OnUpdateFound()
        {
            UpdateHyperLink.Inlines.Add(
	            $"An update is available. Current version is {UpdateManager.CurrentVersion}, latest version is {UpdateManager.LatestVersion}.");
            UpdateLabel.Visibility = Visibility.Visible;
        }


        void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ExceptionHandler.LogException(e.Exception);
        }


        void SetSources()
        {
            CompileProcessesListBox.ItemsSource = CompileProcessesSubList;
            PresetConfigListBox.ItemsSource = ConfigurationManager.KnownPresets;

            MapListBox.ItemsSource = CompilingManager.MapFiles;

			OrderManager.Init();
	        OrderManager.UpdateOrder();

			
			//BindingOperations.EnableCollectionSynchronization(CurrentOrder, lockObj);
		}

        void ProgressManager_ProgressChange(double progress)
        {
            CompileProgressBar.Value = progress;

            if (progress < 0 || progress >= 100)
                CompileStartStopButton.Content = "Compile";
        }

        void ProgressManager_TitleChange(string title)
        {
            Title = title;
        }


        void CompilingManager_OnClear()
        {
            Dispatcher.Invoke(() =>
            {
                OutputParagraph.Inlines.Clear();
            });

        }

        private void CompilingManager_OnStart()
        {
            ConfigDataGrid.IsEnabled = false;
            ProcessDataGrid.IsEnabled = false;
	        OrderGrid.IsEnabled = false;

            AddParameterButton.IsEnabled = false;
            RemoveParameterButton.IsEnabled = false;

            AddProcessesButton.IsEnabled = false;
            RemoveProcessesButton.IsEnabled = false;
            CompileProcessesListBox.IsEnabled = false;

            AddPresetButton.IsEnabled = false;
            RemovePresetButton.IsEnabled = false;
            ClonePresetButton.IsEnabled = false;
            PresetConfigListBox.IsEnabled = false;

            AddMapButton.IsEnabled = false;
            RemoveMapButton.IsEnabled = false;
        }

        private void CompilingManager_OnFinish()
        {
			//If process grid is enabled, disable config grid
            ConfigDataGrid.IsEnabled = !processModeEnabled;
            ProcessDataGrid.IsEnabled = processModeEnabled;
	        OrderGrid.IsEnabled = true;

            AddParameterButton.IsEnabled = true;
            RemoveParameterButton.IsEnabled = true;

            AddProcessesButton.IsEnabled = true;
            RemoveProcessesButton.IsEnabled = true;
            CompileProcessesListBox.IsEnabled = true;

            AddPresetButton.IsEnabled = true;
            RemovePresetButton.IsEnabled = true;
            ClonePresetButton.IsEnabled = true;
            PresetConfigListBox.IsEnabled = true;

            AddMapButton.IsEnabled = true;
            RemoveMapButton.IsEnabled = true;

            string logName = DateTime.Now.ToString("s").Replace(":", "-") + ".txt";
            string textLog = new TextRange(CompileOutputTextbox.Document.ContentStart, CompileOutputTextbox.Document.ContentEnd).Text;

            if (!Directory.Exists("CompileLogs"))
                Directory.CreateDirectory("CompileLogs");

            File.WriteAllText(System.IO.Path.Combine("CompileLogs", logName), textLog);

            CompileStartStopButton.Content = "Compile";

            ProgressManager.SetProgress(1);
        }

        private void OnConfigChanged(object sender, RoutedEventArgs e)
        {
            UpdateParameterTextBox();
        }

        private void AddParameterButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProcess != null)
            {
				//Skip Paramater Adder for Custom Process
	            if (selectedProcess.Name == "CUSTOM")
	            {
					selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset].Add((ConfigItem)selectedProcess.ParameterList[0].Clone());
	            }
	            else
	            {
					ParameterAdder c = new ParameterAdder(selectedProcess.ParameterList);
					c.ShowDialog();

					if (c.ChosenItem != null)
					{
						if (c.ChosenItem.CanBeUsedMoreThanOnce)
						{
							// .clone() removes problems with parameters sometimes becoming linked
							selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset].Add((ConfigItem)c.ChosenItem.Clone());
						} 
						else if (!selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset].Contains(c.ChosenItem))
						{
							selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset].Add(c.ChosenItem);
						}
					}
	            }

                AnalyticsManager.ModifyPreset();

                UpdateParameterTextBox();
            }
        }

        private void RemoveParameterButton_OnClickParameterButton_Click(object sender, RoutedEventArgs e)
        {
	        ConfigItem selectedItem;
	        if (processModeEnabled)
		        selectedItem = (ConfigItem) ProcessDataGrid.SelectedItem;
	        else
				selectedItem = (ConfigItem) ConfigDataGrid.SelectedItem;
            
            if (selectedItem != null)
                selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset].Remove(selectedItem);

            UpdateParameterTextBox();
        }

        private void AddProcessButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessAdder c = new ProcessAdder();
            c.ShowDialog();

            if (c.ProcessDataGrid.SelectedItem != null)
            {
                CompileProcess ChosenProcess = (CompileProcess)c.ProcessDataGrid.SelectedItem;
                ChosenProcess.Metadata.DoRun = true;
                if (!ChosenProcess.PresetDictionary.ContainsKey(ConfigurationManager.CurrentPreset))
                {
                    ChosenProcess.PresetDictionary.Add(ConfigurationManager.CurrentPreset, new ObservableCollection<ConfigItem>());
                }
            }

            AnalyticsManager.ModifyPreset();

            UpdateParameterTextBox();
            UpdateProcessList();

			if (processModeEnabled)
				OrderManager.UpdateOrder();
		}

        private void RemoveProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (CompileProcessesListBox.SelectedItem != null)
            {
                CompileProcess removed = (CompileProcess)CompileProcessesListBox.SelectedItem;
                removed.PresetDictionary.Remove(ConfigurationManager.CurrentPreset);
                ConfigurationManager.RemoveProcess(CompileProcessesListBox.SelectedItem.ToString());
            }
            UpdateProcessList();
            CompileProcessesListBox.SelectedIndex = 0;
		}

        private async void AddPresetButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Preset Name");
            dialog.ShowDialog();

            if (dialog.Result)
            {
                string presetName = dialog.Text;

                ConfigurationManager.NewPreset(presetName);

                AnalyticsManager.NewPreset();

                SetSources();
                CompileProcessesListBox.SelectedIndex = 0;
                PresetConfigListBox.SelectedItem = presetName;
            }
        }
        private async void ClonePresetButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ConfigurationManager.CurrentPreset != null)
            {
                var dialog = new InputDialog("Preset Name");
                dialog.ShowDialog();

                if (dialog.Result)
                {
                    string presetName = dialog.Text;

                    ConfigurationManager.ClonePreset(presetName);

                    AnalyticsManager.NewPreset();

                    SetSources();
                    CompileProcessesListBox.SelectedIndex = 0;
                    PresetConfigListBox.SelectedItem = presetName;
                }
            }
        }

        private void RemovePresetButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (string)PresetConfigListBox.SelectedItem;

            if (selectedItem != null)
                ConfigurationManager.RemovePreset(selectedItem);

            SetSources();
            CompileProcessesListBox.SelectedIndex = 0;
            PresetConfigListBox.SelectedIndex = 0;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConfigurationManager.SavePresets();
            ConfigurationManager.SaveProcesses();

            Environment.Exit(0);//hack because wpf is weird
        }

        private void PresetConfigListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateConfigGrid();
            UpdateProcessList();

			if (processModeEnabled)
				OrderManager.UpdateOrder();
		}
        private void CompileProcessesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateConfigGrid();
        }


        private CompileProcess selectedProcess;

        private void UpdateConfigGrid()
        {
            ConfigurationManager.CurrentPreset = (string)PresetConfigListBox.SelectedItem;

            selectedProcess = (CompileProcess)CompileProcessesListBox.SelectedItem;

            if (selectedProcess != null && ConfigurationManager.CurrentPreset != null && selectedProcess.PresetDictionary.ContainsKey(ConfigurationManager.CurrentPreset))
            {
				//Switch to the process grid for custom program screen
	            if (selectedProcess.Name == "CUSTOM")
	            {
					ProcessDataGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(50))));
					processModeEnabled = true;

					ProcessDataGrid.ItemsSource = selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset];
		            
					ConfigDataGrid.IsEnabled = false;
		            ConfigDataGrid.Visibility = Visibility.Hidden;
					ParametersTextBox.Visibility = Visibility.Hidden;

		            ProcessDataGrid.IsEnabled = true;
		            ProcessDataGrid.Visibility = Visibility.Visible;

		            ProcessTab.IsEnabled = true;
		            ProcessTab.Visibility = Visibility.Visible;

					//Hide parameter buttons if ORDER is the current tab
		            if ((string)(ProcessTab.SelectedItem as TabItem)?.Header == "ORDER")
		            {
						AddParameterButton.Visibility = Visibility.Hidden;
						AddParameterButton.IsEnabled = false;

						RemoveParameterButton.Visibility = Visibility.Hidden;
						RemoveParameterButton.IsEnabled = false;
					}
				}
	            else
	            {
					ConfigDataGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(50))));
					processModeEnabled = false;

					ConfigDataGrid.IsEnabled = true;
					ConfigDataGrid.Visibility = Visibility.Visible;
					ParametersTextBox.Visibility = Visibility.Visible;

					ProcessDataGrid.IsEnabled = false;
					ProcessDataGrid.Visibility = Visibility.Hidden;

					ProcessTab.IsEnabled = false;
					ProcessTab.Visibility = Visibility.Hidden;

					ConfigDataGrid.ItemsSource = selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset];

					//Make buttons visible if they were disabled
		            if (!AddParameterButton.IsEnabled)
		            {
						AddParameterButton.Visibility = Visibility.Visible;
						AddParameterButton.IsEnabled = true;

						RemoveParameterButton.Visibility = Visibility.Visible;
						RemoveParameterButton.IsEnabled = true;
					}

					UpdateParameterTextBox();
	            }


            }
        }

        private void UpdateProcessList()
        {
            CompileProcessesListBox.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(50))));


            int currentIndex = CompileProcessesListBox.SelectedIndex;

            CompileProcessesSubList.Clear();

            CompileProcessesListBox.Items.SortDescriptions.Add(new SortDescription("Ordering", ListSortDirection.Ascending));

            foreach (CompileProcess p in ConfigurationManager.CompileProcesses)
            {
                if (ConfigurationManager.CurrentPreset != null)
                    if (p.PresetDictionary.ContainsKey(ConfigurationManager.CurrentPreset))
                        CompileProcessesSubList.Add(p);
            }

            if (currentIndex < CompileProcessesListBox.Items.Count && currentIndex >= 0)
                CompileProcessesListBox.SelectedIndex = currentIndex;
        }

        void UpdateParameterTextBox()
        {
            if (selectedProcess != null)
                ParametersTextBox.Text = selectedProcess.GetParameterString();
        }

        private void MetroWindow_Activated(object sender, EventArgs e)
        {
            ProgressManager.PingProgress();
        }

        private void AddMapButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();

            if (GameConfigurationManager.GameConfiguration.SDKMapFolder != null)
                dialog.InitialDirectory = GameConfigurationManager.GameConfiguration.SDKMapFolder;

            dialog.Multiselect = true;
            dialog.Filter = "Map files (*.vmf;*.vmm)|*.vmf;*.vmm";

            try
            {
                dialog.ShowDialog();
            }
            catch
            {
                CompilePalLogger.LogDebug($"AddMapButton dialog failed to open, falling back to {Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}");
				// if dialog fails to open it's possible its initial directory is in a non existant folder or something
	            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
	            dialog.ShowDialog();
            }

            foreach (var file in dialog.FileNames)
            {
                CompilingManager.MapFiles.Add(file);
            }
        }

        private void RemoveMapButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedMap = (string)MapListBox.SelectedItem;

            if (selectedMap != null)
                CompilingManager.MapFiles.Remove(selectedMap);
        }


        private void CompileStartStopButton_OnClick(object sender, RoutedEventArgs e)
        {
            CompilingManager.ToggleCompileState();

            CompileStartStopButton.Content = (string)CompileStartStopButton.Content == "Compile" ? "Cancel" : "Compile";

            OutputTab.Focus();
        }

        private void UpdateLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("http://www.github.com/ruarai/CompilePal/releases/latest");
        }

	    private void ReadOutput_OnChecked(object sender, RoutedEventArgs e)
	    {
		    var selectedItem = (ConfigItem) ProcessDataGrid.SelectedItem;

			//Set readOuput to opposite of it's current value
		    selectedItem.ReadOutput = !selectedItem.ReadOutput;

			//UpdateParameterTextBox();
	    }

	    private void ProcessTab_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	    {
			if (e.Source is TabControl)
				OrderManager.UpdateOrder();

			if (OrderTab.IsSelected)
		    {
				AddParameterButton.Visibility = Visibility.Hidden;
				AddParameterButton.IsEnabled = false;

				RemoveParameterButton.Visibility = Visibility.Hidden;
				RemoveParameterButton.IsEnabled = false;
			}
		    else
		    {
				AddParameterButton.Visibility = Visibility.Visible;
				AddParameterButton.IsEnabled = true;

				RemoveParameterButton.Visibility = Visibility.Visible;
				RemoveParameterButton.IsEnabled = true;
			}
		}

	    private void DoRun_OnClick(object sender, RoutedEventArgs e)
	    {
			if (processModeEnabled)
				OrderManager.UpdateOrder();
		}

	    private void OrderGrid_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
	    {
			if (processModeEnabled)
				OrderManager.UpdateOrder();
		}

	    private void DataGridCell_OnEnter(object sender, MouseEventArgs e)
	    {
			//Only show drag cursor if row is draggable
		    if ((sender as DataGridRow)?.Item is CompileProcess process && process.IsDraggable)
			    Cursor = Cursors.SizeAll;
	    }

	    private void DataGridCell_OnExit(object sender, MouseEventArgs e)
	    {
		    if ((sender as DataGridRow)?.Item is CompileProcess process && process.IsDraggable)
			    Cursor = Cursors.Arrow;
	    }

	    public void UpdateOrderGridSource<T>(ObservableCollection<T> newSrc)
	    {
			//Use dispatcher so this can be called from seperate thread
			this.Dispatcher.Invoke(() =>
			{
				//TODO order grid doesnt seem to want to update, so have to do it manually by resetting the source
				//Update ordergrid by resetting collection
				OrderGrid.ItemsSource = newSrc;
			});
		}

		private void RowDragHelperOnRowSwitched(object sender, RowSwitchEventArgs e)
		{
			var primaryItem = OrderGrid.Items[e.PrimaryRowIndex] as CustomProgram;
			var displacedItem = OrderGrid.Items[e.DisplacedRowIndex] as CustomProgram;

			SetOrder(primaryItem, e.PrimaryRowIndex);
			SetOrder(displacedItem, e.DisplacedRowIndex);
		}

	    public void SetOrder<T>(T target, int newOrder)
	    {
			//Generic T is workaround for CustomProgram being
		    //less accessible than this method.
		    var program = target as CustomProgram;
		    if (program == null)
			    return;

			var programConfig = GetConfigFromCustomProgram(program);

			if (programConfig == null)
				return;

			program.CustomOrder = newOrder;
			programConfig.Warning = newOrder.ToString();
		}


		//Search through ProcDataGrid to find corresponding ConfigItem
		private ConfigItem GetConfigFromCustomProgram(CustomProgram program)
	    {
			foreach (var procSourceItem in ProcessDataGrid.ItemsSource)
			{
				if (program.Equals(procSourceItem))
				{
					return procSourceItem as ConfigItem;
				}
			}

			//Return null on failure
		    return null;
	    }

		private void UpdateHyperLink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}

		private void Settings_OnClick(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void ConfigBack_OnClick(object sender, RoutedEventArgs e)
		{
			if (LaunchWindow.Instance == null)
				new LaunchWindow().Show();
			else
				LaunchWindow.Instance.Focus();
		}
    }

	public static class ObservableCollectionExtension
	{
		public static ObservableCollection<T> AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> range)
		{
			foreach (var element in range)
				collection.Add(element);

			return collection;
		}

		public static ObservableCollection<T> RemoveRange<T>(this ObservableCollection<T> collection, IEnumerable<T> range)
		{
			foreach (var element in range)
				collection.Remove(element);

			return collection;
		}
	}
}
