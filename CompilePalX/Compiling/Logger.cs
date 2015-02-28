using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilePalX.Compiling
{
    internal delegate void LogWrite(string s);
    static class CompilePalLogger
    {
        private const string logFile = "debug.log";
        static CompilePalLogger()
        {
            File.Delete(logFile);
        }
        public static event LogWrite OnWrite;

        public static void Log(string s = "",params object[] formatStrings)
        {
            string text = string.Format(s, formatStrings);

            if (OnWrite != null)
                OnWrite(text);

            File.AppendAllText(logFile, text);
        }
        public static void LogLine(string s = "", params object[] formatStrings)
        {
            Log(s + Environment.NewLine,formatStrings);
        }
    }
}
