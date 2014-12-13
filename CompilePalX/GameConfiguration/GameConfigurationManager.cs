using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilePalX
{
    static class GameConfigurationManager
    {
        public static GameConfiguration GameConfiguration;

        public static string SubstituteValues(string text)
        {
            text = text.Replace("%GameFolder%", GameConfiguration.GameFolder);

            text = text.Replace("%VBSP%", GameConfiguration.VBSP);
            text = text.Replace("%VVIS%", GameConfiguration.VVIS);
            text = text.Replace("%VRAD%", GameConfiguration.VRAD);

            text = text.Replace("%GameEXE%", GameConfiguration.GameEXE);

            text = text.Replace("%MapFolder%", GameConfiguration.MapFolder);

            text = text.Replace("%BinFolder%", GameConfiguration.BinFolder);

            return text;
        }
    }
}
