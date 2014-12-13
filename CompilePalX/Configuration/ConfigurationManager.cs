using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace CompilePalX
{

    static class ConfigurationManager
    {
        public static ObservableCollection<CompileProcess> CompileProcesses = new ObservableCollection<CompileProcess>();
        public static ObservableCollection<string> KnownPresets = new ObservableCollection<string>();

        public static string CurrentPreset = "Fast";

        private const string ParametersFolder = "Parameters";
        private const string PresetsFolder = "Presets";

        public static void AssembleParameters()
        {
            CompileProcesses.Clear();

            var metadatas = Directory.GetFiles(ParametersFolder + "\\", "*.meta");

            foreach (var metadata in metadatas)
            {
                var compileProcess = new CompileProcess(metadata);

                CompileProcesses.Add(compileProcess);
            }



            CompileProcesses = new ObservableCollection<CompileProcess>(CompileProcesses.OrderBy(c => c.Order));

            AssemblePresets();
        }

        private static void AssemblePresets()
        {
            var presets = Directory.GetDirectories(PresetsFolder);

            KnownPresets.Clear();

            foreach (var process in CompileProcesses)
            {
                process.PresetDictionary.Clear();
            }

            foreach (string presetPath in presets)
            {
                string preset = Path.GetFileName(presetPath);
                foreach (var process in CompileProcesses)
                {
                    process.PresetDictionary.Add(preset, new ObservableCollection<ConfigItem>());

                    string file = Path.Combine(presetPath, process.PresetFile);
                    if (File.Exists(file))
                    {
                        var lines = File.ReadAllLines(file);

                        foreach (var line in lines)
                        {
                            var item = ParsePresetLine(line);

                            var equivalentparameter = process.ParameterList.FirstOrDefault(c => c.Parameter == item.Parameter);

                            if (equivalentparameter != null)
                            {
                                equivalentparameter.Value = item.Value;

                                process.PresetDictionary[preset].Add(equivalentparameter);
                            }
                        }
                    }
                }
                CurrentPreset = preset;
                KnownPresets.Add(preset);
            }
        }

        public static void SavePresets()
        {
            foreach (var knownPreset in KnownPresets)
            {
                string presetFolder = Path.Combine(PresetsFolder, knownPreset);

                foreach (var compileProcess in CompileProcesses)
                {
                    var lines = new List<string>();
                    foreach (var item in compileProcess.PresetDictionary[knownPreset])
                    {
                        string line = WritePresetLine(item);
                        lines.Add(line);
                    }

                    string presetPath = Path.Combine(presetFolder, compileProcess.PresetFile);

                    File.WriteAllLines(presetPath, lines);
                }
            }
        }

        public static void SaveProcesses()
        {
            foreach (var process in CompileProcesses)
            {
                process.SaveMetadata();
            }
        }

        public static void NewPreset(string name)
        {
            string folder = Path.Combine(PresetsFolder, name);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);

                foreach (var process in CompileProcesses)
                {
                    string path = Path.ChangeExtension(Path.Combine(folder, process.ParameterFile), "csv");
                    File.Create(path).Close();
                }
            }


            AssembleParameters();
        }

        public static void RemovePreset(string name)
        {
            string folder = Path.Combine(PresetsFolder, name);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }


            AssembleParameters();
        }


        public static ObservableCollection<ConfigItem> GetParameters(string parameterlist)
        {
            var list = new ObservableCollection<ConfigItem>();

            string basePath = Path.Combine(ParametersFolder, parameterlist);

            var baselines = File.ReadAllLines(basePath);

            for (int i = 2; i < baselines.Length; i++)
            {
                string baseline = baselines[i];

                var item = ParseBaseLine(baseline);

                list.Add(item);
            }

            return list;
        }

        private static ConfigItem ParsePresetLine(string line)
        {
            var item = new ConfigItem();

            var pieces = line.Split(',');

            item.Parameter = pieces[0];
            item.Value = pieces[1];
            return item;
        }

        private static string WritePresetLine(ConfigItem item)
        {
            return string.Format("{0},{1}", item.Parameter, item.Value);
        }


        private static ConfigItem ParseBaseLine(string line)
        {
            var item = new ConfigItem();

            var pieces = line.Split(';');

            item.Name = pieces[0];
            item.Parameter = pieces[1];
            item.CanHaveValue = bool.Parse(pieces[2]);
            item.Description = pieces[3];
            item.Warning = pieces[4];
            return item;
        }
    }
}
