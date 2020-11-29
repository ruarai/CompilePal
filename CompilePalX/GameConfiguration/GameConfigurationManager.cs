using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CompilePalX
{
    static class GameConfigurationManager
    {
        private static string mapFile = null;
        public static GameConfiguration GameConfiguration;
        public static GameConfiguration GameConfigurationBackup;
        public static List<GameConfiguration> GameConfigurations;

        public static string SubstituteValues(string text, string mapFile = "")
        {
            text = text.Replace("$vmfFile$", $"\"{mapFile}\"");
            text = text.Replace("$map$", $"\"{Path.GetFileNameWithoutExtension(mapFile)}\"");
            text = text.Replace("$bsp$", $"\"{Path.ChangeExtension(mapFile, "bsp")}\"");

            text = text.Replace("$mapCopyLocation$",
	            $"\"{Path.Combine(GameConfiguration.MapFolder, Path.ChangeExtension(Path.GetFileName(mapFile), "bsp"))}\"");

            text = text.Replace("$game$", $"\"{GameConfiguration.GameFolder}\"");
            text = text.Replace("$gameEXE$", $"\"{GameConfiguration.GameEXE}\"");
            text = text.Replace("$binFolder$", $"\"{GameConfiguration.BinFolder}\"");
            text = text.Replace("$mapFolder$", $"\"{GameConfiguration.MapFolder}\"");
            text = text.Replace("$gameName$", $"\"{GameConfiguration.Name}\"");
            text = text.Replace("$sdkFolder$", $"\"{GameConfiguration.SDKMapFolder}\"");


            text = text.Replace("$vbsp$", $"\"{GameConfiguration.VBSP}\"");
            text = text.Replace("$vvis$", $"\"{GameConfiguration.VVIS}\"");
            text = text.Replace("$vrad$", $"\"{GameConfiguration.VRAD}\"");


            text = text.Replace("$bspZip$", $"\"{GameConfiguration.BSPZip}\"");
            text = text.Replace("$vbspInfo$", $"\"{GameConfiguration.VBSPInfo}\"");


            return text;
        }

        /// <summary>
        /// Modifies the current context
        /// </summary>
        /// <param name="val">string in the form 'COMPILE_PAL_SET VARNAME VALUE'</param>
        public static void ModifyCurrentContext(string val)
        {
            val = val.Replace("COMPILE_PAL_SET ", "");
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

        public static CompileContext BuildContext(string mapFile)
        {
            return new CompileContext
            {
                Configuration = GameConfiguration,
                MapFile = GameConfigurationManager.mapFile ?? mapFile,
                BSPFile = Path.ChangeExtension(GameConfigurationManager.mapFile ?? mapFile, "bsp"),
                CopyLocation = Path.Combine(GameConfiguration.MapFolder, Path.ChangeExtension(Path.GetFileName(GameConfigurationManager.mapFile ?? mapFile), "bsp"))
            };
        }
    }
}
