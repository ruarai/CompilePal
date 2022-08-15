using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CompilePalX.Compiling;

namespace CompilePalX {
    class GameConfigurationParser
    {
        public static List<GameConfiguration> Parse(string filename)
        {
            var lines = File.ReadAllLines(filename);
            var gameInfos = new List<GameConfiguration>();

            var data = new KV.FileData(filename);

            foreach (KV.DataBlock gamedb in data.headnode.GetFirstByName(new[] { "\"Configs\"", "\"GameConfig.txt\"" })
                         .GetFirstByName("\"Games\"").subBlocks)
            {
                string binfolder = Path.GetDirectoryName(filename);
                KV.DataBlock hdb = gamedb.GetFirstByName(new[] { "\"Hammer\"", "\"hammer\"" });

                CompilePalLogger.LogLine($"Gamedb: {gamedb}");
                GameConfiguration game = new GameConfiguration
                {
                    Name = gamedb.name.Replace("\"", ""),
                    BinFolder = binfolder,
                    GameFolder = GetFullPath(gamedb.TryGetStringValue("GameDir"), binfolder),
                    GameEXE = GetFullPath(hdb.TryGetStringValue("GameExe"), binfolder),
                    SDKMapFolder = GetFullPath(hdb.TryGetStringValue("MapDir"), binfolder),
                    VBSP = GetFullPath(hdb.TryGetStringValue("BSP"), binfolder),
                    VVIS = GetFullPath(hdb.TryGetStringValue("Vis"), binfolder),
                    VRAD = GetFullPath(hdb.TryGetStringValue("Light"), binfolder),
                    MapFolder = GetFullPath(hdb.TryGetStringValue("BSPDir"), binfolder),
                    BSPZip = Path.Combine(binfolder, "bspzip.exe"),
                    VBSPInfo = Path.Combine(binfolder, "vbspinfo.exe"),
                    VPK = Path.Combine(binfolder, "vpk.exe"),
                };

                game.SteamAppID = GetSteamAppID(game);

                gameInfos.Add(game);
            }

            return gameInfos;
        }

        private static string GetFullPath(string line, string gameInfoDir)
        {
            if (!line.StartsWith("..") || !line.StartsWith(""))
                return line;

            string fullPath = Path.GetFullPath(Path.Combine(gameInfoDir, line));
            return fullPath;
        }

        private static int? GetSteamAppID(GameConfiguration config)
        {
            if (!File.Exists(config.GameInfoPath)) return null;

            foreach (var line in File.ReadLines(config.GameInfoPath))
            {
                // ignore commented out lines
                if (line.TrimStart().StartsWith("//") || string.IsNullOrWhiteSpace(line))
                    continue;

                if (!line.Contains("SteamAppId")) continue;

                // sometimes gameinfo contains tabs, replace with spaces and filter them out
                var splitLine = line.Replace('\t', ' ').Split(' ').Where(c => c != String.Empty).ToList();

                // bad format
                if (splitLine.Count < 2)
                    continue;

                Int32.TryParse(splitLine[1], out int appID);
                return appID;
            }

            return null;
        }
    }
}