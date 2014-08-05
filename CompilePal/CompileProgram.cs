using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CompilePal
{
    public class CompileProgram
    {
        private MainWindow parent;

        public CompileProgram(string path, string name, DataGrid grid, TextBox box)
        {
            ToolName = name;
            ToolPath = path;

            ParameterGrid = grid;
            ParameterTextBox = box;
        }

        public DataGrid ParameterGrid;
        public TextBox ParameterTextBox;

        private Process process;
        private bool ForceClosed = false;

        public string ToolPath;
        public string ToolName;
        public string Parameters;
        public ObservableCollection<Parameter> ParameterList = new ObservableCollection<Parameter>();

        public bool RunTool(MainWindow _parent, string vmfFile, string gamePath)
        {
            parent = _parent;

            WriteTextOutC("Starting " + ToolName.ToUpper(), Brushes.Green);

            string finalParams = Parameters.Replace("$game", "\"" + gamePath + "\"");
            finalParams = finalParams.Replace("$map", "\"" + vmfFile + "\"");


            process = new Process { StartInfo = { RedirectStandardOutput = true, RedirectStandardInput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } };

            process.OutputDataReceived += Process_OutputDataReceived;

            process.StartInfo.FileName = ToolPath;
            process.StartInfo.Arguments = finalParams;
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

        public void CollateParameters()
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


            Parameters += " -game $game";
            Parameters += " $map";

            ParameterTextBox.Text = Parameters;
        }

        public void SaveConfig(string configName)
        {
            string fileName = Path.Combine("config", configName, ToolName + ".csv");
            if (File.Exists(fileName))
                File.Delete(fileName);


            var lines = new List<string>();
            foreach (var para in ParameterList)
            {
                lines.Add(string.Format("{0}^{1}^{2}^{3}^{4}", para.Name, para.Command, para.Option, para.Description, para.Enabled));
            }

            File.WriteAllLines(fileName, lines);
        }

        public void LoadConfig(string configName)
        {
            string fileName = Path.Combine("config", configName, ToolName + ".csv");

            ParameterList.Clear();
            var lines = File.ReadAllLines(fileName);
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

                ParameterList.Add(param);

            }

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
        void WriteTextOutC(string text,Brush brush)
        {
            parent.Dispatcher.Invoke(() => parent.AppendLineC(text,brush));
        }
    }
}
