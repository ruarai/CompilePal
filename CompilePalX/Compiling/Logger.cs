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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CompilePalX.Compiling
{
    internal delegate void LogWrite(string s, Brush b);
    internal delegate void CompileErrorLogWrite(Hyperlink h);
    static class CompilePalLogger
    {
        private const string logFile = "debug.log";
        static CompilePalLogger()
        {
            File.Delete(logFile);
        }
        public static event LogWrite OnWrite;
        public static event CompileErrorLogWrite OnError;

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

        public static void LogCompileError(string errorText,Error e)
        {
            if (OnError == null)
                return;

            Hyperlink errorLink = new Hyperlink();

            Run text = new Run(errorText);

            text.Foreground = e.ErrorColor;

            errorLink.Inlines.Add(text);

            errorLink.TargetName = e.ID.ToString();

            errorLink.Click += errorLink_Click;

            OnError(errorLink);


            File.AppendAllText(logFile, errorText);
        }

        static void errorLink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var link = (Hyperlink) sender;
            int errorCode = int.Parse(link.TargetName);

            ErrorFinder.ShowErrorDialog(errorCode);
        }

    }
}
