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
        public string BSPZip { get; set; }
        public string VBSPInfo { get; set; }
        public string VPK { get; set; }
        public string GameEXE { get; set; }
        public string MapFolder { get; set; }
        public string SDKMapFolder { get; set; }
        public string BinFolder { get; set; }
        public string Name { get; set; }
        public string GameInfoPath => Path.Combine(GameFolder ?? "", "gameinfo.txt");
        public int? SteamAppID { get; set; }
        public string? PluginFolder { get; set; }
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public bool Equals(GameConfiguration? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return BSPZip == other.BSPZip && VBSPInfo == other.VBSPInfo && VPK == other.VPK && GameFolder == other.GameFolder && VBSP == other.VBSP && VVIS == other.VVIS && VRAD == other.VRAD && GameEXE == other.GameEXE && MapFolder == other.MapFolder && SDKMapFolder == other.SDKMapFolder && BinFolder == other.BinFolder && Name == other.Name && SteamAppID == other.SteamAppID;
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
            hashCode.Add(BSPZip);
            hashCode.Add(VBSPInfo);
            hashCode.Add(VPK);
            hashCode.Add(GameFolder);
            hashCode.Add(VBSP);
            hashCode.Add(VVIS);
            hashCode.Add(VRAD);
            hashCode.Add(GameEXE);
            hashCode.Add(MapFolder);
            hashCode.Add(SDKMapFolder);
            hashCode.Add(BinFolder);
            hashCode.Add(Name);
            hashCode.Add(SteamAppID);
            return hashCode.ToHashCode();
        }
    }
}
