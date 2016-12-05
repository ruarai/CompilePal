using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Documents;
using Newtonsoft.Json;

namespace CompilePalX
{
    static class PersistenceManager
    {
        private static string mapFiles = "mapfiles.json";
        public static void Init()
        {
            if (File.Exists(mapFiles))
            {
                var list = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(mapFiles));

                CompilingManager.MapFiles = new ObservableCollection<string>(list);
            }

            CompilingManager.MapFiles.CollectionChanged +=
                delegate
                {
                    File.WriteAllText(mapFiles, JsonConvert.SerializeObject(CompilingManager.MapFiles,Formatting.Indented));
                };
        }
    }
}
