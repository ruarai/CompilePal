using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using CompilePalX.Annotations;
using CompilePalX.Compiling;
using Newtonsoft.Json;

namespace CompilePalX
{
    class CompileProcess
    {
        public List<Error> CompileErrors;
        public bool Draggable = true; // set to false if we ever want to disable reordering non custom compile steps

        public CompileMetadata Metadata;
        public string ParameterFolder = "./Parameters";

        public ObservableCollection<ConfigItem> ParameterList = new ObservableCollection<ConfigItem>();
        public ObservableDictionary<Preset, ObservableCollection<ConfigItem>> PresetDictionary = new ObservableDictionary<Preset, ObservableCollection<ConfigItem>>();

        public Process? Process;

        public CompileProcess(string name)
        {
            var jsonMetadata = Path.Combine(ParameterFolder, name, "meta.json");

            if (File.Exists(jsonMetadata))
            {
                Metadata = JsonConvert.DeserializeObject<CompileMetadata>(File.ReadAllText(jsonMetadata));

                CompilePalLogger.LogLine("Loaded JSON metadata {0} from {1} at order {2}", Metadata.Name, jsonMetadata, Metadata.Order);
            }
            else
            {
                var legacyMetadata = Path.Combine(ParameterFolder, name + ".meta");

                if (File.Exists(legacyMetadata))
                {
                    Metadata = LoadLegacyData(legacyMetadata);

                    Directory.CreateDirectory(Path.Combine(ParameterFolder, name));

                    File.WriteAllText(jsonMetadata, JsonConvert.SerializeObject(Metadata, Formatting.Indented));

                    CompilePalLogger.LogLine("Loaded CSV metadata {0} from {1} at order {2}, converted to JSON successfully.", Metadata.Name, legacyMetadata, Metadata.Order);
                }
                else
                {
                    throw new FileNotFoundException("The metadata file for " + name + " could not be found.");
                }

            }



            ParameterList = ConfigurationManager.GetParameters(Metadata.Name, Metadata.DoRun);

        }

        public string PresetFile => Metadata.Name + ".csv";

        public double Ordering => Metadata.Order;
        public bool DoRun
        {
            get => Metadata.DoRun;
            set => Metadata.DoRun = value;
        }
        public string Name => Metadata.Name;
        public string Description => Metadata.Description;
        public string Warning => Metadata.Warning;
        public bool IsDraggable => Draggable;
        [UsedImplicitly] public bool SupportsBSP => Metadata.SupportsBSP;

        [UsedImplicitly]
        public bool IsCompatible
        {
            get
            {
                // current game configuration has no SteamAppID
                if (GameConfigurationManager.GameConfiguration != null && GameConfigurationManager.GameConfiguration.SteamAppID == null)
                {
                    return true;
                }

                var currentAppID = (int)GameConfigurationManager.GameConfiguration!.SteamAppID!;

                // supported game ID list should take precedence. If defined, check that current GameConfiguration SteamID is in whitelist
                if (Metadata.CompatibleGames != null)
                {
                    return Metadata.CompatibleGames.Contains(currentAppID);
                }

                // If defined, check that current GameConfiguration SteamID is not in blacklist
                if (Metadata.IncompatibleGames != null)
                {
                    return !Metadata.IncompatibleGames.Contains(currentAppID);
                }

                // process does not define which games are supported
                return true;
            }
        }

        public static CompileMetadata LoadLegacyData(string csvFile)
        {
            var metadata = new CompileMetadata();

            var lines = File.ReadAllLines(csvFile);

            metadata.Name = lines[0];
            metadata.Path = lines[1];
            metadata.BasisString = lines[3];
            metadata.Order = float.Parse(lines[4], CultureInfo.InvariantCulture);
            metadata.DoRun = bool.Parse(lines[5]);
            metadata.ReadOutput = bool.Parse(lines[6]);
            if (lines.Count() > 7)
            {
                metadata.Warning = lines[7];
            }
            if (lines.Count() > 8)
            {
                metadata.Description = lines[8];
            }

            return metadata;
        }

        public virtual bool CanRun(CompileContext context)
        {
            if (context.Map.IsBSP && !SupportsBSP)
            {
                CompilePalLogger.LogLineDebug($"Map is BSP, skipping process {Name}");
                return false;
            }
            return true;
        }
        public virtual void Run(CompileContext context, CancellationToken cancellationToken) { }
        public virtual void Cancel()
        {
            if (Process is null || Process.Id == 0 || Process.HasExited)
            {
                return;
            }

            Process.Kill();
            CompilePalLogger.LogLineColor("\nKilled {0}.", (Brush) Application.Current.TryFindResource("CompilePal.Brushes.Severity4"), this.Metadata.Name);
        }


        public string GetParameterString()
        {
            var parameters = Metadata.Arguments;

            if (ConfigurationManager.CurrentPreset != null)
            {
                foreach (var parameter in PresetDictionary[ConfigurationManager.CurrentPreset])
                {
                    parameters += parameter.Parameter;

                    if (parameter.CanHaveValue && !string.IsNullOrEmpty(parameter.Value))
                    {
                        //Handle additional parameters in CUSTOM process
                        if (parameter.Name == "Run Program")
                        {
                            //Add args
                            parameters += " " + parameter.Value;

                            //Read Ouput
                            if (parameter.ReadOutput)
                            {
                                parameters += " " + parameter.ReadOutput;
                            }
                        }
                        else
                            // protect filepaths in quotes, since they can contain -
                        if (parameter.ValueIsFile || parameter.Value2IsFile)
                        {
                            parameters += $" \"{parameter.Value}\"";
                        }
                        else
                        {
                            parameters += " " + parameter.Value;
                        }
                    }
                }
            }

            parameters += Metadata.BasisString;

            return parameters;
        }

        public override string ToString()
        {
            return Metadata.Name;
        }
    }

    class CompileMetadata
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Arguments { get; set; } = string.Empty;
        public float Order { get; set; }

        public bool DoRun { get; set; }
        public bool ReadOutput { get; set; }

        public string Description { get; set; }
        public string Warning { get; set; }
        public bool PresetDefault { get; set; } = false;
        public bool CheckExitCode { get; set; } = true;
        public string BasisString { get; set; }
        public bool SupportsBSP { get; set; } = false;
        public HashSet<int>? IncompatibleGames { get; set; }
        public HashSet<int>? CompatibleGames { get; set; }
    }

    class CompileContext
    {
        public string BSPFile;
        public GameConfiguration Configuration;
        public string CopyLocation;
        public Map Map;
        public string MapFile;
    }
}
