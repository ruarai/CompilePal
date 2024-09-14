using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CompilePalX.Compiling;
using ValveKeyValue;

namespace CompilePalX {
    class GameConfigurationParser
    {
        private static KVSerializer KVSerializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
        public static List<GameConfiguration> Parse(string binFolder)
        {
            // prioritize hammer++ configs, fallback to hammer if it doesn't exist
            string filename = Path.Combine(binFolder, "hammerplusplus", "hammerplusplus_gameconfig.txt");
            if (!File.Exists(filename))
                filename = Path.Combine(binFolder, "GameConfig.txt");

            var gameInfos = new List<GameConfiguration>();

            CompilePalLogger.LogLineDebug($"Reading Game Config: {filename}");
            using (var gameConfigFile = File.OpenRead(filename))
            {
                var data = KVSerializer.Deserialize(gameConfigFile);

                foreach (var gamedb in (IEnumerable<KVObject>)data["Games"])
                {
                    var hdb = gamedb["Hammer"];
                    if (hdb is null)
                    {
                        CompilePalLogger.LogLineDebug($"GameInfo block is missing Hammer section: {gamedb}");
                        continue;
                    }

                    CompilePalLogger.LogLineDebug($"Gamedb: {gamedb}");

                    // use vbsp as a backup path for finding other compile executables if they are in a non standard location
                    var vbsp = GetFullPath(hdb["BSP"].ToString(), binFolder);
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
                        Name = gamedb.Name.Replace("\"", ""),
                        BinFolder = binFolder,
                        GameFolder = GetFullPath(gamedb["GameDir"].ToString(), binFolder),
                        GameEXE = GetFullPath(hdb["GameExe"].ToString(), binFolder),
                        SDKMapFolder = GetFullPath(hdb["MapDir"].ToString(), binFolder),
                        VBSP = vbsp,
                        VVIS = GetFullPath(hdb["Vis"].ToString(), binFolder),
                        VRAD = GetFullPath(hdb["Light"].ToString(), binFolder),
                        MapFolder = GetFullPath(hdb["BSPDir"].ToString(), binFolder),
                        BSPZip = bspzip,
                        VBSPInfo = vbspinfo,
                        VPK = vpk,
                    };

                    var cpdb = gamedb["CompilePal"];
                    if (cpdb is not null)
                    {
                        CompilePalLogger.LogLineDebug($"Found CompilePal GameInfo block");
                        var pluginFolder = cpdb["Plugins"].ToString();
                        if (!string.IsNullOrEmpty(pluginFolder))
                        {
                            game.PluginFolder = pluginFolder;
                        }
                    }

                    game.SteamAppID = GetSteamAppID(game);
                    gameInfos.Add(game);
                }
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

            using (var gameInfoFile = File.OpenRead(config.GameInfoPath))
            {
                var gameInfo = KVSerializer.Deserialize(gameInfoFile);
                var appIDValue = gameInfo["FileSystem"]?["SteamAppId"];
                if (appIDValue is null)
                    return null;

                Int32.TryParse(appIDValue.ToString(), out int appID);
                return appID;
            }
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
