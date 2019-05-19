using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilePalX
{
    static class GameConfigurationManager
    {
        public static GameConfiguration GameConfiguration;

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

        public static CompileContext BuildContext(string mapFile)
        {
            return new CompileContext
            {
                Configuration = GameConfiguration,
                MapFile = mapFile,
                BSPFile = Path.ChangeExtension(mapFile, "bsp"),
                CopyLocation = Path.Combine(GameConfiguration.MapFolder, Path.ChangeExtension(Path.GetFileName(mapFile), "bsp"))
            };
        }
    }
}
