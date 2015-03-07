using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompilePalX.Compiling
{
    internal delegate void LogWrite(string s,Brush b);
    static class CompilePalLogger
    {
        private const string logFile = "debug.log";
        static CompilePalLogger()
        {
            File.Delete(logFile);
        }
        public static event LogWrite OnWrite;

        public static void LogColor(string s, Brush b, params object[] formatStrings)
        {
            string text = string.Format(s, formatStrings);

            if (OnWrite != null)
                OnWrite(text,b);

            File.AppendAllText(logFile, text);
        }


        public static void LogLineColor(string s, Brush b, params object[] formatStrings)
        {
            LogColor(s + Environment.NewLine, b, formatStrings);
        }

        public static void Log(string s = "",params object[] formatStrings)
        {
            LogColor(s,null,formatStrings);
        }

        public static void LogLine(string s = "", params object[] formatStrings)
        {
            Log(s + Environment.NewLine,formatStrings);
        }

    }
}
