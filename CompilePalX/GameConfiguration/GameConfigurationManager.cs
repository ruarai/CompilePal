using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace CompilePalX
{
    static class GameConfigurationManager
    {
        private static string? mapFile = null;
        public static GameConfiguration? GameConfiguration;
        public static GameConfiguration GameConfigurationBackup;
        public static List<GameConfiguration> GameConfigurations;
        private static string GameConfigurationFolder = "./GameConfiguration";
        private static readonly string GameConfigurationsPath = Path.Combine(GameConfigurationFolder, "gameConfigs.json");

        public static string SubstituteValues(string text, string mapFile = "", bool quote = true)
        {
            text = text.Replace("$vmfFile$", FormatValue(mapFile, quote));
            text = text.Replace("$map$", FormatValue(Path.GetFileNameWithoutExtension(mapFile), quote));
            text = text.Replace("$bsp$", FormatValue(Path.ChangeExtension(mapFile, "bsp"), quote));

            text = text.Replace("$mapCopyLocation$", FormatValue(Path.Combine(GameConfiguration.MapFolder, Path.ChangeExtension(Path.GetFileName(mapFile), "bsp")), quote));

            text = text.Replace("$game$", FormatValue(GameConfiguration.GameFolder, quote));
            text = text.Replace("$gameEXE$", FormatValue(GameConfiguration.GameEXE, quote));
            text = text.Replace("$binFolder$", FormatValue(GameConfiguration.BinFolder, quote));
            text = text.Replace("$mapFolder$", FormatValue(GameConfiguration.MapFolder, quote));
            text = text.Replace("$gameName$", FormatValue(GameConfiguration.Name, quote));
            text = text.Replace("$sdkFolder$", FormatValue(GameConfiguration.SDKMapFolder, quote));


            text = text.Replace("$vbsp$", FormatValue(GameConfiguration.VBSP, quote));
            text = text.Replace("$vvis$", FormatValue(GameConfiguration.VVIS, quote));
            text = text.Replace("$vrad$", FormatValue(GameConfiguration.VRAD, quote));


            text = text.Replace("$bspZip$", FormatValue(GameConfiguration.BSPZip, quote));
            text = text.Replace("$vbspInfo$", FormatValue(GameConfiguration.VBSPInfo, quote));


            return text;
        }

        private static string FormatValue(string text, bool quote)
        {
            if (quote)
                return $"\"{text}\"";
            return text;
        }

        /// <summary>
        /// Modifies the current context
        /// </summary>
        /// <param name="val">string in the form 'COMPILE_PAL_SET VARNAME VALUE'</param>
        public static void ModifyCurrentContext(string val)
        {
            val = val.Replace("COMPILE_PAL_SET ", "");
            val = Regex.Replace(val, @"\t|\r|\n", "");
            var firstSpace = val.IndexOf(" ");
            var field = val.Substring(0, firstSpace);
            var value = val.Substring(firstSpace + 1).Replace("\"", "");

            switch (field)
            {
                case "vbsp_exe":
                    GameConfiguration.VBSP = value;
                    break;
                case "vvis_exe":
                    GameConfiguration.VVIS = value;
                    break;
                case "vrad_exe":
                    GameConfiguration.VRAD = value;
                    break;
                case "game_exe":
                    GameConfiguration.GameEXE = value;
                    break;
                case "bspzip_exe":
                    GameConfiguration.BSPZip = value;
                    break;
                case "vpk_exe":
                    GameConfiguration.VPK = value;
                    break;
                case "vbspinfo_exe":
                    GameConfiguration.VBSPInfo = value;
                    break;
                case "bspdir":
                    GameConfiguration.MapFolder = value;
                    break;
                case "sdkbspdir":
                    GameConfiguration.SDKMapFolder = value;
                    break;
                case "bindir":
                    GameConfiguration.BinFolder = value;
                    break;
                case "gamedir":
                    GameConfiguration.GameFolder = value;
                    break;
                case "file":
                    mapFile = value;
                    break;
            }
        }

        public static void BackupCurrentContext()
        {
            GameConfigurationBackup = GameConfiguration;
        }

        public static void RestoreCurrentContext()
        {
            GameConfiguration = GameConfigurationBackup;
            mapFile = null;
        }

        public static CompileContext BuildContext(Map map)
        {
            return new CompileContext
            {
                Configuration = GameConfiguration,
                MapFile = GameConfigurationManager.mapFile ?? map.File,
                Map = map,
                BSPFile = Path.ChangeExtension(GameConfigurationManager.mapFile ?? map.File, "bsp"),
                CopyLocation = map.IsBSP ? map.File : Path.Combine(GameConfiguration.MapFolder, Path.ChangeExtension(Path.GetFileName(GameConfigurationManager.mapFile ?? map.File), "bsp"))
            };
        }

        public static void LoadGameConfigurations()
        {
            if (!Directory.Exists(GameConfigurationFolder))
                Directory.CreateDirectory(GameConfigurationFolder);

            //Loading the last used configurations for hammer
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Hammer\General");

            var configs = new List<GameConfiguration>();

            //try loading json
            if (File.Exists(GameConfigurationsPath))
            {
                string jsonLoadText = File.ReadAllText(GameConfigurationsPath);
                configs.AddRange(JsonConvert.DeserializeObject<List<GameConfiguration>>(jsonLoadText) ?? []);
            }

            //try loading from registry
            if (rk != null)
            {
                string binFolder = (string)rk.GetValue("Directory")!;

                try
                {
                    configs.AddRange(GameConfigurationParser.Parse(binFolder));
                }
                catch (Exception e)
                {
                    ExceptionHandler.LogException(e);
                }
            }

            // remove duplicates
            GameConfigurations = configs.GroupBy(g => (g.Name, g.GameFolder)).Select(grp => grp.First()).ToList();
            
            SaveGameConfigurations();
        }

        public static void SaveGameConfigurations()
        {
            string jsonSaveText = JsonConvert.SerializeObject(GameConfigurations, Formatting.Indented);
            File.WriteAllText(GameConfigurationsPath, jsonSaveText);
        }
    }
}
