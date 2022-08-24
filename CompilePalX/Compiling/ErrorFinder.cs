using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using CompilePalX.Compiling;

namespace CompilePalX
{
    static class ErrorFinder
    {
        private static readonly List<Error> errorList = new List<Error>();

        //interlopers list of errors
        private static readonly string errorURL = "https://www.interlopers.net/includes/errorpage/errorChecker.txt";

        private static readonly Regex errorDescriptionPattern = new Regex("<h4>(.*?)</h4>");

        private static readonly string errorStyle = Path.Combine("./Compiling", "errorstyle.html");
        private static readonly string errorCache = Path.Combine("./Compiling", "errors.txt");
        public static void Init()
        {
            var t = new Thread(AsyncInit);
            t.Start();
        }

        private static async void AsyncInit()
        {
            try
            {
                if (File.Exists(errorCache) && DateTime.Now.Subtract(File.GetLastWriteTime(errorCache)).TotalDays < 7)
                {
                    LoadErrorData(File.ReadAllText(errorCache));
                }
                else
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    try
                    {
                        var c = new HttpClient();
                        var result = await c.GetStringAsync(new Uri(errorURL));

                        LoadErrorData(result);
                        File.WriteAllText(errorCache, result);
                    }
                    catch (Exception e)
                    {
                        // fallback to cache if download fails
                        ExceptionHandler.LogException(e, false);
                        LoadErrorData(File.ReadAllText(errorCache));
                    }

                }
            }
            catch (Exception x)
            {
                //nonvital part, record but dont quit
                ExceptionHandler.LogException(x, false);
            }
        }

        private static void LoadErrorData(string input)
        {
            var style = File.ReadAllText(errorStyle);

            var lines = input.Split(new[]
            {
                "\r\n", "\n"
            }, StringSplitOptions.None);

            var count = int.Parse(lines[0]);

            var id = 0;
            for (var i = 1; i < count * 2 + 1; i++)
            {
                var error = new Error();

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
                    var err = error.Clone() as Error;
                    // remove all control chars
                    err.ShortDescription = new string(line.Where(c => !char.IsControl(c)).ToArray());
                    ;
                    return err;
                }
            }
            return null;
        }

        public static void ShowErrorDialog(Error error)
        {
            var w = new ErrorWindow(error);
            w.ShowDialog();
        }
    }

    public class Error : ICloneable
    {

        public int ID;
        public string Message;
        public Regex RegexTrigger;
        public int Severity;
        public string ShortDescription;

        public Error() { }

        public Error(string message, string shortDescription, ErrorSeverity severity, int id = -1)
        {
            Message = message;
            ShortDescription = shortDescription;
            Severity = (int)severity;
            ID = id;
        }
        public Error(string message, ErrorSeverity severity, int id = -1)
        {
            Message = message;
            ShortDescription = message;
            Severity = (int)severity;
            ID = id;
        }

        public Brush ErrorColor => GetSeverityBrush(Severity);

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

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override bool Equals(object obj)
        {
            return ((Error)obj).ID == ID;
        }

        public override int GetHashCode()
        {
            return ID; //ID is unique between errors
        }

        public static Brush GetSeverityBrush(int severity)
        {
            switch (severity)
            {
                default:
                    return (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Severity1");
                case 2:
                    return (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Severity2");
                case 3:
                    return (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Severity3");
                case 4:
                    return (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Severity4");
                case 5:
                    return (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Severity5");
            }
        }
    }

    public enum ErrorSeverity
    {
        Info = 1,
        Caution = 2,
        Warning = 3,
        Error = 4,
        FatalError = 5
    }
}
