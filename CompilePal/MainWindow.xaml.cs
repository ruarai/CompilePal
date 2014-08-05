using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using SharpConfig;


namespace CompilePal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public List<CompileProgram> CompilePrograms = new List<CompileProgram>();

        private string currentConfig = "Normal";


        private string GamePath;
        private string MapPath;

        private string oldTitle = "COMPILE PAL";


        private ObservableCollection<string> mapFiles = new ObservableCollection<string>();

        public Config uiConfig = new Config("ui", true, true);


        public MainWindow()
        {
            InitializeComponent();

            mapFilesListBox.ItemsSource = mapFiles;

            if (uiConfig.Values.ContainsKey("copymap"))
                CopyMapCheckBox.IsChecked = uiConfig["copymap"];

            if (!uiConfig.Values.ContainsKey("lowpriority"))
                uiConfig["lowpriority"] = true;

            if (uiConfig.Values.ContainsKey("vmffiles"))
            {
                foreach (var vmf in uiConfig["vmffiles"])
                {
                    if (!string.IsNullOrEmpty((string)vmf))
                        mapFiles.Add((string)vmf);
                }
            }

            //Loading the last used configurations for hammer
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Hammer\General");

            string binFolder = (string)rk.GetValue("Directory");
            string gameData = Path.Combine(binFolder, "GameConfig.txt");

            var lines = File.ReadAllLines(gameData);


            //Lazy parsing
            string VBSPPath = lines[17].Split('"')[3];
            string VVISPath = lines[18].Split('"')[3];
            string VRADPath = lines[19].Split('"')[3];

            CompilePrograms.Add(new CompileProgram(VBSPPath, "vbsp", VBSPDataGrid, VBSPParamsTextBox));
            CompilePrograms.Add(new CompileProgram(VVISPath, "vvis", VVISDataGrid, VVISParamsTextBox));
            CompilePrograms.Add(new CompileProgram(VRADPath, "vrad", VRADDataGrid, VRADParamsTextBox));

            GamePath = lines[6].Split('"')[3];
            MapPath = lines[22].Split('"')[3];

            Title = "Compile Pal: " + lines[4].Replace("\"", "").Trim();

            LoadConfigs();

            LoadConfig(currentConfig);

        }



        #region SaveLoad
        private void LoadConfigs()
        {
            ConfigComboBox.Items.Clear();
            var dirs = Directory.GetDirectories("config");
            foreach (var dir in dirs)
            {
                string configName = dir.Replace("config\\", "");
                ConfigComboBox.Items.Add(configName);
            }

        }

        private void LoadConfig(string configName)
        {
            currentConfig = configName;

            foreach (CompileProgram program in CompilePrograms)
            {
                program.LoadConfig(currentConfig);
            }
        }

        #endregion


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "Valve Map Files (*.vmf)|*.vmf|All files (*.*)|*.*";

            dialog.ShowDialog();

            if (dialog.FileNames.Any())
            {
                foreach (var fileName in dialog.FileNames)
                {
                    mapFiles.Add(fileName);
                }
            }
        }



        private Thread CompileThread;
        private void CompileButton_Click(object sender, RoutedEventArgs e)
        {
            CancelCompileButton.Visibility = Visibility.Visible;

            OutputTab.Focus();

            oldTitle = Title;

            CompileThread = new Thread(Compile);
            CompileThread.Start();
        }

        private void Compile()
        {
            float progress = 0f;
            foreach (var vmf in mapFiles)
            {
                Dispatcher.Invoke(() => AppendLine("Compiling " + vmf));
                foreach (var program in CompilePrograms)
                {
                    Dispatcher.Invoke(() =>Title = string.Format("{0} {1}", program.ToolName, vmf).ToUpper() );

                    if (program.RunTool(this, vmf, GamePath))
                        return;//If true, then the compile was cancelled

                    progress += (1f / CompilePrograms.Count) / mapFiles.Count;

                    Dispatcher.Invoke(() => SetProgress(progress));

                }
            }
            Dispatcher.Invoke(CompileFinish);
        }

        void SetProgress(double progress)
        {

            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            TaskbarItemInfo.ProgressValue = progress;
        }



        void CompileFinish()
        {
            CancelCompileButton.Visibility = Visibility.Hidden;

            Title = oldTitle;

            if (CopyMapCheckBox.IsChecked.GetValueOrDefault())
            {
                foreach (var vmf in mapFiles)
                {
                    string newmap = Path.Combine(MapPath, Path.GetFileNameWithoutExtension(vmf) + ".bsp");
                    string oldmap = vmf.Replace(".vmf", ".bsp");

                    //Make sure we actually need to copy the map
                    if (!String.Equals(newmap, oldmap, StringComparison.CurrentCultureIgnoreCase))
                    {
                        File.Delete(newmap);

                        File.Copy(oldmap, newmap);
                        AppendLine("Map {0} copied to {1}", oldmap, newmap);
                    }
                    else
                        AppendLine("BSP file didn't have to be copied. Skipping.");
                }
            }
        }

        public void AppendLine(string s, params string[] arguments)
        {
            if (string.IsNullOrEmpty(s))
                return;
            s = string.Format(s, arguments);
            s += Environment.NewLine;

            CompileOutputTextbox.Focus();

            CompileOutputTextbox.AppendText(s);
            CompileOutputTextbox.ScrollToEnd();
        }

        public void AppendLineC(string s, Brush brush = null)
        {
            if (string.IsNullOrEmpty(s))
                return;
            s += Environment.NewLine;

            CompileOutputTextbox.Focus();

            if (brush == null) brush = Brushes.Black;
            CompileOutputTextbox.AppendText(s, brush);
            CompileOutputTextbox.ScrollToEnd();
        }


        private void CancelCompileButton_OnClick(object sender, RoutedEventArgs e)
        {
            CancelCompile();
        }

        private void CancelCompile()
        {
            CancelCompileButton.Visibility = Visibility.Hidden;

            foreach (var program in CompilePrograms)
            {
                program.Kill();
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Path.Combine("config", NewConfigNameBox.Text)))
                Directory.CreateDirectory(Path.Combine("config", NewConfigNameBox.Text));

            foreach (CompileProgram program in CompilePrograms)
            {
                program.SaveConfig(NewConfigNameBox.Text);
            }
            currentConfig = NewConfigNameBox.Text;

            LoadConfigs();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (CompileProgram program in CompilePrograms)
            {
                program.SaveConfig(currentConfig);
            }

            LoadConfigs();
        }

        private void ConfigComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty((string)ConfigComboBox.SelectedItem))
                LoadConfig((string)ConfigComboBox.SelectedItem);
        }

        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            foreach (CompileProgram program in CompilePrograms)
            {
                program.CollateParameters();
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            uiConfig["copymap"] = CopyMapCheckBox.IsChecked.GetValueOrDefault();
            uiConfig["vmffiles"] = mapFiles;
        }

        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            if (TaskbarItemInfo.ProgressValue == 1)
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                TaskbarItemInfo.ProgressValue = 0;
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            mapFiles.Remove((string)mapFilesListBox.SelectedItem);
        }
    }
}
