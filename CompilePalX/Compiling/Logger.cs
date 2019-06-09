using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CompilePalX.Compiling
{
    internal delegate Run LogWrite(string s, Brush b);
    internal delegate void LogBacktrack(List<Run> l);
    internal delegate void CompileErrorLogWrite(string errorText, Error e);

    internal delegate void CompileErrorFound(Error e);


    static class CompilePalLogger
    {
        private static readonly string logFile = CompilePalPath.Directory + "debug.log";
        static CompilePalLogger()
        {
            File.Delete(logFile);
        }
        public static event LogWrite OnWrite;
        public static event LogBacktrack OnBacktrack;
        public static event CompileErrorLogWrite OnErrorLog;

        public static event CompileErrorFound OnErrorFound;


        public static Run LogColor(string s, Brush b, params object[] formatStrings)
        {
            string text = s;
            if (formatStrings.Length != 0)
            {
                text = string.Format(s, formatStrings);
            }

            try
            {
                File.AppendAllText(logFile, text);

            }
            catch { }

            return OnWrite?.Invoke(text, b);
        }


        public static Run LogLineColor(string s, Brush b, params object[] formatStrings)
        {
            return LogColor(s + Environment.NewLine, b, formatStrings);
        }

        public static Run Log(string s = "", params object[] formatStrings)
        {
            return LogColor(s, null, formatStrings);
        }

        public static Run LogLine(string s = "", params object[] formatStrings)
        {
            return Log(s + Environment.NewLine, formatStrings);
        }


        public static void LogCompileError(string errorText, Error e)
        {
            OnErrorLog(errorText, e);

            File.AppendAllText(logFile, errorText);
        }


        private static Dictionary<Error, int> errorsFound = new Dictionary<Error, int>();

        private static StringBuilder lineBuffer = new StringBuilder();
        private static List<Run> tempText = new List<Run>();
        public static void ProgressiveLog(string s)
        {
            lineBuffer.Append(s);

            if (s.Contains("\n"))
            {
                List<string> lines = lineBuffer.ToString().Split('\n').ToList();

                string suffixText = lines.Last();

                lineBuffer = new StringBuilder(suffixText);
                
                OnBacktrack(tempText);

                for (var i = 0; i < lines.Count - 1; i++)
                {
                    var line = lines[i];
                    var error = ErrorFinder.GetError(line);

                    if (error == null)
                        Log(line);
                    else
                    {
                        if (errorsFound.ContainsKey(error))
                            errorsFound[error]++;
                        else
                            errorsFound.Add(error, 1);

                        if (errorsFound[error] < 128)
                            LogCompileError(line, error);
                        else
                            Log(line);//Stop hyperlinking errors if we see over 128 of them
                        
                        OnErrorFound(error);
                    }
                }

                if (suffixText.Length > 0)
                {
                    tempText = new List<Run>();
                    tempText.Add(Log(suffixText));
                }
            }
            else
            {
                tempText.Add(Log(s));
            }
        }

    }
}
