using System.IO;

namespace CompilePalX
{
    class GameConfiguration
    {
        public string GameFolder { get; set; }

        public string VBSP { get; set; }
        public string VVIS { get; set; }
        public string VRAD { get; set; }

        private string _BSPZip = null;
        public string BSPZip
        {
            get => _BSPZip ?? Path.Combine(BinFolder, "bspzip.exe");
            set => _BSPZip = value;
        }

        private string _VBSPInfo = null;
        public string VBSPInfo {
            get => _VBSPInfo ?? Path.Combine(BinFolder, "vbspinfo.exe");
            set => _VBSPInfo = value;
        }
        private string _VPK = null;
        public string VPK {
            get => _VPK ?? Path.Combine(BinFolder, "vpk.exe");
            set => _VPK = value;
        }

        public string GameEXE { get; set; }

        public string MapFolder { get; set; }
        public string SDKMapFolder { get; set; }

        public string BinFolder { get; set; }

        public string Name { get; set; }
    }
}