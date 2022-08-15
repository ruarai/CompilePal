using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilePalX
{
    public class ConfigItem : ICloneable
    {
        public string Name { get; set; }
        public string Parameter { get; set; }
        public string Description { get; set; }

        public string Value { get; set; }
        public bool ValueIsFile { get; set; }
        public bool ValueIsFolder { get; set; }
        public string Value2 { get; set; }
		public bool Value2IsFile { get; set; }
		public bool Value2IsFolder { get; set; }

		public bool ReadOutput { get; set; }

		public bool WaitForExit { get; set; }

		public bool CanHaveValue { get; set; }

        public string Warning { get; set; }

        public bool CanBeUsedMoreThanOnce { get; set; }
        public HashSet<int>? IncompatibleGames { get; set; }
        public HashSet<int>? CompatibleGames { get; set; }

        public bool IsCompatible
        {
            get
            {
                // current game configuration has no SteamAppID
                if (GameConfigurationManager.GameConfiguration != null && GameConfigurationManager.GameConfiguration.SteamAppID == null)
                    return true;

                int currentAppID = (int)GameConfigurationManager.GameConfiguration!.SteamAppID!;

                // supported game ID list should take precedence. If defined, check that current GameConfiguration SteamID is in whitelist
                if (CompatibleGames != null)
                    return CompatibleGames.Contains(currentAppID);

                // If defined, check that current GameConfiguration SteamID is not in blacklist
                if (IncompatibleGames != null)
                    return !IncompatibleGames.Contains(currentAppID);

                // parameter does not define which games are supported
                return true;
            }
        }

        public object Clone()
        {
            return new ConfigItem() {Name=Name,Parameter=Parameter,Description = Description,Value=Value, Value2 = Value2, CanHaveValue = CanHaveValue,Warning = Warning,CanBeUsedMoreThanOnce = CanBeUsedMoreThanOnce, ReadOutput = ReadOutput, ValueIsFile = ValueIsFile, Value2IsFile = Value2IsFile, ValueIsFolder = ValueIsFolder, Value2IsFolder = Value2IsFolder, WaitForExit = WaitForExit, CompatibleGames = CompatibleGames, IncompatibleGames = IncompatibleGames};
        }
    }
}
