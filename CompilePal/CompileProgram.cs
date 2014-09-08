using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using SharpConfig;

namespace CompilePal
{
    public class CompileProgram
    {
        private MainWindow parent;

        public CompileProgram(string path, string name, DataGrid grid, TextBox box, CheckBox doRunCheckBox)
        {
            ToolName = name;
            ToolPath = path;

            ParameterGrid = grid;
            ParameterTextBox = box;

            RunToolBox = doRunCheckBox;

            DoRun = RunToolBox.IsChecked.GetValueOrDefault();
            RunToolBox.Checked += RunToolBox_Checked;
            RunToolBox.Unchecked += RunToolBox_Checked;
        }

        void RunToolBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            DoRun = RunToolBox.IsChecked.GetValueOrDefault();
        }

        public DataGrid ParameterGrid;
        public TextBox ParameterTextBox;
        public CheckBox RunToolBox;

        public bool DoRun = false;

        private Process process;
        private bool ForceClosed;

        public string ToolPath;
        public string ToolName;
        public string Parameters;
        public ObservableCollection<Parameter> ParameterList = new ObservableCollection<Parameter>();

        public string ForcedParameters;

        public string RunningDirectory = "dumps";

        public bool RunTool(MainWindow _parent, string vmfFile, string gamePath)
        {
            ForceClosed = false;

            parent = _parent;

            WriteTextOutC("Starting " + ToolName.ToUpper(), Brushes.Green);

            string finalParams = Parameters.Replace("$game", "\"" + gamePath + "\"");
            finalParams = finalParams.Replace("$map", "\"" + vmfFile + "\"");
            finalParams = finalParams.Replace("$bsp", "\"" + vmfFile.Replace(".vmf", ".bsp") + "\"");
            finalParams = finalParams.Replace("$zip", "\"" + MainWindow.BinFolder + "bspzip.exe\"");


            process = new Process { StartInfo = { RedirectStandardOutput = true, RedirectStandardInput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } };

            process.OutputDataReceived += Process_OutputDataReceived;

            process.StartInfo.FileName = ToolPath;
            process.StartInfo.Arguments = finalParams;
            process.StartInfo.WorkingDirectory = RunningDirectory;

            process.Start();
            process.PriorityClass = ProcessPriorityClass.BelowNormal;

            process.BeginOutputReadLine();

            process.WaitForExit();
            if (!ForceClosed)
                WriteTextOutC("Finished " + ToolName.ToUpper(), Brushes.Green);

            return ForceClosed;
        }

        public void Kill()
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                ForceClosed = true;

                WriteTextOutC("Killed " + ToolName.ToUpper(), Brushes.OrangeRed);
            }
        }

        public virtual void CollateParameters()
        {
            Parameters = string.Empty;
            foreach (var parameter in ParameterList)
            {
                if (parameter.Enabled)
                {
                    Parameters += parameter.Command;
                    if (!string.IsNullOrEmpty(parameter.Option))
                        Parameters += " " + parameter.Option;
                }
            }

            Parameters += ForcedParameters;

            ParameterTextBox.Text = Parameters;
        }

        public void SaveConfig(string configName)
        {
            var programConfig = new Config(Path.Combine("config", configName, ToolName) + ".json", true);

            programConfig["parameters"] = ParameterList;

            programConfig["run"] = RunToolBox.IsChecked.GetValueOrDefault();
        }

        public void LoadConfig(string configName)
        {
            var programConfig = new Config(Path.Combine("config", configName, ToolName) + ".json", true);

            if (programConfig.Values.ContainsKey("run"))
                RunToolBox.IsChecked = programConfig["run"];

            ParameterList = programConfig["parameters"].ToObject<ObservableCollection<Parameter>>();

            ParameterGrid.ItemsSource = ParameterList;

            CollateParameters();
        }

        void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            WriteTextOut(e.Data);
        }

        void WriteTextOut(string text)
        {
            parent.Dispatcher.Invoke(() => parent.AppendLine(text));
        }
        void WriteTextOutC(string text, Brush brush)
        {
            parent.Dispatcher.Invoke(() => parent.AppendLineC(text, brush));
        }
    }
}
