using System;
using System.Collections.Generic;
using System.IO;
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

            System.Media.SystemSounds.Asterisk.Play();

            AnalyticsManager.Error();//risky, but /interesting/

            Environment.Exit(0);
        }
    }
}
