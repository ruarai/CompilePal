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
        public static List<GameConfiguration> Parse(string binFolder)
        {
            // prioritize hammer++ configs, fallback to hammer if it doesn't exist
            string filename = Path.Combine(binFolder, "hammerplusplus", "hammerplusplus_gameconfig.txt");
            if (!File.Exists(filename))
                filename = Path.Combine(binFolder, "GameConfig.txt");

            var gameInfos = new List<GameConfiguration>();

            var data = new KV.FileData(filename);
            foreach (KV.DataBlock gamedb in data.headnode.GetFirstByName(new[] { "\"Configs\"", "\"GameConfig.txt\"", "\"hammerplusplus\\hammerplusplus_gameconfig.txt\"" })
                         .GetFirstByName("\"Games\"").subBlocks)
            {
                KV.DataBlock hdb = gamedb.GetFirstByName(new[] { "\"Hammer\"", "\"hammer\"" });

                CompilePalLogger.LogLineDebug($"Gamedb: {gamedb}");

                // use vbsp as a backup path for finding other compile executables if they are in a non standard location
                var vbsp = GetFullPath(hdb.TryGetStringValue("BSP"), binFolder);
                var vbspPath = Path.GetDirectoryName(vbsp);

                var bspzip = FindPath("bspzip.exe", binFolder, vbspPath);
                var vbspinfo = FindPath("vbspinfo.exe", binFolder, vbspPath);
                var vpk = FindPath("vpk.exe", binFolder, vbspPath);

                if (Path.GetDirectoryName(bspzip) != binFolder)
                {
                    CompilePalLogger.LogLineDebug($"Bin folder \"{binFolder}\" differs from compiler location \"{Path.GetDirectoryName(bspzip)}\"");
                    binFolder = Path.GetDirectoryName(bspzip);
                }

                GameConfiguration game = new GameConfiguration
                {
                    Name = gamedb.name.Replace("\"", ""),
                    BinFolder = binFolder,
                    GameFolder = GetFullPath(gamedb.TryGetStringValue("GameDir"), binFolder),
                    GameEXE = GetFullPath(hdb.TryGetStringValue("GameExe"), binFolder),
                    SDKMapFolder = GetFullPath(hdb.TryGetStringValue("MapDir"), binFolder),
                    VBSP = vbsp,
                    VVIS = GetFullPath(hdb.TryGetStringValue("Vis"), binFolder),
                    VRAD = GetFullPath(hdb.TryGetStringValue("Light"), binFolder),
                    MapFolder = GetFullPath(hdb.TryGetStringValue("BSPDir"), binFolder),
                    BSPZip = bspzip,
                    VBSPInfo = vbspinfo,
                    VPK = vpk,
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

        
        private static string? FindPath(string program, string binFolder, string backupBinFolder)
        {
            var path = Path.Combine(binFolder, program);
            if (File.Exists(path))
            {
                return path;
            }

            // program does not exist in standard bin folder, fallback to trying to locate it by using a known executable
            CompilePalLogger.LogLineDebug($"{program} does not exist at \"{path}\", using known compiler location {backupBinFolder}");

            path = Path.Combine(backupBinFolder, program);
            return File.Exists(path) ? path : null;

        }
    }
}
