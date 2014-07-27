using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace SharpConfig
{
    /// <summary>
    /// The basis of the SharpConfig configuration system.</summary>
    public class Config
    {
        public ObservableCollection<KeyValue> Values = new ObservableCollection<KeyValue>();
        /// <summary>
        /// Changes which namespace the configuration saves to. You must create a new object to load from a new namespace.</summary>
        public string Namespace;
        /// <summary>
        /// Whether or not the configuration will be automatically saved every time a value is modified. </summary>
        public bool AutoSave;

        private string DataFile;
        private string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        /// <summary>
        /// The constructor. Loads the configuration from the specified namespace on construction. </summary>
        /// <param name="nameSpace">Designates the configuration that will be loaded from and later saved to.</param>
        /// <param name="saveLocal">Designates whether the configuration will be saved in the executable's directory or not.</param>
        /// <param name="autoSave">Designates whether the configuration will be saved everytime it is changed.</param>
        public Config(string nameSpace, bool saveLocal = false, bool autoSave = false)
        {
            Namespace = nameSpace;
            AutoSave = autoSave;

            if (!saveLocal)
            {
                Directory.CreateDirectory(Path.Combine(appData, Namespace));

                DataFile = Path.Combine(appData, Namespace, "config.json");
            }
            else
            {
                //find where the exe of the application is stored
                //rather then using the working directory which may change
                string exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                //but if exeDirectory fails, we use the working directory.
                DataFile = Path.Combine(exeDirectory ?? Environment.CurrentDirectory, nameSpace + ".json");
            }

            Load();
        }

        /// <summary>
        /// Indexer for the configuration, providing dynamic access to the collection of values in the configuration.</summary>
        /// <param name="key">Designates what configuration item will be changed or retrieved.</param>
        public dynamic this[string key]
        {
            get { return Values.FirstOrDefault(i => i.Key == key).Value; }
            set
            {
                if (Exists(key))
                    Values.FirstOrDefault(i => i.Key == key).Value = value;
                else
                    Values.Add(new KeyValue() { Key = key, Value = value });

                if (AutoSave)
                    Save();
            }
        }

        /// <summary>
        /// Saves the configuration to the disk.</summary>
        public void Save()
        {
            string json = JsonConvert.SerializeObject(Values, Formatting.Indented);
            File.WriteAllText(DataFile, json);
        }

        public bool Exists(string key)
        {
            return Values.Any(i => i.Key == key);
        }

        /// <summary>
        /// Removes an item from the collection.</summary>
        public void Delete(string key)
        {
            Values.Remove(Values.FirstOrDefault(i => i.Key == key));

            if (AutoSave)
                Save();
        }

        /// <summary>
        /// Reloads the configuration from the disk.</summary>
        private void Load()
        {
            if (File.Exists(DataFile))
            {
                string json = File.ReadAllText(DataFile);
                Values = JsonConvert.DeserializeObject<ObservableCollection<KeyValue>>(json);
            }
        }
    }
}