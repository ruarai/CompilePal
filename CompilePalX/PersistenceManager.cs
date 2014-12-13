using System.Collections.ObjectModel;
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


            CompilingManager.MapFiles.CollectionChanged += MapFiles_CollectionChanged;
        }

        static void MapFiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            File.WriteAllLines(mapFiles, CompilingManager.MapFiles);
        }
    }
}
