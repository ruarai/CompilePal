using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media;
using CompilePalX.Compiling;

namespace CompilePalX
{
    static class ErrorFinder
    {
        private static List<Error> errorList = new List<Error>();

        //interlopers list of errors
        private static string errorURL = "http://www.interlopers.net/includes/errorpage/errorChecker.txt";
        public static void Init()
        {
            WebClient c = new WebClient();

            c.DownloadStringCompleted += c_DownloadStringCompleted;

            c.DownloadStringAsync(new Uri(errorURL));
        }

        static void c_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                try
                {
                    var lines = e.Result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                    int count = int.Parse(lines[0]);

                    for (int i = 1; i < (count * 2) + 1; i++)
                    {
                        Error error = new Error();

                        var data = lines[i].Split('|');

                        error.Severity = int.Parse(data[0]);
                        error.RegexTrigger = new Regex(data[1]);
                        i++;
                        error.Message = lines[i];

                        //CompilePalLogger.LogLineColor("Loaded trigger regex: {0}",error.ErrorColor,data[1]);


                        errorList.Add(error);
                    }
                }
                catch (Exception x)
                {
                    ExceptionHandler.LogException(x);
                }

            }
            else
            {
                CompilePalLogger.LogColor("Failed to download error list from interlopers.net.", Brushes.Red);
            }
        }

        public static Error GetError(string line)
        {
            foreach (var error in errorList)
            {
                if (error.RegexTrigger.IsMatch(line))
                {
                    return error;
                }
            }
            return null;
        }
    }

    class Error
    {
        public Regex RegexTrigger;
        public string Message;
        public int Severity;

        public Brush ErrorColor
        {
            get
            {
                switch (Severity)
                {
                    default:
                        return Brushes.Black;
                    case 2:
                        return Brushes.DimGray;
                    case 3:
                        return Brushes.Orange;
                    case 4:
                        return Brushes.DarkRed;
                    case 5:
                        return Brushes.Red;
                }
            }
        }
    }
}
