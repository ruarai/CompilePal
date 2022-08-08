using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CompilePalX.Compiling;

namespace CompilePalX {
    class GameConfigurationParser {
        public static List<GameConfiguration> Parse(string filename) {
            var lines = File.ReadAllLines(filename);
            var gameInfos = new List<GameConfiguration>();

            var data = new KV.FileData(filename);

            foreach (KV.DataBlock gamedb in data.headnode.GetFirstByName(new []{"\"Configs\"", "\"GameConfig.txt\""}).GetFirstByName("\"Games\"").subBlocks) {
                string binfolder = Path.GetDirectoryName(filename);
                KV.DataBlock hdb = gamedb.GetFirstByName(new []{"\"Hammer\"", "\"hammer\""});

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

                gameInfos.Add(game);
            }

            return gameInfos;
        }

        private static string GetFullPath(string line, string gameInfoDir) {
            if (!line.StartsWith("..") || !line.StartsWith(""))
                return line;

            string fullPath = Path.GetFullPath(Path.Combine(gameInfoDir, line));
            return fullPath;
        }
    }
}