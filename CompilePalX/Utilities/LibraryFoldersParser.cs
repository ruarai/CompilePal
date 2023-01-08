using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompilePalX.Compiling;

namespace CompilePalX.Utilities
{
    internal class LibraryFoldersParser
    {
        private KV.DataBlock head;
        public LibraryFoldersParser(string libraryFoldersPath)
        {
            this.head = new KV.FileData(libraryFoldersPath).headnode;
        }

        /// <summary>
        /// Parses libraryfolders.vdf
        /// </summary>
        /// <param name="steamAppsPath"></param>
        /// <returns>A list of tuples containing base path and steamID</returns>
        public List<(string basePath, string steamId)>? GetInstallLocations()
        {
            var folders = this.head.GetFirstByName("\"libraryfolders\"");
            if (folders is null)
            {
                CompilePalLogger.LogLineDebug("No library folders found");
                return null;
            }

            // create list of steam ID's and their base path
            var locations = new List<(string basePath, string steamId)>();
            foreach (var folder in folders.subBlocks)
            {
                var basePath = Path.Combine(folder.TryGetStringValue("path"), "steamapps");
                var ids = folder.subBlocks.FirstOrDefault()?.values;
                if (ids == null) continue;

                foreach ((string id, _) in ids)
                {
                    locations.Add((basePath, id));
                }
            }

            return locations;
        }
    }
}
