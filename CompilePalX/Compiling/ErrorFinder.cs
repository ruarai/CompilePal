﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private static string errorURL = "https://www.interlopers.net/includes/errorpage/errorChecker.txt";

        private static Regex errorDescriptionPattern = new Regex("<h4>(.*?)</h4>");

        private static string errorStyle = Path.Combine("./Compiling", "errorstyle.html");
        private static string errorCache = Path.Combine("./Compiling", "errors.txt");
        public static void Init()
        {
            Thread t = new Thread(AsyncInit);
            t.Start();
        }

        static async void AsyncInit()
        {
            try
            {
                if (File.Exists(errorCache) && (DateTime.Now.Subtract(File.GetLastWriteTime(errorCache)).TotalDays < 7))
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
	                    string result = await c.GetStringAsync(new Uri(errorURL));

	                    LoadErrorData(result);
						File.WriteAllText(errorCache, result);
                    }
                    catch (Exception e)
                    {
						// fallback to cache if download fails
						ExceptionHandler.LogException(e, false);
						LoadErrorData(File.ReadAllText((errorCache)));
                    }

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
	                var err = error.Clone() as Error;
					// remove all control chars
	                err.ShortDescription = new string(line.Where(c => !char.IsControl(c)).ToArray());;
                    return err;
                }
            }
            return null;
        }

        public static void ShowErrorDialog(Error error)
        {
            ErrorWindow w = new ErrorWindow(error);
            w.ShowDialog();
        }
    }

    public class Error : ICloneable
    {
        public Regex RegexTrigger;
        public string Message;
        public string ShortDescription;
        public int Severity;

        public int ID;

        public Error() { }

        public Error(string message, string shortDescription, ErrorSeverity severity, int id = -1)
        {
            this.Message = message;
            this.ShortDescription = shortDescription;
            this.Severity = (int) severity;
            this.ID = id;
        }
        public Error(string message, ErrorSeverity severity, int id = -1)
        {
            this.Message = message;
            this.ShortDescription = message;
            this.Severity = (int) severity;
            this.ID = id;
        }

        public override bool Equals(object obj)
        {
            return ((Error)obj).ID == this.ID;
        }

        public override int GetHashCode()
        {
            return ID;//ID is unique between errors
        }

        public object Clone()
        {
	        return this.MemberwiseClone();
        }

        public Brush ErrorColor => GetSeverityBrush(Severity);

        public static Brush GetSeverityBrush(int severity)
        {
            switch (severity)
            {
                default:
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom("#0e5fc1")); // blue
                case 2:
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom("#e19520")); // yellow orange
                case 3:
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom("#ce4a08")); // orange
                case 4:
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom("#d93600")); // red
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

    public enum ErrorSeverity {
        Info = 1,
        Caution = 2,
        Warning = 3,
        Error = 4,
        FatalError = 5,
    }
}
