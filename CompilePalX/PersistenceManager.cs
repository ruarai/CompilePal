using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;

namespace CompilePalX
{
    static class PersistenceManager
    {
        private static string mapFiles = "mapfiles.csv";
        public static void Init()
        {
            if (File.Exists(mapFiles))
                CompilingManager.MapFiles = new ObservableCollection<string>(File.ReadAllLines(mapFiles));

            CompilingManager.MapFiles.CollectionChanged +=
                delegate { File.WriteAllLines(mapFiles, CompilingManager.MapFiles); };
        }
    }
}
