using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CompilePalX.Compiling;
using Newtonsoft.Json;

namespace CompilePalX
{
    class CompileProcess
    {
        public string ParameterFolder = CompilePalPath.Directory + "Parameters";
	    public bool Draggable = false;

        public CompileProcess(string name)
        {
            string jsonMetadata = Path.Combine(ParameterFolder, name, "meta.json");

            if (File.Exists(jsonMetadata))
            {
                Metadata = JsonConvert.DeserializeObject<CompileMetadata>(File.ReadAllText(jsonMetadata));

                CompilePalLogger.LogLine("Loaded JSON metadata {0} from {1} at order {2}", Metadata.Name, jsonMetadata, Metadata.Order);
            }
            else
            {
                string legacyMetadata = Path.Combine(ParameterFolder, name + ".meta");

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



            ParameterList = ConfigurationManager.GetParameters(Metadata.Name);

        }

        public static CompileMetadata LoadLegacyData(string csvFile)
        {
            CompileMetadata metadata = new CompileMetadata();

            var lines = File.ReadAllLines(csvFile);

            metadata.Name = lines[0];
            metadata.Path = lines[1];
            metadata.BasisString = lines[3];
            metadata.Order = float.Parse(lines[4], CultureInfo.InvariantCulture);
            metadata.DoRun = bool.Parse(lines[5]);
            metadata.ReadOutput = bool.Parse(lines[6]);
            if (lines.Count() > 7)
                metadata.Warning = lines[7];
            if (lines.Count() > 8)
                metadata.Description = lines[8];

            return metadata;
        }

        public CompileMetadata Metadata;

        public string PresetFile { get { return Metadata.Name + ".csv"; } }

        public double Ordering { get { return Metadata.Order; } }
        public bool DoRun { get { return Metadata.DoRun; } set { Metadata.DoRun = value; } }
        public string Name { get { return Metadata.Name; } }
        public string Description { get { return Metadata.Description; } }
        public string Warning { get { return Metadata.Warning; } }
		public bool IsDraggable { get { return Draggable; } }

        public Process Process;

        public virtual void Run(CompileContext context)
        {

        }
        public virtual void Cancel()
        {

        }

        public ObservableCollection<ConfigItem> ParameterList = new ObservableCollection<ConfigItem>();
        public ObservableDictionary<string, ObservableCollection<ConfigItem>> PresetDictionary = new ObservableDictionary<string, ObservableCollection<ConfigItem>>();


        public string GetParameterString()
        {
            string parameters = string.Empty;
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
							parameters += " " + parameter.ReadOutput;
					}
					else
						parameters += " " + parameter.Value;
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

        public float Order { get; set; }

        public bool DoRun { get; set; }
        public bool ReadOutput { get; set; }

        public string Description { get; set; }
        public string Warning { get; set; }

        public bool PresetDefault { get; set; }

        public string BasisString { get; set; }
    }

    class CompileContext
    {
        public string MapFile;
        public GameConfiguration Configuration;
        public string BSPFile;
        public string CopyLocation;
    }
}
