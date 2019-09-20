using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Shell;
using CompilePalX.Compiling;
using MahApps.Metro.Controls.Dialogs;

namespace CompilePalX
{
    static class ExceptionHandler
    {
        public static async void LogException(Exception e, bool crash = true)
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
				
				ProgressManager.ErrorProgress();
				var modalDialogSettings = new MetroDialogSettings
				{
					AffirmativeButtonText = "Exit",
					ColorScheme = MetroDialogColorScheme.Theme
				};
				await MainWindow.Instance.ShowModal("A fatal exception has occured", e.Message, settings: modalDialogSettings);

                Environment.Exit(0);
            }
        }
    }
}
