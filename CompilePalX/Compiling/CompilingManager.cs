using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CompilePalX.Annotations;
using CompilePalX.Compiling;
using CompilePalX.Configuration;

namespace CompilePalX
{
    delegate void CompileCleared();
    delegate void CompileStarted();
    delegate void CompileFinished();

    public class Map : INotifyPropertyChanged
    {

        private bool compile;
        private string file;

        private Preset? preset;

        public Map(string file, bool compile = true, Preset? preset = null)
        {
            File = file;
            Compile = compile;
            Preset = preset;
        }

        public string File
        {
            get => file;
            set
            {
                file = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Map name without version identifiers
        /// </summary>
        public string MapName
        {
            get
            {
                var fullMapName = Path.GetFileNameWithoutExtension(file);

                // try removing version identifier
                return Regex.Replace(fullMapName, @"_[^_]+\d$", "");
            }
        }

        public bool IsBSP => Path.GetExtension(file) == ".bsp";
        public bool Compile
        {
            get => compile;
            set
            {
                compile = value;
                OnPropertyChanged();
            }
        }
        public Preset? Preset
        {
            get => preset;
            set
            {
                preset = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    static class CompilingManager
    {

        public static TrulyObservableCollection<Map> MapFiles = new TrulyObservableCollection<Map>();

        private static readonly Stopwatch compileTimeStopwatch = new Stopwatch();
        private static CancellationTokenSource cts;

        private static CompileProcess currentCompileProcess;
        static CompilingManager()
        {
            CompilePalLogger.OnErrorFound += CompilePalLogger_OnErrorFound;
        }

        public static bool IsCompiling { get; private set; }

        private static void CompilePalLogger_OnErrorFound(Error e)
        {
            currentCompileProcess.CompileErrors.Add(e);

            if (e.Severity == 5 && IsCompiling)
            {
                //We're currently in the thread we would like to kill, so make sure we invoke from the window thread to do this.
                MainWindow.ActiveDispatcher.Invoke(() =>
                {
                    CompilePalLogger.LogLineColor("An error cancelled the compile.", Error.GetSeverityBrush(5));
                    CancelCompile();
                    ProgressManager.ErrorProgress();
                });
            }
        }

        public static event CompileCleared OnClear;
        public static event CompileFinished OnStart;
        public static event CompileFinished OnFinish;

        public static void ToggleCompileState()
        {
            if (IsCompiling)
            {
                CancelCompile();
            }
            else
            {
                StartCompile();
            }
        }

        public static void StartCompile()
        {
            OnStart();

            // Tells windows to not go to sleep during compile
            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);

            AnalyticsManager.Compile();

            IsCompiling = true;

            compileTimeStopwatch.Start();

            OnClear();

            cts = new CancellationTokenSource();
            Task.Run(() => CompileThreaded(cts.Token));
        }

        private static void CompileThreaded(CancellationToken cancellationToken)
        {
            try
            {
                ProgressManager.SetProgress(0);

                var mapErrors = new List<MapErrors>();


                foreach (var map in MapFiles)
                {
                    if (!map.Compile)
                    {
                        CompilePalLogger.LogDebug($"Skipping {map.File}");
                        continue;
                    }

                    var mapFile = map.File;
                    var cleanMapName = Path.GetFileNameWithoutExtension(mapFile);
                    ConfigurationManager.CurrentPreset = map.Preset;

                    var compileErrors = new List<Error>();
                    CompilePalLogger.LogLine($"Starting a '{ConfigurationManager.CurrentPreset?.Name}' compile.");
                    CompilePalLogger.LogLine($"Starting compilation of {cleanMapName}");

                    //Update the grid so we have the most up to date order
                    OrderManager.UpdateOrder();

                    GameConfigurationManager.BackupCurrentContext();
                    foreach (var compileProcess in OrderManager.CurrentOrder)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        currentCompileProcess = compileProcess;
                        compileProcess.Run(GameConfigurationManager.BuildContext(map), cancellationToken);

                        compileErrors.AddRange(currentCompileProcess.CompileErrors);

                        //Portal 2 cannot work with leaks, stop compiling if we do get a leak.
                        if (GameConfigurationManager.GameConfiguration.Name == "Portal 2")
                        {
                            if (currentCompileProcess.Name == "VBSP" && currentCompileProcess.CompileErrors.Count > 0)
                            {
                                //we have a VBSP error, aka a leak -> stop compiling;
                                break;
                            }
                        }

                        ProgressManager.Progress += 1d / ConfigurationManager.CompileProcesses.Count(c => c.Metadata.DoRun &&
                                                                                                          c.PresetDictionary.ContainsKey(ConfigurationManager.CurrentPreset)) / MapFiles.Count;
                    }

                    mapErrors.Add(new MapErrors
                    {
                        MapName = cleanMapName, Errors = compileErrors
                    });

                    GameConfigurationManager.RestoreCurrentContext();
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    MainWindow.ActiveDispatcher.Invoke(() => postCompile(mapErrors));
                }
            }
            catch (OperationCanceledException) { ProgressManager.ErrorProgress(); }
        }

        private static void postCompile(List<MapErrors> errors)
        {
            CompilePalLogger.LogLineColor(
                $"\n'{ConfigurationManager.CurrentPreset!.Name}' compile finished in {compileTimeStopwatch.Elapsed.ToString(@"hh\:mm\:ss")}", (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Success"));

            if (errors != null && errors.Any())
            {
                var numErrors = errors.Sum(e => e.Errors.Count);
                var maxSeverity = errors.Max(e => e.Errors.Any() ? e.Errors.Max(e2 => e2.Severity) : 0);
                CompilePalLogger.LogLineColor("{0} errors/warnings logged:", Error.GetSeverityBrush(maxSeverity), numErrors);

                foreach (var map in errors)
                {
                    CompilePalLogger.Log("  ");

                    if (!map.Errors.Any())
                    {
                        CompilePalLogger.LogLineColor("No errors/warnings logged for {0}", Error.GetSeverityBrush(0), map.MapName);
                        continue;
                    }

                    var mapMaxSeverity = map.Errors.Max(e => e.Severity);
                    CompilePalLogger.LogLineColor("{0} errors/warnings logged for {1}:", Error.GetSeverityBrush(mapMaxSeverity), map.Errors.Count, map.MapName);

                    var distinctErrors = map.Errors.GroupBy(e => e.ID).OrderBy(e => e.First().Severity);
                    foreach (var errorList in distinctErrors)
                    {
                        var error = errorList.First();

                        var errorText = $"{errorList.Count()}x: {error.SeverityText}: {error.ShortDescription}";

                        CompilePalLogger.Log("    ● ");
                        CompilePalLogger.LogCompileError(errorText, error);
                        CompilePalLogger.LogLine();

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
            try
            {
                cts.Cancel();
            }
            catch { }
            IsCompiling = false;

            ProgressManager.SetProgress(0);

            CompilePalLogger.LogLineColor("Compile forcefully ended.", (Brush)Application.Current.TryFindResource("CompilePal.Brushes.Severity4"));

            postCompile(null);
        }

        public static Stopwatch GetTime()
        {
            return compileTimeStopwatch;
        }

        private class MapErrors
        {
            public string MapName { get; set; }
            public List<Error> Errors { get; set; }
        }

        internal static class NativeMethods
        {
            public const uint ES_CONTINUOUS = 0x80000000;
            public const uint ES_SYSTEM_REQUIRED = 0x00000001;
            // Import SetThreadExecutionState Win32 API and necessary flags
            [DllImport("kernel32.dll")]
            public static extern uint SetThreadExecutionState(uint esFlags);
        }
    }
}
