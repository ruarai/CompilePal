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
using SharpConfig;

namespace CompilePal
{
    class PostCompileActions
    {
        public CheckBox shutdownCheckBox;
        public CheckBox runFileCheckBox;
        public CheckBox archiveMapCheckBox;

        public ListBox fileListBox;

        public ObservableCollection<string> fileList = new ObservableCollection<string>() { "sadasioduaois", "dsad" };



        public static void Shutdown()
        {
            Process.Start("shutdown", "/s /t 120 /c \"The computer will shut down in 120 seconds as the compile has finished.\"");
        }
        public static void Run(string path)
        {
            Process.Start(path);
        }

        public static void Archive(string vmfPath)
        {
            File.Copy(vmfPath, vmfPath.Replace(".vmf", " " + DateTime.Now.ToString("s").Replace(":", "-") + ".vmf"));
        }

        public void Run(IEnumerable<string> vmfFiles)
        {
            if (shutdownCheckBox.IsChecked.GetValueOrDefault())
                Shutdown();


            if (runFileCheckBox.IsChecked.GetValueOrDefault())
            {
                foreach (var file in fileList)
                {
                    Run(file);
                }
            }

            if (archiveMapCheckBox.IsChecked.GetValueOrDefault())
            {
                foreach (var vmfFile in vmfFiles)
                {
                    Archive(vmfFile);
                }
            }
        }

        public void SaveConfig(string configName)
        {
            var programConfig = new Config(Path.Combine("config", configName, "post") + ".json", true);

            programConfig["shutdown"] = shutdownCheckBox.IsChecked.GetValueOrDefault();
            programConfig["runFile"] = runFileCheckBox.IsChecked.GetValueOrDefault();
            programConfig["archive"] = archiveMapCheckBox.IsChecked.GetValueOrDefault();

            programConfig["files"] = fileList;

        }

        public void LoadConfig(string configName)
        {
            var programConfig = new Config(Path.Combine("config", configName, "post") + ".json", true);

            shutdownCheckBox.IsChecked = programConfig["shutdown"];
            runFileCheckBox.IsChecked = programConfig["runFile"];
            archiveMapCheckBox.IsChecked = programConfig["archive"];

            fileList.Clear();

            foreach (var file in programConfig["files"])
            {
                fileList.Add(file);
            }

        }
    }
}
