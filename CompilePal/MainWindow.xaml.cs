using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
        public ObservableCollection<Parameter> VRADParameters = new ObservableCollection<Parameter>();
        public ObservableCollection<Parameter> VBSPParameters = new ObservableCollection<Parameter>();
        public ObservableCollection<Parameter> VVISParameters = new ObservableCollection<Parameter>();

        private string OutputVRADParameters = "";
        private string OutputVBSPParameters = "";
        private string OutputVVISParameters = "";

        private string currentConfig = "Normal";

        private string VBSPPath;
        private string VVISPath;
        private string VRADPath;

        private string GamePath;
        private string MapPath;



        private string VMFFile;

        public Config uiConfig = new Config("ui", true, true);


        public MainWindow()
        {
            InitializeComponent();

            if (uiConfig.Values.ContainsKey("vmffile"))
                MapFileText.Text = uiConfig["vmffile"];

            if (uiConfig.Values.ContainsKey("copymap"))
                CopyMapCheckBox.IsChecked = uiConfig["copymap"];

            if (!uiConfig.Values.ContainsKey("lowpriority"))
                uiConfig["lowpriority"] = true;

            //Loading the last used configurations for hammer
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Hammer\General");

            string binFolder = (string)rk.GetValue("Directory");
            string gameData = Path.Combine(binFolder, "GameConfig.txt");

            var lines = File.ReadAllLines(gameData);


            //Lazy parsing
            VBSPPath = lines[17].Split('"')[3];
            VVISPath = lines[18].Split('"')[3];
            VRADPath = lines[19].Split('"')[3];

            GamePath = lines[6].Split('"')[3];
            MapPath = lines[22].Split('"')[3];

            Title="Compile Pal: " + lines[4].Replace("\"", "").Trim();

            LoadConfigs();

            VRADDataGrid.DataContext = VRADParameters;
            VBSPDataGrid.DataContext = VBSPParameters;
            VVISDataGrid.DataContext = VVISParameters;

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

            LoadVRADConfig();
            LoadVBSPConfig();
            LoadVVISConfig();
        }

        void SaveVRADConfig(string configName)
        {
            if (File.Exists(Path.Combine("config", configName, "VRAD.csv")))
                File.Delete(Path.Combine("config", configName, "VRAD.csv"));

            var lines = new List<string>();
            foreach (var para in VRADParameters)
            {
                lines.Add(string.Format("{0}^{1}^{2}^{3}^{4}", para.Name, para.Command, para.Option, para.Description, para.Enabled));
            }

            File.WriteAllLines(Path.Combine("config", configName, "VRAD.csv"), lines);
        }

        void SaveVVISConfig(string configName)
        {
            if (File.Exists(Path.Combine("config", configName, "VVIS.csv")))
                File.Delete(Path.Combine("config", configName, "VVIS.csv"));

            var lines = new List<string>();
            foreach (var para in VVISParameters)
            {
                lines.Add(string.Format("{0}^{1}^{2}^{3}^{4}", para.Name, para.Command, para.Option, para.Description, para.Enabled));
            }

            File.WriteAllLines(Path.Combine("config", configName, "VVIS.csv"), lines);
        }

        void SaveVBSPConfig(string configName)
        {
            if (File.Exists(Path.Combine("config", configName, "VBSP.csv")))
                File.Delete(Path.Combine("config", configName, "VBSP.csv"));

            var lines = new List<string>();
            foreach (var para in VBSPParameters)
            {
                lines.Add(string.Format("{0}^{1}^{2}^{3}^{4}", para.Name, para.Command, para.Option, para.Description, para.Enabled));
            }

            File.WriteAllLines(Path.Combine("config", configName, "VBSP.csv"), lines);
        }
        #endregion

        #region Config
        private void LoadVRADConfig()
        {
            VRADParameters.Clear();
            var lines = File.ReadAllLines(Path.Combine("config", currentConfig, "vrad.csv"));
            foreach (var line in lines)
            {
                string[] split = line.Split('^');

                var param = new Parameter()
                            {
                                Name = split[0],
                                Command = split[1],
                                Option = split[2],
                                Description = split[3],
                                Enabled = bool.Parse(split[4])
                            };

                VRADParameters.Add(param);

            }

            CollateVRADParameters();

        }
        private void LoadVBSPConfig()
        {
            VBSPParameters.Clear();
            var lines = File.ReadAllLines(System.IO.Path.Combine("config", currentConfig, "VBSP.csv"));
            foreach (var line in lines)
            {
                string[] split = line.Split('^');

                var param = new Parameter()
                {
                    Name = split[0],
                    Command = split[1],
                    Option = split[2],
                    Description = split[3],
                    Enabled = bool.Parse(split[4]),
                };

                VBSPParameters.Add(param);

            }

            CollateVBSPParameters();

        }
        private void LoadVVISConfig()
        {
            VVISParameters.Clear();
            var lines = File.ReadAllLines(Path.Combine("config", currentConfig, "VVIS.csv"));
            foreach (var line in lines)
            {
                string[] split = line.Split('^');

                var param = new Parameter()
                {
                    Name = split[0],
                    Command = split[1],
                    Option = split[2],
                    Description = split[3],
                    Enabled = bool.Parse(split[4]),
                };

                VVISParameters.Add(param);

            }

            CollateVVISParameters();

        }
        #endregion
        #region Parameters
        void CollateVRADParameters()
        {
            OutputVRADParameters = string.Empty;
            foreach (var parameter in VRADParameters)
            {
                if (parameter.Enabled)
                {
                    OutputVRADParameters += parameter.Command;
                    if (!string.IsNullOrEmpty(parameter.Option))
                        OutputVRADParameters += " " + parameter.Option;
                }
            }


            OutputVRADParameters += " -game $game";
            OutputVRADParameters += " $map";
            VRADParamsTextBox.Text = OutputVRADParameters;
        }

        void CollateVBSPParameters()
        {
            OutputVBSPParameters = string.Empty;
            foreach (var parameter in VBSPParameters)
            {
                if (parameter.Enabled)
                {
                    OutputVBSPParameters += parameter.Command;
                    if (!string.IsNullOrEmpty(parameter.Option))
                        OutputVBSPParameters += " " + parameter.Option;
                }
            }


            OutputVBSPParameters += " -game $game";
            OutputVBSPParameters += " $map";
            VBSPParamsTextBox.Text = OutputVBSPParameters;
        }


        void CollateVVISParameters()
        {
            OutputVVISParameters = string.Empty;
            foreach (var parameter in VVISParameters)
            {
                if (parameter.Enabled)
                {
                    OutputVVISParameters += parameter.Command;
                    if (!string.IsNullOrEmpty(parameter.Option))
                        OutputVVISParameters += " " + parameter.Option;
                }
            }


            OutputVVISParameters += " -game $game";
            OutputVVISParameters += " $map";
            VVISParamsTextBox.Text = OutputVVISParameters;
        }
        private string FinaliseParameters(string parameters)
        {
            VMFFile = MapFileText.Text;
            parameters = parameters.Replace("$game", "\"" + GamePath + "\"");
            parameters = parameters.Replace("$map", "\"" + VMFFile + "\"");

            return parameters;
        }
        #endregion


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Valve Map Files (*.vmf)|*.vmf|All files (*.*)|*.*";

            dialog.ShowDialog();

            MapFileText.Text = dialog.FileName;
        }


        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CollateVRADParameters();
            CollateVVISParameters();
            CollateVBSPParameters();
        }

        private Thread CompileThread;
        private void CompileButton_Click(object sender, RoutedEventArgs e)
        {
            CancelCompileButton.Visibility = Visibility.Visible;

            OutputTab.Focus();

            string bspparams = FinaliseParameters(OutputVBSPParameters);
            string visparams = FinaliseParameters(OutputVVISParameters);
            string radparams = FinaliseParameters(OutputVRADParameters);

            CompileThread = new Thread(() => Compile(bspparams, visparams, radparams));
            CompileThread.Start();
        }

        private Process VBSPProcess;
        private Process VVISProcess;
        private Process VRADProcess;

        private void Compile(string bspparams, string visparams, string radparams)
        {
            Dispatcher.Invoke(() => AppendLine("--VBSP Starting."));

            VBSPProcess = new Process();
            VBSPProcess.StartInfo.RedirectStandardOutput = true;
            VBSPProcess.StartInfo.RedirectStandardInput = true;
            VBSPProcess.StartInfo.RedirectStandardError = true;
            VBSPProcess.StartInfo.UseShellExecute = false;
            VBSPProcess.StartInfo.CreateNoWindow = true;

            VBSPProcess.OutputDataReceived += OutputDataReceived;

            VBSPProcess.StartInfo.FileName = VBSPPath;
            VBSPProcess.StartInfo.Arguments = bspparams;
            VBSPProcess.Start();
            if (uiConfig.Values.ContainsKey("lowpriority") && uiConfig["lowpriority"])
                VBSPProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

            VBSPProcess.BeginOutputReadLine();

            VBSPProcess.WaitForExit();

            Dispatcher.Invoke(() => AppendLine("--VVIS Starting."));

            VVISProcess = new Process();
            VVISProcess.StartInfo.RedirectStandardOutput = true;
            VVISProcess.StartInfo.RedirectStandardInput = true;
            VVISProcess.StartInfo.RedirectStandardError = true;
            VVISProcess.StartInfo.UseShellExecute = false;
            VVISProcess.StartInfo.CreateNoWindow = true;

            VVISProcess.OutputDataReceived += OutputDataReceived;

            VVISProcess.StartInfo.FileName = VVISPath;
            VVISProcess.StartInfo.Arguments = visparams;
            VVISProcess.Start();
            if (uiConfig.Values.ContainsKey("lowpriority") && uiConfig["lowpriority"])
                VVISProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

            VVISProcess.BeginOutputReadLine();

            VVISProcess.WaitForExit();

            Dispatcher.Invoke(() => AppendLine("--VRAD Starting."));

            VRADProcess = new Process();
            VRADProcess.StartInfo.RedirectStandardOutput = true;
            VRADProcess.StartInfo.RedirectStandardInput = true;
            VRADProcess.StartInfo.RedirectStandardError = true;
            VRADProcess.StartInfo.UseShellExecute = false;
            VRADProcess.StartInfo.CreateNoWindow = true;

            VRADProcess.OutputDataReceived += OutputDataReceived;

            VRADProcess.StartInfo.FileName = VRADPath;
            VRADProcess.StartInfo.Arguments = radparams;
            VRADProcess.Start();
            if (uiConfig.Values.ContainsKey("lowpriority") && uiConfig["lowpriority"])
                VRADProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

            VRADProcess.BeginOutputReadLine();

            VRADProcess.WaitForExit();

            Dispatcher.Invoke(() => AppendLine("--Compile Ending."));

            Dispatcher.Invoke(CompileFinish);
        }

        void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                CompileOutputTextbox.Dispatcher.Invoke(() => AppendLine(e.Data));
        }

        void CompileFinish()
        {
            CancelCompileButton.Visibility = Visibility.Hidden;

            if (CopyMapCheckBox.IsChecked.GetValueOrDefault())
            {
                string newmap = Path.Combine(MapPath, Path.GetFileNameWithoutExtension(VMFFile) + ".bsp");
                string oldmap = VMFFile.Replace(".vmf", ".bsp");

                //Make sure we actually need to copy the map
                if (!String.Equals(newmap, oldmap, StringComparison.CurrentCultureIgnoreCase))
                {
                    File.Delete(newmap);

                    File.Copy(oldmap, newmap);
                    AppendLine("Map {0} copied to {1}",oldmap, newmap);
                }
                else
                    AppendLine("BSP file didn't have to be copied. Skipping.");
            }
        }

        void AppendLine(string s, params string[] arguments)
        {
            s = string.Format(s, arguments);

            CompileOutputTextbox.Focus();
            CompileOutputTextbox.Text += s + Environment.NewLine;
            CompileOutputTextbox.CaretIndex = CompileOutputTextbox.Text.Length;
            CompileOutputTextbox.ScrollToEnd();


            if (s.Contains("--VVIS Starting."))
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                TaskbarItemInfo.ProgressValue = 0.33;
            }

            if (s.Contains("--VRAD Starting."))
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                TaskbarItemInfo.ProgressValue = 0.66;
            }

            if (s.StartsWith("--Compile Ending."))
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                TaskbarItemInfo.ProgressValue = 1;
            }
        }


        private void CancelCompileButton_OnClick(object sender, RoutedEventArgs e)
        {
            CancelCompile();
        }

        private void CancelCompile()
        {
            CancelCompileButton.Visibility = Visibility.Hidden;
            try
            {
                VBSPProcess.Kill();
                AppendLine("Killed VBSP");
            }
            catch { AppendLine("Could not kill VBSP."); }
            try
            {
                VVISProcess.Kill();
                AppendLine("Killed VVIS");
            }
            catch { AppendLine("Could not kill VVIS."); }
            try
            {
                VRADProcess.Kill();
                AppendLine("Killed VRAD");
            }
            catch { AppendLine("Could not kill VRAD."); }

        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Path.Combine("config", NewConfigNameBox.Text)))
                Directory.CreateDirectory(Path.Combine("config", NewConfigNameBox.Text));
            SaveVBSPConfig(NewConfigNameBox.Text);
            SaveVRADConfig(NewConfigNameBox.Text);
            SaveVVISConfig(NewConfigNameBox.Text);

            LoadConfigs();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveVBSPConfig(currentConfig);
            SaveVRADConfig(currentConfig);
            SaveVVISConfig(currentConfig);

            LoadConfigs();
        }

        private void ConfigComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty((string)ConfigComboBox.SelectedItem))
                LoadConfig((string)ConfigComboBox.SelectedItem);
        }

        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CollateVRADParameters();
            CollateVVISParameters();
            CollateVBSPParameters();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            uiConfig["copymap"] = CopyMapCheckBox.IsChecked.GetValueOrDefault();
            uiConfig["vmffile"] = MapFileText.Text;
        }

        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            if (TaskbarItemInfo.ProgressValue == 1)
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                TaskbarItemInfo.ProgressValue = 0;
            }
        }
    }
}
