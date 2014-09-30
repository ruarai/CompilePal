using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SharpConfig;

namespace CompilePal
{
    public class CompileProgram
    {
        private MainWindow parent;

        List<string> errors = new List<string>();

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

            errors = File.ReadAllLines(Path.Combine("config", "errors.txt")).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
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

            WriteLineOutC("Starting " + ToolName.ToUpper(), Brushes.Green);

            string finalParams = Parameters.Replace("$game", "\"" + gamePath + "\"");
            finalParams = finalParams.Replace("$map", "\"" + vmfFile + "\"");
            finalParams = finalParams.Replace("$bsp", "\"" + vmfFile.Replace(".vmf", ".bsp") + "\"");
            finalParams = finalParams.Replace("$zip", "\"" + MainWindow.BinFolder + "bspzip.exe\"");


            process = new Process { StartInfo = { RedirectStandardOutput = true, RedirectStandardInput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } };


            process.StartInfo.FileName = ToolPath;
            process.StartInfo.Arguments = finalParams;
            process.StartInfo.WorkingDirectory = RunningDirectory;

            process.Start();
            process.PriorityClass = ProcessPriorityClass.BelowNormal;

            char[] buffer = new char[256];
            Task<int> read = null;

            while (true)
            {
                if (read == null)
                    read = process.StandardOutput.ReadAsync(buffer, 0, buffer.Length);

                read.Wait(100); // an arbitray timeout

                if (read.IsCompleted)
                {
                    if (read.Result > 0)
                    {
                        string text = new string(buffer, 0, read.Result);
                        WriteTextOut(text);

                        if (ForceClosed)
                            break;

                        read = null; // ok, this task completed so we need to create a new one
                        continue;
                    }

                    // got -1, process ended
                    break;
                }
            }

            process.WaitForExit();
            if (!ForceClosed)
                WriteLineOutC("Finished " + ToolName.ToUpper(), Brushes.Green);

            return ForceClosed;
        }

        public void Kill()
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                ForceClosed = true;

                WriteLineOutC("Killed " + ToolName.ToUpper(), Brushes.OrangeRed);
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


        void WriteLineOut(string text)
        {
            parent.Dispatcher.Invoke(() => parent.AppendLine(text));
        }

        private string lineBuffer = "";
        void WriteTextOut(string text)
        {
            parent.Dispatcher.Invoke(() => parent.AppendText(text));

            //The process of trying to sort the random spouts of letters back into lines. Hacky.
            
            if (text.Contains("\n"))
            {
                lineBuffer += text;

                List<string> lines = lineBuffer.Split('\n').ToList();

                lineBuffer = lines.Last();

                foreach (string line in lines)
                {
                    if (ContainsError(line))
                        ForceClosed = true;
                }
            }
            else
                lineBuffer += text;
        }
        void WriteLineOutC(string text, Brush brush)
        {
            parent.Dispatcher.Invoke(() => parent.AppendLineC(text, brush));
        }

        bool ContainsError(string text)
        {
            foreach (string error in errors)
            {
                if (text.Contains(error))
                    return true;
            }
            return false;
        }
    }
}
