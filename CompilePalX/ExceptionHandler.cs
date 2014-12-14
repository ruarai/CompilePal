using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;

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

            AnalyticsManager.Error();//risky, but /interesting/
            Thread.Sleep(2000);

            Environment.Exit(0);
        }
    }
}
