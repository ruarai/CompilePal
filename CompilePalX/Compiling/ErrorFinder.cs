using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using CompilePalX.Compiling;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CompilePalX
{
    static partial class ErrorFinder
    {
        private static List<Error> errorList = [];

        [GeneratedRegex("<h4>(.*?)</h4>")]
        private static partial Regex ErrorRegex();
        private static Regex errorDescriptionPattern = ErrorRegex();

        private static string errorStyle = Path.Combine("./Compiling", "errorstyle.html");
        private static string errorCache = Path.Combine("./Compiling", "errors.txt");
        public static void Init(bool refresh = false)
        {
            Thread t = new Thread(() => AsyncInit(ConfigurationManager.Settings.ErrorSourceURL, ConfigurationManager.Settings.ErrorCacheExpirationDays, refresh));
            t.Start();
        }

        static async void AsyncInit(string errorURL, int errorCacheExpirationDays, bool refresh)
        {
            try
            {
                if (!refresh && (File.Exists(errorCache) && (DateTime.Now.Subtract(File.GetLastWriteTime(errorCache)).TotalDays < errorCacheExpirationDays)))
                {
                    LoadJSONErrorData(File.ReadAllText(errorCache));
                    return;
                }

                try
                {
                    var c = new HttpClient();
                    c.DefaultRequestHeaders.ExpectContinue = true;
                    var httpResult = await c.GetAsync(errorURL);
                    string result = await c.GetStringAsync(new Uri(errorURL));

                    httpResult.Headers.TryGetValues("Content-Type", out var contentType);
                    if (contentType != null && contentType.First() == "application/json")
                    {
                        LoadJSONErrorData(result);
                    } else
                    {
                        LoadTextErrorData(result);
                    }

                    await File.WriteAllTextAsync(errorCache, JsonConvert.SerializeObject(errorList, new RegexConverter()));
                }
                catch (Exception e)
                {
                    // fallback to cache if download fails
                    ExceptionHandler.LogException(e, false);
                    if (File.Exists((errorCache)))
                    {
                        CompilePalLogger.LogLineDebug("Loading error data from cache");
                        LoadJSONErrorData(await File.ReadAllTextAsync(errorCache));
                    }
                    else
                    {
                        CompilePalLogger.LogLineDebug($"Error cache not found: {errorCache}");
                    }
                }
            }
            catch (Exception x)
            {
                //nonvital part, record but dont quit
                ExceptionHandler.LogException(x, false);
            }
        }

        static void LoadJSONErrorData(string input)
        {
            var errors = JsonConvert.DeserializeObject<List<Error>>(input, new RegexConverter()) ?? throw new Exception("Failed to deserialize errors");
            for (var i = 0; i < errors.Count; i++)
            {
                errors[i].ID = i;
            }
            errorList = errors;
        }

        static void LoadTextErrorData(string input)
        {
            string style = File.ReadAllText(errorStyle);

            var lines = input.Split(["\r\n", "\n"], StringSplitOptions.None);

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


                error.ID = id;
                errorList.Add(error);
                id++;
            }
        }

        public static Error? GetError(string line)
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

        [JsonIgnore]
        public int ID;

        public Error() { }

        public Error(string message, string shortDescription, ErrorSeverity severity, int id = -1)
        {
            Message = message;
            ShortDescription = shortDescription;
            Severity = (int) severity;
            ID = id;
        }
        public Error(string message, ErrorSeverity severity, int id = -1)
        {
            Message = message;
            ShortDescription = message;
            Severity = (int) severity;
            ID = id;
        }

        public override bool Equals(object obj)
        {
            if (obj is not Error) {
                return false;
            }
            return ((Error)obj).ID == ID;
        }

        public override int GetHashCode()
        {
            return ID;//ID is unique between errors
        }

        public object Clone()
        {
	        return MemberwiseClone();
        }

        [JsonIgnore]
        public Brush ErrorColor => GetSeverityBrush(Severity);

        public static Brush GetSeverityBrush(int severity)
        {
            return severity switch
            {
                2 => (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Severity2"),
                3 => (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Severity3"),
                4 => (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Severity4"),
                5 => (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Severity5"),
                _ => (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Severity1"),
            };
        }

        public string SeverityText
        {
            get
            {
                return Severity switch
                {
                    2 => "Caution",
                    3 => "Warning",
                    4 => "Error",
                    5 => "Fatal Error",
                    _ => "Info",
                };
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
