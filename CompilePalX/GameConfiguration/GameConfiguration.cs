using System;
using System.IO;

namespace CompilePalX
{
    public class GameConfiguration : ICloneable
    {
        public string GameFolder { get; set; }

        public string VBSP { get; set; }
        public string VVIS { get; set; }
        public string VRAD { get; set; }

        private string? _BSPZip = null;
        public string BSPZip
        {
            get => _BSPZip ?? (BinFolder != null ? Path.Combine(BinFolder, "bspzip.exe") : "");
            set => _BSPZip = value;
        }

        private string? _VBSPInfo = null;
        public string VBSPInfo {
            get => _VBSPInfo ?? (BinFolder != null ? Path.Combine(BinFolder, "vbspinfo.exe") : "");
            set => _VBSPInfo = value;
        }
        private string? _VPK = null;
        public string VPK {
            get => _VPK ?? (BinFolder != null ? Path.Combine(BinFolder, "vpk.exe") : "");
            set => _VPK = value;
        }

        public string GameEXE { get; set; }

        public string MapFolder { get; set; }
        public string SDKMapFolder { get; set; }

        public string? BinFolder { get; set; }

        public string Name { get; set; }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}