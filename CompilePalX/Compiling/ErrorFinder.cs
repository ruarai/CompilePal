using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

        private static Regex errorDescriptionPattern = new Regex("<h4>(.*?)</h4>");

        private static string errorStyle = Path.Combine("Compiling", "errorstyle.html");
        private static string errorCache = Path.Combine("Compiling", "errors.txt");
        public static void Init()
        {
            Thread t = new Thread(AsyncInit);
            t.Start();
        }

        static void AsyncInit()
        {
            try
            {
                if (File.Exists(errorCache) && (DateTime.Now.Subtract(File.GetLastWriteTime(errorCache)).TotalDays < 7))
                {
                    LoadErrorData(File.ReadAllText(errorCache));
                }
                else
                {
                    WebClient c = new WebClient();

                    string result = c.DownloadString(new Uri(errorURL));

                    LoadErrorData(result);

                    File.WriteAllText(errorCache, result);
                }
            }
            catch (Exception x)
            {
                //nonvital part, record but dont quit
                ExceptionHandler.LogException(x, false);
            }
        }

        static void LoadErrorData(string input)
        {
            string style = File.ReadAllText(errorStyle);

            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            int count = int.Parse(lines[0]);

            int id = 0;
            for (int i = 1; i < (count * 2) + 1; i++)
            {
                Error error = new Error();

                var data = lines[i].Split('|');

                error.Severity = int.Parse(data[0]);
                error.RegexTrigger = new Regex(data[1]);
                i++;

                var shortDesc = errorDescriptionPattern.Match(lines[i]);
                error.ShortDescription = shortDesc.Success ? shortDesc.Groups[1].Value : "unknown error";

                error.Message = style.Replace("%content%", lines[i]);

                //CompilePalLogger.LogLineColor("Loaded trigger regex: {0}",error.ErrorColor,data[1]);


                error.ID = id;
                errorList.Add(error);
                id++;
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

        public static void ShowErrorDialog(int errorID)
        {
            var error = errorList.FirstOrDefault(e => e.ID == errorID);

            if (error != null)
            {
                ErrorWindow w = new ErrorWindow(error.Message);
                w.ShowDialog();
            }
        }
    }

    class Error
    {
        public Regex RegexTrigger;
        public string Message;
        public string ShortDescription;
        public int Severity;

        public int ID;

        public override bool Equals(object obj)
        {
            return ((Error)obj).ID == this.ID;
        }

        public override int GetHashCode()
        {
            return ID;//ID is unique between errors
        }

        public Brush ErrorColor => GetSeverityBrush(Severity);

        public static Brush GetSeverityBrush(int severity)
        {
            switch (severity)
            {
                default:
                    return Brushes.Black;
                case 2:
                    return Brushes.Orange;
                case 3:
                    return Brushes.OrangeRed;
                case 4:
                    return Brushes.DarkRed;
                case 5:
                    return Brushes.Red;
            }
        }

        public string SeverityText
        {
            get
            {
                switch (Severity)
                {
                    default:
                        return "Info";
                    case 2:
                        return "Caution";
                    case 3:
                        return "Warning";
                    case 4:
                        return "Error";
                    case 5:
                        return "Fatal Error";
                }
            }
        }
    }
}
