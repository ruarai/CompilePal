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
            text = text.Replace("$vmfFile$", string.Format("\"{0}\"", mapFile));
            text = text.Replace("$map$", string.Format("\"{0}\"", Path.GetFileNameWithoutExtension(mapFile)));
            text = text.Replace("$bsp$", string.Format("\"{0}\"", Path.ChangeExtension(mapFile, "bsp")));

            text = text.Replace("$mapCopyLocation$", string.Format("\"{0}\"", Path.Combine(GameConfiguration.MapFolder, Path.ChangeExtension(Path.GetFileName(mapFile), "bsp"))));

            text = text.Replace("$game$", string.Format("\"{0}\"", GameConfiguration.GameFolder));
            text = text.Replace("$gameEXE$", string.Format("\"{0}\"", GameConfiguration.GameEXE));
            text = text.Replace("$binFolder$", string.Format("\"{0}\"", GameConfiguration.BinFolder));
            text = text.Replace("$mapFolder$", string.Format("\"{0}\"", GameConfiguration.MapFolder));
            text = text.Replace("$gameName$", string.Format("\"{0}\"", GameConfiguration.Name));
            text = text.Replace("$sdkFolder$", string.Format("\"{0}\"", GameConfiguration.SDKMapFolder));


            text = text.Replace("$vbsp$", string.Format("\"{0}\"", GameConfiguration.VBSP));
            text = text.Replace("$vvis$", string.Format("\"{0}\"", GameConfiguration.VVIS));
            text = text.Replace("$vrad$", string.Format("\"{0}\"", GameConfiguration.VRAD));


            text = text.Replace("$bspZip$", string.Format("\"{0}\"", GameConfiguration.BSPZip));
            text = text.Replace("$vbspInfo$", string.Format("\"{0}\"", GameConfiguration.VBSPInfo));


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
