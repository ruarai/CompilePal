using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Media;

namespace CompilePalX.Compiling
{
    delegate Run LogWrite(string s, Brush b);
    delegate void LogBacktrack(List<Run> l);
    delegate void CompileErrorLogWrite(string errorText, Error e);

    delegate void CompileErrorFound(Error e);


    static class CompilePalLogger
    {
        private static readonly string logFile = "./debug.log";


        private static readonly Dictionary<Error, int> errorsFound = new Dictionary<Error, int>();

        private static StringBuilder lineBuffer = new StringBuilder();
        private static List<Run> tempText = new List<Run>();
        static CompilePalLogger()
        {
            File.Delete(logFile);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }
        public static event LogWrite OnWrite;
        public static event LogBacktrack OnBacktrack;
        public static event CompileErrorLogWrite OnErrorLog;

        public static event CompileErrorFound OnErrorFound;


        public static Run LogColor(string s, Brush b, params object[] formatStrings)
        {
            var text = s;
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

        public static void LogDebug(string s)
        {
            // log in debug, no op in release
#if DEBUG
            try
            {
                File.AppendAllText(logFile, s);
            } catch { }
#endif
        }

        public static void LogLineDebug(string s)
        {
            LogDebug(s + Environment.NewLine);
        }


        public static void LogCompileError(string errorText, Error e)
        {
            if (errorsFound.ContainsKey(e))
            {
                errorsFound[e]++;
            }
            else
            {
                errorsFound.Add(e, 1);
            }

            if (errorsFound[e] < 128)
            {
                OnErrorLog(errorText, e);
            }
            else
            {
                Log(errorText); //Stop hyperlinking errors if we see over 128 of them
            }

            File.AppendAllText(logFile, errorText);
            OnErrorFound(e);
        }
        public static void ProgressiveLog(string s)
        {
            lineBuffer.Append(s);

            if (s.Contains("\n"))
            {
                var lines = lineBuffer.ToString().Split('\n').ToList();

                var suffixText = lines.Last();

                lineBuffer = new StringBuilder(suffixText);

                OnBacktrack(tempText);

                for (var i = 0; i < lines.Count - 1; i++)
                {
                    var line = lines[i];
                    var error = ErrorFinder.GetError(line);

                    if (error == null)
                    {
                        Log(line);
                    }
                    else
                    {
                        LogCompileError(line, error);
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
