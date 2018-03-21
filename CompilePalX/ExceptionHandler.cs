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
        public static void LogException(Exception e, bool crash = true)
        {
            if (!Directory.Exists(CompilePalPath.Directory + "CrashLogs"))
                Directory.CreateDirectory(CompilePalPath.Directory + "CrashLogs");


            CompilePalLogger.LogLine("An exception was caught by the ExceptionHandler:");
            CompilePalLogger.LogLine(e.ToString());
            if (e.InnerException != null)
                CompilePalLogger.LogLine(e.InnerException.ToString());

            try {
                AnalyticsManager.Error();//risky, but /interesting/
            } catch (Exception) {}

            if (crash)
            {
                string crashLogName = DateTime.Now.ToString("s").Replace(":", "-");

                File.WriteAllText(Path.Combine("CrashLogs", crashLogName + ".txt"), e.ToString() + e.InnerException ?? "");

                Thread.Sleep(2000);
                Environment.Exit(0);
            }
        }
    }
}
