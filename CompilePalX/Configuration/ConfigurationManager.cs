using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using CompilePalX.Compilers;
using CompilePalX.Compilers.BSPPack;
using CompilePalX.Compilers.UtilityProcess;
using CompilePalX.Compiling;
using CompilePalX.Configuration;
using Newtonsoft.Json;

namespace CompilePalX
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class PresetProcessParameter
    {
        public string Name { get; set; }
        public string? Value { get; set; }
        public string? Value2 { get; set; }
        public bool ReadOutput { get; set; }
        public bool WaitForExit { get; set; }
        public string? Order { get; set; }

        public PresetProcessParameter(ConfigItem config)
        {
            Name = config.Name;
            Value = config.Value;
            Value2 = config.Value2;
            ReadOutput = config.ReadOutput;
            WaitForExit = config.WaitForExit;
            Order = config.Warning; // HACK: old model uses Warning to store the order for CUSTOM PROGRAM step
        }

        [Newtonsoft.Json.JsonConstructor]
        public PresetProcessParameter(string name, string? value, string? value2, bool readOutput, bool waitForExit, string? order)
        {
            Name = name;
            Value = value;
            Value2 = value2;
            ReadOutput = readOutput;
            WaitForExit = waitForExit;
            Order = order;
        }
    }
    public class Preset : IEquatable<Preset>, ICloneable
    {
        public required string Name { get; set; }
        public string? Map { get; set; }
        public string? MapRegex { get; set; }
        public Dictionary<string, List<PresetProcessParameter>> Processes { get; set; } = [];

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
        public static ObservableCollection<CompileProcess> CompileProcesses = [];
        public static ObservableCollection<Preset> KnownPresets = [];
        public static Settings Settings = new Settings();

        public static Preset? CurrentPreset = null;

        private static readonly string ParametersFolder = "./Parameters";
        private static readonly string PresetsFolder = "./Presets";
        private static readonly string PluginFolder = "./Plugins";
        private static readonly string SettingsFile = "./Settings.json";
        

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
            var metadatas = Directory.GetDirectories(ParametersFolder).Concat(Directory.GetDirectories(PluginFolder)).ToArray();

            // load autodiscovered plugins
            if (GameConfigurationManager.GameConfiguration is not null && GameConfigurationManager.GameConfiguration.PluginFolder is not null)
            {
                CompilePalLogger.LogLineDebug($"Loading additional plugins from: {GameConfigurationManager.GameConfiguration.PluginFolder}");
                metadatas = metadatas.Concat(Directory.GetDirectories(GameConfigurationManager.GameConfiguration.PluginFolder)).ToArray();
            }

            foreach (var metadata in metadatas)
            {
                string folderName = Path.GetFileName(metadata);

                if (CompileProcesses.Any(c => String.Equals(c.Metadata.Name, folderName, StringComparison.CurrentCultureIgnoreCase)))
                    continue;

                string? parameterFolder = null;
                if (!String.Equals(Path.GetDirectoryName(metadata), ParametersFolder, StringComparison.CurrentCultureIgnoreCase))
                {
                    parameterFolder = Path.GetDirectoryName(metadata);
                }

                try
                {
                    var compileProcess = new CompileExecutable(folderName, parameterFolder);
                    CompileProcesses.Add(compileProcess);
                }
                catch (Exception ex)
                {
                    CompilePalLogger.LogLine($"Failed to load Compile Process: {metadata}, {ex}");
                }
            }

            //collect legacy metadatas
            var csvMetaDatas = Directory.GetFiles(ParametersFolder + "\\", "*.meta");

            foreach (var metadata in csvMetaDatas)
            {
                string name = Path.GetFileName(metadata).Replace(".meta", "");

                if (CompileProcesses.Any(c => String.Equals(c.Metadata.Name, name, StringComparison.CurrentCultureIgnoreCase)))
                    continue;

                try
                {
                    var compileProcess = new CompileExecutable(name);
                    CompileProcesses.Add(compileProcess);
                }
                catch (Exception ex)
                {
                    CompilePalLogger.LogLine($"Failed to load Compile Process: {metadata}, {ex}");
                }
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

                // handle legacy CSV presets
                if (preset.Processes.Count == 0)
                {
                    foreach (var process in CompileProcesses)
                    {
                        string file = Path.Combine(presetPath, process.PresetFile);
                        if (File.Exists(file))
                        {
                            process.PresetDictionary.Add(preset, []);
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

                            // convert to new preset parameter model
                            preset.Processes[process.Name] = process.PresetDictionary[preset].Select(config => new PresetProcessParameter(config)).ToList();
                        }
                    }
                } 
                else
                // handle json presets
                {
                    foreach ((var processName, var parameters) in preset.Processes)
                    {
                        var process = CompileProcesses.FirstOrDefault(p => p.Name == processName);
                        if (process is null)
                        {
                            CompilePalLogger.LogLine($"Failed to find process \"{processName}\" while loading presets");
                            continue;
                        }

                        process.PresetDictionary[preset] = [];
                        foreach (var parameter in parameters)
                        {
                            var configItem = process.ParameterList.FirstOrDefault(c => c.Name == parameter.Name);
                            if (configItem is null)
                            {
                                CompilePalLogger.LogLine($"Failed to find parameter \"{parameter.Name}\" while loading preset \"{processName}\"");
                                continue;
                            }

                            // TODO: this should be improved at some point. I dont think we need to store extra info such as Warnings, description, etc, for presets
                            //remove .clone if you are a masochist and wish to enter the object oriented version of hell
                            var equivalentItem = (ConfigItem)configItem.Clone();

                            equivalentItem.Value = parameter.Value;
                            equivalentItem.Value2 = parameter.Value2;
                            equivalentItem.ReadOutput = parameter.ReadOutput;
                            equivalentItem.WaitForExit = parameter.WaitForExit;
                            if (processName == "CUSTOM")
                            {
                                equivalentItem.Warning = parameter.Order;
                            }

                            process.PresetDictionary[preset].Add(equivalentItem);
                        }
                    }
                }

                CompilePalLogger.LogLine($"Added preset {preset.Name} {(preset.Map != null ? $"for map {preset.Map} " : "")}for processes {string.Join(", ", CompileProcesses)}");
                CurrentPreset = preset;
                KnownPresets.Add(preset);
            }
        }

        public static void LoadSettings()
        {
            if (!File.Exists(SettingsFile))
            {
                CompilePalLogger.LogLine("No settings file found, falling back to default");
                return;
            }

            var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsFile));
            if (settings is null)
            {
                CompilePalLogger.LogLine("Failed to load settings, falling back to default");
                return;
            }

            Settings = settings;
        }

        public static void SavePresets()
        {
            foreach (var knownPreset in KnownPresets)
            {
                SavePreset(knownPreset);
            }
        }

        public static void SavePreset(Preset preset)
        {
            foreach (var compileProcess in CompileProcesses)
            {
                // update preset processes/parameters incase they have been updated
                if (compileProcess.PresetDictionary.ContainsKey(preset))
                {
                    // convert ConfigItems to PresetParameters                        
                    preset.Processes[compileProcess.Name] = compileProcess.PresetDictionary[preset].Select(config => new PresetProcessParameter(config)).ToList();
                }
            }

            // save preset metadata
            string presetFolder = GetPresetFolder(preset);
            string metadataPath = Path.Combine(presetFolder, "meta.json");
            string jsonSaveText = JsonConvert.SerializeObject(preset, Formatting.Indented);

            File.WriteAllText(metadataPath, jsonSaveText);
        }

        public static void SaveProcesses()
        {
            foreach (var process in CompileProcesses)
            {
                string jsonMetadata = Path.Combine(process.ParameterFolder, process.Metadata.Name, "meta.json");
                File.WriteAllText(jsonMetadata, JsonConvert.SerializeObject(process.Metadata, Formatting.Indented));
            }
        }

        public static void SaveSettings(Settings settings)
        {
            File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(settings, Formatting.Indented));
            Settings = settings;
            ErrorFinder.Init(true);
        }
        public static void SaveSettings()
        {
            SaveSettings(Settings);
        }

        public static Preset NewPreset(Preset preset, bool initializeDefaultProcesses = true)
        {
            if (initializeDefaultProcesses)
            {
                string[] defaultProcesses = new string[] { "VBSP", "VVIS", "VRAD", "COPY", "GAME" };
                preset.Processes = defaultProcesses.ToDictionary(key => key, key => new List<PresetProcessParameter>());
            }

            // if map specific, append map to name so you can make map specific presets with the same name as global ones
            string folder = GetPresetFolder(preset);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);

                SavePreset(preset);
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

            preset.Processes = CurrentPreset.Processes;

            if (!Directory.Exists(newFolder))
            {
                Directory.CreateDirectory(newFolder);
                SavePreset(preset);
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

            // copy processes
            preset.Processes = CurrentPreset.Processes;

            // TODO: this can be improved, deleting and recreating isn't really neccessary now that all preset info is consolidated into one file
            // "Edit" preset by deleting the current preset and adding a new preset, then make it the currently selected preset
            RemovePreset(CurrentPreset);
            var newPreset = NewPreset(preset, false);

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
            if (CurrentPreset is null)
                return;

            CurrentPreset.Processes.Remove(name);
            // make sure value is also removed from preset in master preset list
            KnownPresets.FirstOrDefault(p => p.Name == CurrentPreset.Name)?.Processes.Remove(name);
            SavePreset(CurrentPreset);
        }


        public static ObservableCollection<ConfigItem> GetParameters(string processName, bool doRun = false, string? parameterFolder = null)
        {
            var list = new ObservableCollection<ConfigItem>();

            if ( parameterFolder is null)
            {
                parameterFolder = ParametersFolder;
            }

            string jsonParameters = Path.Combine(parameterFolder, processName, "parameters.json");

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
                string csvParameters = Path.Combine(parameterFolder, processName + ".csv");

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

            // Split on commas unless they are escaped with a backslash
            var pieces = Regex.Split(line, "(?<!\\\\),");

            if (pieces.Any())
            {
                // Custom parameter stores name as first value instead of parameter, because it has no parameter
                if (pieces[0] == "Command Line Argument")
                    item.Name = pieces[0];
                else
                    item.Parameter = pieces[0];

                if (pieces.Count() >= 2)
                    item.Value = pieces[1].Replace("\\,", ",");
				//Handle extra information stored for custom programs
	            if (pieces.Count() >= 3)
		            item.Value2 = pieces[2].Replace("\\,", ",");
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
			// Handle extra information stored for custom programs
	        if (item.Name == "Run Program")
		        return $"{item.Parameter},{item.Value.Replace(",", "\\,")},{item.Value2.Replace(",", "\\,")},{item.ReadOutput},{item.WaitForExit},{item.Warning}";
            else if (item.Name == "Command Line Argument") // Command line arguments have no parameter value
                return $"{item.Name},{item.Value.Replace(",", "\\,")}";
            return $"{item.Parameter},{item.Value?.Replace(",", "\\,")}";
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
