using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using GlobalHotKey;
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
        private GameProgram gameProgram;
        private PostCompileActions postCompile;

        public static string CurrentConfig = "Normal";

        public GameInfo gameInfo;

        public static string BinFolder;


        private string oldTitle = "COMPILE PAL";


        private ObservableCollection<string> mapFiles = new ObservableCollection<string>();

        public Config uiConfig = new Config("ui", true, true);

        private HotKeyManager hotkeyManager;


        public MainWindow(GameInfo gameinfo)
        {
            gameInfo = gameinfo;

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            InitializeComponent();

            mapFilesListBox.ItemsSource = mapFiles;

            if (!Directory.Exists("dumps"))
                Directory.CreateDirectory("dumps");

            try
            {
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
            }
            catch (Exception e)
            {
                ThrowException("An error occured whilst loading the UI configuration.", e);
            }

            Title = "Compile Pal: " + gameInfo.Name;
            CompilePrograms.Add(new CompileProgram(gameInfo.VBSP, "vbsp", VBSPDataGrid, VBSPParamsTextBox, VBSPRunCheckBox) {ForcedParameters = " -game $game $map"});
            CompilePrograms.Add(new CompileProgram(gameInfo.VVIS, "vvis", VVISDataGrid, VVISParamsTextBox, VVISRunCheckBox) { ForcedParameters = " -game $game $map" });
            CompilePrograms.Add(new CompileProgram(gameInfo.VRAD, "vrad", VRADDataGrid, VRADParamsTextBox, VRADRunCheckBox) { ForcedParameters = " -game $game $map" });
            CompilePrograms.Add(new CompileProgram("BSPAutoPack.exe", "pack", PackDataGrid, PackParamsTextBox, PackRunCheckBox) { RunningDirectory = "", ForcedParameters = " -game $game" });

            gameProgram = new GameProgram(gameInfo.GameEXE, "game", GameDataGrid, GameParamsTextBox, GameDataGrid, GameParamsTextBox, GameRunCheckBox);

            postCompile = new PostCompileActions();
            postCompile.fileListBox = RunFilesListBox;
            postCompile.fileListBox.ItemsSource = postCompile.fileList;

            postCompile.runFileCheckBox = RunFileCheckBox;
            postCompile.shutdownCheckBox = ShutdownCheckBox;
            postCompile.archiveMapCheckBox = ArchiveMapCheckBox;


            BinFolder = gameInfo.BinFolder;

            LoadConfigs();

            LoadConfig(CurrentConfig);

            postCompile.fileListBox.ItemsSource = postCompile.fileList;

            VersionChecker.CheckVersion();
            Analytics.Startup();


            hotkeyManager = new HotKeyManager();
            hotkeyManager.Register(Key.F8, ModifierKeys.None);

            hotkeyManager.KeyPressed += hotkeyManager_KeyPressed;
        }

        void hotkeyManager_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            Activate();
            //Bring up the window on hotkey press

            CompileButton.Focus();
        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ThrowException("An unhandled exception occurred.", e.Exception);
        }

        private void ThrowException(string description, Exception e)
        {
            string crashName = DateTime.Now.ToString("s").Replace(":","-");

            MessageBox.Show(description + Environment.NewLine + "Crash report " + crashName + " written to dumps folder.");
            File.WriteAllText(Path.Combine("dumps", crashName + ".txt"), e.ToString() + e.InnerException ?? "");
            throw e;
        }



        #region SaveLoad
        private void LoadConfigs()
        {
            try
            {
                ConfigComboBox.Items.Clear();
                var dirs = Directory.GetDirectories("config");
                foreach (var dir in dirs)
                {
                    string configName = dir.Replace("config\\", "");
                    ConfigComboBox.Items.Add(configName);
                }

                if (uiConfig.Values.ContainsKey("lastConfig"))
                {
                    ConfigComboBox.SelectedItem = uiConfig["lastConfig"];
                }


            }
            catch (Exception e)
            {
                ThrowException("An error occured whilst loading the tool configurations.", e);
            }

        }

        private void LoadConfig(string configName)
        {
            CurrentConfig = configName;

            foreach (CompileProgram program in CompilePrograms)
            {
                program.LoadConfig(CurrentConfig);
            }

            gameProgram.LoadConfig(CurrentConfig);

            postCompile.LoadConfig(CurrentConfig);
        }

        #endregion


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog {Multiselect = true, Filter = "Valve Map Files (*.vmf)|*.vmf|All files (*.*)|*.*"};

            dialog.ShowDialog();

            if (dialog.FileNames.Any())
            {
                foreach (var fileName in dialog.FileNames)
                {
                    if(!mapFiles.Contains(fileName))
                        mapFiles.Add(fileName);
                }
            }
        }



        private void CompileButton_Click(object sender, RoutedEventArgs e)
        {
            if (mapFiles.Any())
                Compile();
        }

        private Thread CompileThread;
        private void Compile()
        {
            CancelCompileButton.Visibility = Visibility.Visible;
            CompileButton.Visibility = Visibility.Collapsed;

            OutputTab.Focus();

            oldTitle = Title;

            CompileOutputTextbox.Document.Blocks.Clear();

            CompileThread = new Thread(CompileThreaded);
            CompileThread.Start();
        }

        private void CompileThreaded()
        {
            float progress = 0f;
            foreach (var vmf in mapFiles)
            {
                Dispatcher.Invoke(() => AppendLine("Compiling " + vmf));

                foreach (var program in CompilePrograms.Where(p => p.DoRun))
                {
                    Dispatcher.Invoke(() => Title = string.Format("{0} {1}", program.ToolName, Path.GetFileNameWithoutExtension(vmf)).ToUpper());

                    bool failure = program.RunTool(this, vmf, gameInfo.GameFolder);
                    if (failure)
                    {
                        Dispatcher.Invoke(() => AppendLineC("Compile program failed", Brushes.OrangeRed));
                        Dispatcher.Invoke(CompileFinish);
                        return; //If true, then the compile was cancelled
                    }

                    progress += (1f / CompilePrograms.Count(p => p.DoRun)) / mapFiles.Count;

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
            try
            {
                CancelCompileButton.Visibility = Visibility.Hidden;
                CompileButton.Visibility = Visibility.Visible;

                Title = oldTitle;

                if (CopyMapCheckBox.IsChecked.GetValueOrDefault())
                {
                    foreach (var vmf in mapFiles)
                    {
                        string newmap = Path.Combine(gameInfo.MapFolder, Path.GetFileNameWithoutExtension(vmf) + ".bsp");
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

                        if (gameProgram.DoRun && mapFiles.IndexOf(vmf) == mapFiles.Count - 1)
                        {
                            gameProgram.Run(Path.GetFileNameWithoutExtension(vmf));
                        }
                    }
                }
                else if (gameProgram.DoRun)
                    gameProgram.Run(Path.GetFileNameWithoutExtension(mapFiles[mapFiles.Count - 1]));


                if (!Directory.Exists("logs"))
                    Directory.CreateDirectory("logs");

                string logText = new TextRange(CompileOutputTextbox.Document.ContentStart, CompileOutputTextbox.Document.ContentEnd).Text;

                File.WriteAllText(Path.Combine("logs", "Compile Pal log - " + DateTime.Now.ToString("s").Replace(":", "-") +".txt"),logText);
                postCompile.Run(mapFiles);
            }
            catch (Exception e)
            {
                ThrowException("An error occured whilst finalising the compile.", e);
            }

        }

        public void AppendLine(string s, params object[] arguments)
        {
            if (string.IsNullOrEmpty(s))
                return;
            s = string.Format(s, arguments);
            s += Environment.NewLine;

            CompileOutputTextbox.Focus();

            CompileOutputTextbox.AppendText(s);
            CompileOutputTextbox.ScrollToEnd();
        }
        public void AppendText(string s, params object[] arguments)
        {
            if (string.IsNullOrEmpty(s))
                return;
            s = string.Format(s, arguments);

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
            CompileButton.Visibility = Visibility.Visible;
            CancelCompileButton.Visibility = Visibility.Hidden;

            foreach (var program in CompilePrograms)
            {
                program.Kill();
            }

            //CompileThread.Abort();

            Title = oldTitle;

            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            TaskbarItemInfo.ProgressValue = 0;
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Path.Combine("config", NewConfigNameBox.Text)))
                Directory.CreateDirectory(Path.Combine("config", NewConfigNameBox.Text));

            foreach (CompileProgram program in CompilePrograms)
            {
                program.SaveConfig(NewConfigNameBox.Text);
            }
            gameProgram.SaveConfig(NewConfigNameBox.Text);

            CurrentConfig = NewConfigNameBox.Text;

            LoadConfigs();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (CompileProgram program in CompilePrograms)
                {
                    program.SaveConfig(CurrentConfig);
                }
                gameProgram.SaveConfig(CurrentConfig);

                postCompile.SaveConfig(CurrentConfig);

                LoadConfigs();
            }
            catch (Exception ex)
            {
                ThrowException("An error occured whilst saving the configurations.", ex);
            }
        }

        private void ConfigComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty((string)ConfigComboBox.SelectedItem))
                LoadConfig((string)ConfigComboBox.SelectedItem);
        }

        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            UpdateParameters();
        }
        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateParameters();
        }

        private void UpdateParameters()
        {
            foreach (CompileProgram program in CompilePrograms)
            {
                program.CollateParameters();
            }
            gameProgram.CollateParameters();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            uiConfig["copymap"] = CopyMapCheckBox.IsChecked.GetValueOrDefault();
            uiConfig["vmffiles"] = mapFiles;
            uiConfig["lastConfig"] = CurrentConfig;
        }

        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            if (TaskbarItemInfo.ProgressValue == 1.0)
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                TaskbarItemInfo.ProgressValue = 0;
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            mapFiles.Remove((string)mapFilesListBox.SelectedItem);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(new TextRange(CompileOutputTextbox.Document.ContentStart,CompileOutputTextbox.Document.ContentEnd).Text);
        }

        private void FileBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Multiselect = true};

            dialog.ShowDialog();

            if (dialog.FileNames.Any())
            {
                foreach (var fileName in dialog.FileNames)
                {
                    if (!postCompile.fileList.Contains(fileName))
                        postCompile.fileList.Add(fileName);
                }
            }
        }

        private void FileRemoveBox_Click(object sender, RoutedEventArgs e)
        {

            postCompile.fileList.Remove((string)RunFilesListBox.SelectedItem);
        }
    }
}
