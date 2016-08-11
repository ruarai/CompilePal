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

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static Dispatcher ActiveDispatcher;
        public MainWindow()
        {
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            InitializeComponent();

            ActiveDispatcher = Dispatcher;

            CompilePalLogger.OnWrite += Logger_OnWrite;
            CompilePalLogger.OnError += CompilePalLogger_OnError;

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
            CompilingManager.OnFinish += CompilingManager_OnFinish;
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

                if (CompileOutputTextbox.Document.Blocks.Any())
                {
                    var lastPara = (Paragraph)CompileOutputTextbox.Document.Blocks.LastBlock;
                    lastPara.Inlines.Add(errorLink);
                }
                else
                {
                    var newPara = new Paragraph(errorLink);
                    CompileOutputTextbox.Document.Blocks.Add(newPara);
                }

                CompileOutputTextbox.ScrollToEnd();

            });
        }

        static void errorLink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var link = (Hyperlink)sender;
            int errorCode = int.Parse(link.TargetName);

            ErrorFinder.ShowErrorDialog(errorCode);
        }

        void Logger_OnWrite(string s, Brush b = null)
        {
            Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(s))
                    return;

                //OutputTab.Focus();

                if (b != null)
                {
                    TextRange tr = new TextRange(CompileOutputTextbox.Document.ContentEnd,
                        CompileOutputTextbox.Document.ContentEnd) { Text = s };
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, b);
                }
                else
                {
                    CompileOutputTextbox.AppendText(s);
                }

                CompileOutputTextbox.ScrollToEnd();

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
            CompileProcessesListBox.ItemsSource = ConfigurationManager.CompileProcesses;

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
                CompileOutputTextbox.Document.Blocks.Clear();
                CompileOutputTextbox.AppendText(Environment.NewLine);
            });

        }


        private void CompilingManager_OnFinish()
        {
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
            ParameterAdder c = new ParameterAdder(selectedProcess.ParameterList);
            c.ShowDialog();

            if (c.ChosenItem != null && !selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset].Contains(c.ChosenItem))
                selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset].Add(c.ChosenItem);

            AnalyticsManager.ModifyPreset();

            UpdateParameterTextBox();

        }

        private void RemoveParameterButton_OnClickParameterButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (ConfigItem)ConfigDataGrid.SelectedItem;
            if (selectedItem != null)
                selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset].Remove(selectedItem);

            UpdateParameterTextBox();
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
                ConfigDataGrid.ItemsSource = selectedProcess.PresetDictionary[ConfigurationManager.CurrentPreset];

                UpdateParameterTextBox();
            }
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

    }
}
