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
    static class ErrorFinder
    {
        private static List<Error> errorList = new List<Error>();

        private static Regex errorDescriptionPattern = new Regex("<h4>(.*?)</h4>");

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
                
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                try
                {
                    var c = new HttpClient();
                    var httpResult = await c.GetAsync(errorURL);
                    string result = await c.GetStringAsync(new Uri(errorURL));

                    IEnumerable<string>? contentType;
                    httpResult.Headers.TryGetValues("Content-Type", out contentType);
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
            var errors = JsonConvert.DeserializeObject<List<Error>>(input, new RegexConverter());
            if (errors == null)
            {
                throw new Exception("Failed to deserialize errors");
            }
            for (var i = 0; i < errors.Count; i++)
            {
                errors[i].ID = i;
            }
            errorList = errors;
        }

        static void LoadTextErrorData(string input)
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
            if (obj is not Error) {
                return false;
            }
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

        [JsonIgnore]
        public Brush ErrorColor => GetSeverityBrush(Severity);

        public static Brush GetSeverityBrush(int severity)
        {
            switch (severity)
            {
                default:
                    return (Brush) Application.Current.TryFindResource("CompilePal.Brushes.Severity1");
                case 2:
                    return (Brush) Application.Current.TryFindResource("CompilePal.Brushes.Severity2");
                case 3:
                    return (Brush) Application.Current.TryFindResource("CompilePal.Brushes.Severity3");
                case 4:
                    return (Brush) Application.Current.TryFindResource("CompilePal.Brushes.Severity4");
                case 5:
                    return (Brush) Application.Current.TryFindResource("CompilePal.Brushes.Severity5");
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
