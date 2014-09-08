using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using SharpConfig;

namespace CompilePal
{
    class GameProgram
    {
        public string Parameters;
        public ObservableCollection<Parameter> ParameterList = new ObservableCollection<Parameter>();

        public string ToolPath;
        public string ToolName;


        public DataGrid ParameterGrid;
        public TextBox ParameterTextBox;
        public CheckBox RunToolBox;


        public bool DoRun;

        
        public GameProgram(string path, string name, DataGrid grid, TextBox box,DataGrid paramDataGrid,TextBox paramTextBox,CheckBox doRunCheckBox)
        {

            ToolName = name;
            ToolPath = path;

            ParameterGrid = paramDataGrid;
            ParameterTextBox = paramTextBox;

            RunToolBox = doRunCheckBox;

            DoRun = RunToolBox.IsChecked.GetValueOrDefault();
            RunToolBox.Checked += RunToolBox_Checked;
            RunToolBox.Unchecked += RunToolBox_Checked;
        }

        void RunToolBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            DoRun = RunToolBox.IsChecked.GetValueOrDefault();
        }

        public void Run(string mapName)
        {
            string finalParams = Parameters.Replace("$map", mapName);
            Process.Start(ToolPath, finalParams);
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


            ParameterTextBox.Text = Parameters;
        }
    }
}
