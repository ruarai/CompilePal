using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompilePalX.Compiling;
using CompilePalX.KV;

namespace CompilePalX
{
    class GameConfigurationParser
    {
        public static List<GameConfiguration> Parse(string binFolder)
        {
            // prioritize hammer++ configs, fallback to hammer if it doesn't exist
            var filename = Path.Combine(binFolder, "hammerplusplus", "hammerplusplus_gameconfig.txt");
            if (!File.Exists(filename))
            {
                filename = Path.Combine(binFolder, "GameConfig.txt");
            }

            var gameInfos = new List<GameConfiguration>();

            var data = new FileData(filename);
            foreach (var gamedb in data.headnode.GetFirstByName(new[]
                         {
                             "\"Configs\"", "\"GameConfig.txt\""
                         })
                         .GetFirstByName("\"Games\"").subBlocks)
            {
                var hdb = gamedb.GetFirstByName(new[]
                {
                    "\"Hammer\"", "\"hammer\""
                });

                CompilePalLogger.LogLineDebug($"Gamedb: {gamedb}");
                var game = new GameConfiguration
                {
                    Name = gamedb.name.Replace("\"", ""),
                    BinFolder = binFolder,
                    GameFolder = GetFullPath(gamedb.TryGetStringValue("GameDir"), binFolder),
                    GameEXE = GetFullPath(hdb.TryGetStringValue("GameExe"), binFolder),
                    SDKMapFolder = GetFullPath(hdb.TryGetStringValue("MapDir"), binFolder),
                    VBSP = GetFullPath(hdb.TryGetStringValue("BSP"), binFolder),
                    VVIS = GetFullPath(hdb.TryGetStringValue("Vis"), binFolder),
                    VRAD = GetFullPath(hdb.TryGetStringValue("Light"), binFolder),
                    MapFolder = GetFullPath(hdb.TryGetStringValue("BSPDir"), binFolder),
                    BSPZip = Path.Combine(binFolder, "bspzip.exe"),
                    VBSPInfo = Path.Combine(binFolder, "vbspinfo.exe"),
                    VPK = Path.Combine(binFolder, "vpk.exe")
                };

                game.SteamAppID = GetSteamAppID(game);

                gameInfos.Add(game);
            }

            return gameInfos;
        }

        private static string GetFullPath(string line, string gameInfoDir)
        {
            if (!line.StartsWith("..") || !line.StartsWith(""))
            {
                return line;
            }

            var fullPath = Path.GetFullPath(Path.Combine(gameInfoDir, line));
            return fullPath;
        }

        private static int? GetSteamAppID(GameConfiguration config)
        {
            if (!File.Exists(config.GameInfoPath))
            {
                return null;
            }

            foreach (var line in File.ReadLines(config.GameInfoPath))
            {
                // ignore commented out lines
                if (line.TrimStart().StartsWith("//") || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!line.Contains("SteamAppId"))
                {
                    continue;
                }

                // sometimes gameinfo contains tabs, replace with spaces and filter them out
                var splitLine = line.Replace('\t', ' ').Split(' ').Where(c => c != string.Empty).ToList();

                // bad format
                if (splitLine.Count < 2)
                {
                    continue;
                }

                int.TryParse(splitLine[1], out var appID);
                return appID;
            }

            return null;
        }
    }
}
