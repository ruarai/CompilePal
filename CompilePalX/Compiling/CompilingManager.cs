using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using CompilePalX.Compilers;
using CompilePalX.Compiling;

namespace CompilePalX
{
    internal delegate void CompileCleared();
    internal delegate void CompileFinished();
    static class CompilingManager
    {
        public static event CompileCleared OnClear;
        public static event CompileFinished OnFinish;

        public static ObservableCollection<string> MapFiles = new ObservableCollection<string>();


        private static Thread compileThread;
        private static Stopwatch compileTimeStopwatch = new Stopwatch();

        private static bool IsCompiling;

        public static void ToggleCompileState()
        {
            if (IsCompiling)
                CancelCompile();
            else
                StartCompile();
        }

        public static void StartCompile()
        {
            AnalyticsManager.Compile();

            IsCompiling = true;

            compileTimeStopwatch.Start();

            OnClear();

            CompilePalLogger.LogLine(string.Format("Starting a '{0}' compile.", ConfigurationManager.CurrentPreset));

            compileThread = new Thread(CompileThreaded);
            compileThread.Start();
        }

        private static void CompileThreaded()
        {
            try
            {
                ProgressManager.SetProgress(0);

                var compileErrors = new List<Error>();

                foreach (string mapFile in MapFiles)
                {
                    CompilePalLogger.LogLine(string.Format("Starting compilation of {0}", mapFile));

                    foreach (var compileProcess in ConfigurationManager.CompileProcesses.Where(c => c.DoRun))
                    {
                        compileProcess.Run(GameConfigurationManager.BuildContext(mapFile));

                        if (compileProcess is CompileExecutable)
                        {
                            var executable = compileProcess as CompileExecutable;

                            compileErrors.AddRange(executable.CompileErrors);
                        }

                        ProgressManager.Progress += (1d / ConfigurationManager.CompileProcesses.Count(c => c.DoRun)) / MapFiles.Count;
                    }
                }

                MainWindow.ActiveDispatcher.Invoke(() => postCompile(compileErrors));
            }
            catch (ThreadAbortException) { ProgressManager.ErrorProgress(); }
        }

        private static void postCompile(List<Error> errors)
        {
            CompilePalLogger.LogLineColor(string.Format("'{0}' compile finished in {1}", ConfigurationManager.CurrentPreset, compileTimeStopwatch.Elapsed.ToString(@"hh\:mm\:ss")), Brushes.ForestGreen);

            if (errors != null && errors.Any())
            {
                int maxSeverity = errors.Max(s => s.Severity);

                var severityBrush = Error.GetSeverityBrush(maxSeverity);

                CompilePalLogger.LogLineColor("{0} errors/warnings logged:", severityBrush, errors.Count);

                int i = 0;
                foreach (var error in errors)
                {
                    i++;

                    string errorText = string.Format("({0}) - ({1})", i, Error.GetSeverityText(error.Severity)) + Environment.NewLine;

                    CompilePalLogger.LogCompileError(errorText, error);

                    if (error.Severity >= 3)
                    {
                        AnalyticsManager.CompileError();
                    }
                }


            }

            OnFinish();

            compileTimeStopwatch.Reset();

            IsCompiling = false;
        }

        public static void CancelCompile()
        {
            compileThread.Abort();
            foreach (var compileProcess in ConfigurationManager.CompileProcesses.Where(cP => cP.Process != null))
            {
                try
                {
                    compileProcess.Process.Kill();

                    CompilePalLogger.LogLineColor("Killed {0}.", Brushes.OrangeRed, compileProcess.Name);
                }
                catch (InvalidOperationException) { }
                catch (Exception e) { ExceptionHandler.LogException(e); }
            }

            ProgressManager.SetProgress(0);

            CompilePalLogger.LogLineColor("Compile forcefully ended.", Brushes.OrangeRed);

            postCompile(null);
        }
    }
}
