using System;
using System.IO;

namespace CompilePalX
{
    public class GameConfiguration : ICloneable, IEquatable<GameConfiguration>
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

        public bool Equals(GameConfiguration? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _BSPZip == other._BSPZip && _VBSPInfo == other._VBSPInfo && _VPK == other._VPK && GameFolder == other.GameFolder && VBSP == other.VBSP && VVIS == other.VVIS && VRAD == other.VRAD && GameEXE == other.GameEXE && MapFolder == other.MapFolder && SDKMapFolder == other.SDKMapFolder && BinFolder == other.BinFolder && Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GameConfiguration)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_BSPZip);
            hashCode.Add(_VBSPInfo);
            hashCode.Add(_VPK);
            hashCode.Add(GameFolder);
            hashCode.Add(VBSP);
            hashCode.Add(VVIS);
            hashCode.Add(VRAD);
            hashCode.Add(GameEXE);
            hashCode.Add(MapFolder);
            hashCode.Add(SDKMapFolder);
            hashCode.Add(BinFolder);
            hashCode.Add(Name);
            return hashCode.ToHashCode();
        }
    }
}