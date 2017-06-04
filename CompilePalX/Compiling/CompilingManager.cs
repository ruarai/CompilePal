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
using System.Runtime.InteropServices;

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
            // Tells windows to not go to sleep during compile
            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);

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

                var mapErrors = new List<Tuple<string, List<Error>>>();


                foreach (string mapFile in MapFiles)
                {
                    string cleanMapName = Path.GetFileNameWithoutExtension(mapFile);

                    var compileErrors = new List<Error>();
                    CompilePalLogger.LogLine(string.Format("Starting compilation of {0}", cleanMapName));

                    foreach (var compileProcess in ConfigurationManager.CompileProcesses.Where(c => c.Metadata.DoRun && c.PresetDictionary.ContainsKey(ConfigurationManager.CurrentPreset)))
                    {
                        compileProcess.Run(GameConfigurationManager.BuildContext(mapFile));

                        if (compileProcess is CompileExecutable)
                        {
                            var executable = compileProcess as CompileExecutable;

                            compileErrors.AddRange(executable.CompileErrors);
                        }

                        ProgressManager.Progress += (1d / ConfigurationManager.CompileProcesses.Count(c => c.Metadata.DoRun &&
                            c.PresetDictionary.ContainsKey(ConfigurationManager.CurrentPreset))) / MapFiles.Count;
                    }

                    mapErrors.Add(new Tuple<string, List<Error>>(cleanMapName, compileErrors));
                }

                MainWindow.ActiveDispatcher.Invoke(() => postCompile(mapErrors));
            }
            catch (ThreadAbortException) { ProgressManager.ErrorProgress(); }
        }

        private static void postCompile(List<Tuple<string, List<Error>>> errors)
        {
            CompilePalLogger.LogLineColor(string.Format("'{0}' compile finished in {1}", ConfigurationManager.CurrentPreset, compileTimeStopwatch.Elapsed.ToString(@"hh\:mm\:ss")), Brushes.ForestGreen);

            if (errors != null && errors.Any())
            {
                CompilePalLogger.LogLineColor("{0} errors/warnings logged:", Error.GetSeverityBrush(errors.Max(e => e.Item2.Max(e2 => e2.Severity))), errors.Sum(e=>e.Item2.Count));

                foreach (var mapTuple in errors)
                {
                    string mapName = mapTuple.Item1;
                    var mapErrors = mapTuple.Item2;

                    CompilePalLogger.Log("  ");
                    CompilePalLogger.LogLineColor("{0} errors/warnings logged for {1}:", Error.GetSeverityBrush(mapErrors.Max(s => s.Severity)), mapErrors.Count(), mapName);

                    var distinctErrors = mapErrors.GroupBy(e => e.ID);

                    foreach (var errorList in distinctErrors)
                    {
                        var error = errorList.First();

                        string errorText = string.Format("{0}x: {1} ({2})", errorList.Count(), error.ShortDescription, Error.GetSeverityText(error.Severity)) + Environment.NewLine;

                        CompilePalLogger.Log("    ● ");
                        CompilePalLogger.LogCompileError(errorText, error);

                        if (error.Severity >= 3)
                        {
                            AnalyticsManager.CompileError();
                        }
                    }
                }
            }

            OnFinish();

            compileTimeStopwatch.Reset();

            IsCompiling = false;

            // Tells windows it's now okay to enter sleep
            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
        }

        public static void CancelCompile()
        {
            compileThread.Abort();
            foreach (var compileProcess in ConfigurationManager.CompileProcesses.Where(cP => cP.Process != null))
            {
                try
                {
                    compileProcess.Cancel();
                    compileProcess.Process.Kill();

                    CompilePalLogger.LogLineColor("Killed {0}.", Brushes.OrangeRed, compileProcess.Metadata.Name);
                }
                catch (InvalidOperationException) { }
                catch (Exception e) { ExceptionHandler.LogException(e); }
            }

            ProgressManager.SetProgress(0);

            CompilePalLogger.LogLineColor("Compile forcefully ended.", Brushes.OrangeRed);

            postCompile(null);
        }

        internal static class NativeMethods
        {
            // Import SetThreadExecutionState Win32 API and necessary flags
            [DllImport("kernel32.dll")]
            public static extern uint SetThreadExecutionState(uint esFlags);
            public const uint ES_CONTINUOUS = 0x80000000;
            public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        }
    }
}
