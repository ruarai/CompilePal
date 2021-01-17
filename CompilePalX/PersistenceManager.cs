using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Documents;
using CompilePalX.Compiling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CompilePalX
{
    static class PersistenceManager
    {
        private static string mapFiles = "mapfiles.json";
        public static void Init()
        {
            if (File.Exists(mapFiles))
            {
                var list = JsonConvert.DeserializeObject<List<object>>(File.ReadAllText(mapFiles));
                var mapList = new List<Map>();

                // make this backwards compatible by allowing plain string values in maplist array (old format)
                foreach (var item in list)
                {
                    if (item is string mapFile)
                        mapList.Add(new Map(mapFile));
                    else if (item is JObject obj)
                        mapList.Add(obj.ToObject<Map>());
                    else
                        CompilePalLogger.LogDebug($"Failed to load item from mapfiles: {item}");
                }

                CompilingManager.MapFiles = new TrulyObservableCollection<Map>(mapList);
            }

            CompilingManager.MapFiles.CollectionChanged +=
                delegate
                {
                    File.WriteAllText(mapFiles, JsonConvert.SerializeObject(CompilingManager.MapFiles,Formatting.Indented));
                };
        }
    }
}
