using System;
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

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static Dispatcher ActiveDispatcher;
        private ObservableCollection<CompileProcess> CompileProcessesSubList = new ObservableCollection<CompileProcess>();

        public MainWindow()
        {
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

            CompileProcessesListBox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Order", System.ComponentModel.ListSortDirection.Ascending));

            CompileProcessesListBox.SelectedIndex = 0;
            PresetConfigListBox.SelectedIndex = 0;

            UpdateConfigGrid();

            CompilingManager.OnClear += CompilingManager_OnClear;

            CompilingManager.OnStart += CompilingManager_OnStart;
            CompilingManager.OnFinish += CompilingManager_OnFinish;

            HandleArgs();
        }

        private void HandleArgs(bool ignoreWipeArg = false)
        {
            //Handle command line args
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            foreach (string arg in commandLineArgs)
            {
                try
                {
                    if (!ignoreWipeArg)
                    {
                        //Wipes the map list
                        if (arg.Substring(0, 5).ToLower() == "-wipe")
                        {
                            CompilingManager.MapFiles.Clear();
                            //Recursive so if the wipe arg comes after a arg path, it will readd it
                            HandleArgs(true);
                            break;
                        }
                    }
                    else
                    {
                        //If arg type is a path, continue
                        if (arg.Substring(0, 6).ToLower() == "-path:")
                        {
                            //Remove arg type
                            string argPath = arg.Remove(0, 6);

                            if (File.Exists(argPath))
                            {
                                if (argPath.EndsWith(".vmf") || argPath.EndsWith(".vmm") || argPath.EndsWith(".vmx"))
                                    CompilingManager.MapFiles.Add(argPath);
                            }
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
            UpdateHyperLink.Inlines.Add(string.Format("An update is available. Current version is {0}, latest version is {1}.", UpdateManager.CurrentVersion, UpdateManager.LatestVersion));
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
            ConfigDataGrid.IsEnabled = true;

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
                ParameterAdder c = new ParameterAdder(selectedProcess.ParameterList);
                c.ShowDialog();

                if (c.ChosenItem != null)
                {
                    if (c.ChosenItem.CanBeUsedMoreThanOnce)
                    {
                        selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset].Add(c.ChosenItem);
                    } 
                    else if (!selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset].Contains(c.ChosenItem))
                    {
                        selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset].Add(c.ChosenItem);
                    }
                }
                    

                AnalyticsManager.ModifyPreset();

                UpdateParameterTextBox();
            }
        }

        private void RemoveParameterButton_OnClickParameterButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (ConfigItem)ConfigDataGrid.SelectedItem;
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
        }
        private void CompileProcessesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateConfigGrid();
        }


        private CompileProcess selectedProcess;

        private void UpdateConfigGrid()
        {
            ConfigDataGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(50))));

            ConfigurationManager.CurrentPreset = (string)PresetConfigListBox.SelectedItem;

            selectedProcess = (CompileProcess)CompileProcessesListBox.SelectedItem;
            if (selectedProcess != null && ConfigurationManager.CurrentPreset != null && selectedProcess.PresetDictionary.ContainsKey(ConfigurationManager.CurrentPreset))
            {
                ConfigDataGrid.ItemsSource = selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset];

                UpdateParameterTextBox();
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

            dialog.ShowDialog();

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

        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            settings.Show();
        }
    }
}
