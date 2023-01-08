using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilePalX.Utilities
{
    internal class AppManifestParser
    {
        private KV.DataBlock head;
        public AppManifestParser(string appManifestPath)
        {
            this.head = new KV.FileData(appManifestPath).headnode;
        }

        public string? GetInstallationDirectory()
        {
            return head.GetFirstByName("\"AppState\"")?.TryGetStringValue("installdir");
        }
    }
}
