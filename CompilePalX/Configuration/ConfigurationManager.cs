using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using CompilePalX.Compilers;
using CompilePalX.Compilers.BSPPack;
using CompilePalX.Compilers.UtilityProcess;
using CompilePalX.Compiling;
using Newtonsoft.Json;

namespace CompilePalX
{
    public class Preset : IEquatable<Preset>, ICloneable
    {
        public string Name { get; set; }
        public string? Map { get; set; }
        public string? MapRegex { get; set; }

        public bool Equals(Preset? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && MapRegex == other.MapRegex && Map == other.Map;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Preset)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, MapRegex, Map);
        }
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        /// <summary>
        /// Returns whether a map can use the preset
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        public bool IsValidMap(string mapName)
        {
            // presets with no map are global
            return this.MapRegex == null || Regex.IsMatch(mapName, MapRegex);
        }
    }

    static class ConfigurationManager
    {
        public static ObservableCollection<CompileProcess> CompileProcesses = new ObservableCollection<CompileProcess>();
        public static ObservableCollection<Preset> KnownPresets = new();

        public static Preset? CurrentPreset = null;

        private static readonly string ParametersFolder = "./Parameters";
        private static readonly string PresetsFolder = "./Presets";
        

        public static void AssembleParameters()
        {
            CompileProcesses.Clear();

            CompileProcesses.Add(new BSPPack());
            CompileProcesses.Add(new CubemapProcess());
            CompileProcesses.Add(new NavProcess());
            CompileProcesses.Add(new ShutdownProcess());
            CompileProcesses.Add(new UtilityProcess());
			CompileProcesses.Add(new CustomProcess());

            //collect new metadatas

            var metadatas = Directory.GetDirectories(ParametersFolder);

            foreach (var metadata in metadatas)
            {
                string folderName = Path.GetFileName(metadata);

                if (CompileProcesses.Any(c => String.Equals(c.Metadata.Name, folderName, StringComparison.CurrentCultureIgnoreCase)))
                    continue;

                var compileProcess = new CompileExecutable(folderName);

                CompileProcesses.Add(compileProcess);
            }

            //collect legacy metadatas
            var csvMetaDatas = Directory.GetFiles(ParametersFolder + "\\", "*.meta");

            foreach (var metadata in csvMetaDatas)
            {
                string name = Path.GetFileName(metadata).Replace(".meta", "");

                if (CompileProcesses.Any(c => String.Equals(c.Metadata.Name, name, StringComparison.CurrentCultureIgnoreCase)))
                    continue;

                var compileProcess = new CompileExecutable(name);

                CompileProcesses.Add(compileProcess);
            }



            CompileProcesses = new ObservableCollection<CompileProcess>(CompileProcesses.OrderBy(c => c.Metadata.Order));

            AssemblePresets();
        }

        private static void AssemblePresets()
        {
            if (!Directory.Exists(PresetsFolder))
                Directory.CreateDirectory(PresetsFolder);

            //get a list of presets from the directories in the preset folder
            var presets = Directory.GetDirectories(PresetsFolder);

            //clear old lists
            KnownPresets.Clear();

            foreach (var process in CompileProcesses)
            {
                process.PresetDictionary.Clear();
            }

            foreach (string presetPath in presets)
            {
                string presetName = Path.GetFileName(presetPath);

                // try reading preset metadata
                string metadataFile = Path.Combine(presetPath, "meta.json");

                Preset preset;
                if (File.Exists(metadataFile))
                    preset = JsonConvert.DeserializeObject<Preset>(File.ReadAllText(metadataFile)) ?? new Preset() { Name = presetName };
                else
                    // legacy presets don't have metadata, use folder name as preset name
                    preset = new Preset() { Name = presetName };

                foreach (var process in CompileProcesses)
                {
                    string file = Path.Combine(presetPath, process.PresetFile);
                    if (File.Exists(file))
                    {
                        process.PresetDictionary.Add(preset, new ObservableCollection<ConfigItem>());
                        //read the list of preset parameters
                        var lines = File.ReadAllLines(file);

                        foreach (var line in lines)
                        {
	                        var item = ParsePresetLine(line);

                            if (process.ParameterList.Any(c => c.Parameter == item.Parameter))
                            {
                                //remove .clone if you are a masochist and wish to enter the object oriented version of hell
                                var equivalentItem = (ConfigItem)process.ParameterList.FirstOrDefault(c => c.Parameter == item.Parameter).Clone();

                                equivalentItem.Value = item.Value;

								//Copy extra information stored for custom programs
	                            if (item.Parameter == "program")
	                            {
									equivalentItem.Value2 = item.Value2;
									equivalentItem.WaitForExit= item.WaitForExit;
		                            equivalentItem.Warning = item.Warning;
	                            }
	                            

                                process.PresetDictionary[preset].Add(equivalentItem);
                            }
                        }
                    }
                }

                CompilePalLogger.LogLine($"Added preset {preset.Name} {(preset.Map != null ? $"for map {preset.Map} " : "")}for processes {string.Join(", ", CompileProcesses)}");
                CurrentPreset = preset;
                KnownPresets.Add(preset);

            }
        }

        public static void SavePresets()
        {
            foreach (var knownPreset in KnownPresets)
            {
                string presetFolder =  GetPresetFolder(knownPreset);

                foreach (var compileProcess in CompileProcesses)
                {
                    if (compileProcess.PresetDictionary.ContainsKey(knownPreset))
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

                // save preset metadata
                string metadataPath = Path.Combine(presetFolder, "meta.json");
                string jsonSaveText = JsonConvert.SerializeObject(knownPreset, Formatting.Indented);

                File.WriteAllText(metadataPath, jsonSaveText);
            }
        }

        public static void SaveProcesses()
        {
            foreach (var process in CompileProcesses)
            {
                string jsonMetadata = Path.Combine("./Parameters", process.Metadata.Name, "meta.json");

                File.WriteAllText(jsonMetadata, JsonConvert.SerializeObject(process.Metadata, Formatting.Indented));
            }
        }

        public static Preset NewPreset(Preset preset)
        {
            string[] defaultProcesses = new string[] { "VBSP", "VVIS", "VRAD", "COPY", "GAME" };

            // if map specific, append map to name so you can make map specific presets with the same name as global ones
            string folder = GetPresetFolder(preset);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);

                foreach (var process in CompileProcesses)
                {
                    if (defaultProcesses.Contains(process.Metadata.Name))
                    {
                        string path = Path.ChangeExtension(Path.Combine(folder, process.Metadata.Name), "csv");
                        File.Create(path).Close();
                    }
                }

                // create metadata
                string metadataPath = Path.Combine(folder, "meta.json");
                string jsonSaveText = JsonConvert.SerializeObject(preset, Formatting.Indented);
                File.WriteAllText(metadataPath, jsonSaveText);
            }

            AssembleParameters();
            return preset;
        }
        public static Preset? ClonePreset(Preset preset)
        {
            if (CurrentPreset == null)
                return null;

            // if map specific, append map to name so you can make map specific presets with the same name as global ones
            string newFolder = GetPresetFolder(preset);

            // if cloned preset is map specific, append map to name
            string oldFolder = GetPresetFolder(CurrentPreset);

            if (!Directory.Exists(newFolder))
            {
                SavePresets();

                DirectoryCopy(oldFolder, newFolder, true);

                // overwrite metadata
                string metadataPath = Path.Combine(newFolder, "meta.json");
                string jsonSaveText = JsonConvert.SerializeObject(preset, Formatting.Indented);
                File.WriteAllText(metadataPath, jsonSaveText);

                AssembleParameters();
            }

            return preset;
        }

        public static Preset? EditPreset(Preset preset)
        {
            if (CurrentPreset == null)
            {
                return null;
            }

            // "Edit" preset by deleting the current preset and adding a new preset, then make it the currently selected preset
            RemovePreset(CurrentPreset);
            var newPreset = NewPreset(preset);

            CurrentPreset = newPreset;

            return newPreset;
        }

        private static string GetPresetFolder(Preset preset)
        {
            return preset.Map != null ? Path.Combine(PresetsFolder, $"{preset.Name}_{preset.Map}") : Path.Combine(PresetsFolder, preset.Name);
        }
        public static void RemovePreset(Preset preset)
        {
            string folder = GetPresetFolder(preset);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }


            AssembleParameters();
        }
        public static void RemoveProcess(string name)
        {
            if (CurrentPreset == null)
                return;

            string presetPath = Path.Combine(GetPresetFolder(CurrentPreset), name.ToLower() + ".csv");
            if (File.Exists(presetPath))
            {
                File.Delete(presetPath);
            }
        }


        public static ObservableCollection<ConfigItem> GetParameters(string processName, bool doRun = false)
        {
            var list = new ObservableCollection<ConfigItem>();

            string jsonParameters = Path.Combine(ParametersFolder, processName, "parameters.json");

            if (File.Exists(jsonParameters))
            {
                ConfigItem[] items = JsonConvert.DeserializeObject<ConfigItem[]>(File.ReadAllText(jsonParameters));
                foreach (var configItem in items)
                {
                    list.Add(configItem);
                }

                // add custom parameter to all runnable steps
                if (doRun)
                {
                    list.Add(new ConfigItem()
                    {
                        Name = "Command Line Argument",
                        CanHaveValue = true,
                        CanBeUsedMoreThanOnce = true,
                        Description = "Passes value as a command line argument",
                    });
                }
            }
            else
            {
                string csvParameters = Path.Combine(ParametersFolder, processName + ".csv");

                if (File.Exists(csvParameters))
                {
                    var baselines = File.ReadAllLines(csvParameters);

                    for (int i = 2; i < baselines.Length; i++)
                    {
                        string baseline = baselines[i];

                        var item = ParseBaseLine(baseline);

                        list.Add(item);
                    }

                    ConfigItem[] items = list.ToArray();

                    File.WriteAllText(jsonParameters, JsonConvert.SerializeObject(items, Formatting.Indented));
                }
                else
                {
                    throw new FileNotFoundException("Parameter files could not be found for " + processName);
                }
            }


            return list;
        }

        private static ConfigItem ParsePresetLine(string line)
        {
            var item = new ConfigItem();

            var pieces = line.Split(',');

            if (pieces.Any())
            {
                // Custom parameter stores name as first value instead of parameter, because it has no parameter
                if (pieces[0] == "Command Line Argument")
                    item.Name = pieces[0];
                else
                    item.Parameter = pieces[0];

                if (pieces.Count() >= 2)
                    item.Value = pieces[1];
				//Handle extra information stored for custom programs
	            if (pieces.Count() >= 3)
		            item.Value2 = pieces[2];
	            if (pieces.Length >= 4)
		            item.ReadOutput = Convert.ToBoolean(pieces[3]);
	            if (pieces.Length >= 5)
					item.WaitForExit= Convert.ToBoolean(pieces[4]);
	            if (pieces.Length >= 6)
		            item.Warning = pieces[5];
            }
            return item;
        }

		private static string WritePresetLine(ConfigItem item)
        {
			//Handle extra information stored for custom programs
	        if (item.Name == "Run Program")
		        return $"{item.Parameter},{item.Value},{item.Value2},{item.ReadOutput},{item.WaitForExit},{item.Warning}";
            else if (item.Name == "Command Line Argument") // Command line arguments have no parameter value
                return $"{item.Name},{item.Value}";
            return $"{item.Parameter},{item.Value}";
        }

        private static ConfigItem ParseBaseLine(string line)
        {
            var item = new ConfigItem();

            var pieces = line.Split(';');

            if (pieces.Any())
            {
                item.Name = pieces[0];
                if (pieces.Count() >= 2)
                    item.Parameter = pieces[1];
                if (pieces.Count() >= 3)
                    item.CanHaveValue = bool.Parse(pieces[2]);
                if (pieces.Count() >= 4)
                    item.Description = pieces[3];
                if (pieces.Count() >= 5)
                    item.Warning = pieces[4];
            }
            return item;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
