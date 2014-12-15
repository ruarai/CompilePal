using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using CompilePalX.Compiling;

namespace CompilePalX
{
    static class ExceptionHandler
    {
        public static void LogException(Exception e)
        {
            if (!Directory.Exists("CrashLogs"))
                Directory.CreateDirectory("CrashLogs");

            string crashLogName = DateTime.Now.ToString("s").Replace(":", "-");

            File.WriteAllText(Path.Combine("CrashLogs", crashLogName + ".txt"), e.ToString() + e.InnerException ?? "");

            Logger.LogLine("An exception was caught by the ExceptionHandler:");
            Logger.LogLine(e.ToString());
            if(e.InnerException != null)
                Logger.LogLine(e.InnerException.ToString());

            AnalyticsManager.Error();//risky, but /interesting/
            Thread.Sleep(2000);

            Environment.Exit(0);
        }
    }
}
