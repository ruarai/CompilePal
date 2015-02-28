using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
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

        private static string runningDirectory = "CompileLogs";

        private static void CompileThreaded()
        {
            try
            {
                if (!Directory.Exists(runningDirectory))
                    Directory.CreateDirectory(runningDirectory);

                ProgressManager.SetProgress(0);

                foreach (string mapFile in MapFiles)
                {
                    CompilePalLogger.LogLine(string.Format("Starting compilation of {0}", mapFile));

                    foreach (var compileProcess in ConfigurationManager.CompileProcesses.Where(c => c.DoRun))
                    {
                        compileProcess.Process = new Process { StartInfo = { RedirectStandardOutput = true, RedirectStandardInput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } };


                        compileProcess.Process.StartInfo.FileName = compileProcess.Path;
                        compileProcess.Process.StartInfo.Arguments = GameConfigurationManager.SubstituteValues(compileProcess.GetParameterString(), mapFile);
                        compileProcess.Process.StartInfo.WorkingDirectory = runningDirectory;

                        compileProcess.Process.Start();
                        compileProcess.Process.PriorityClass = ProcessPriorityClass.BelowNormal;

                        char[] buffer = new char[256];
                        Task<int> read = null;

                        while (true)
                        {
                            if (read == null)
                                read = compileProcess.Process.StandardOutput.ReadAsync(buffer, 0, buffer.Length);

                            read.Wait(100); // an arbitray timeout

                            if (read.IsCompleted)
                            {
                                if (read.Result > 0)
                                {
                                    string text = new string(buffer, 0, read.Result);
                                    CompilePalLogger.Log(text);

                                    if (CheckError(text))
                                    {
                                        CompilePalLogger.LogLine("An error cancelled the compile.");
                                        return;
                                    }

                                    read = null; // ok, this task completed so we need to create a new one
                                    continue;
                                }

                                // got -1, process ended
                                break;
                            }
                        }

                        compileProcess.Process.WaitForExit();


                        ProgressManager.Progress += (1d / ConfigurationManager.CompileProcesses.Count(c => c.DoRun)) / MapFiles.Count;
                    }
                }

                MainWindow.ActiveDispatcher.Invoke(postCompile);
            }
            catch (ThreadAbortException) { }
        }

        private static void postCompile()
        {
            CompilePalLogger.LogLine(string.Format("'{0}' compile finished in {1}", ConfigurationManager.CurrentPreset, compileTimeStopwatch.Elapsed.ToString(@"hh\:mm\:ss")));

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

                    CompilePalLogger.LogLine(string.Join("Killed {0}.",compileProcess.Name));
                }
                catch (InvalidOperationException) { }
                catch(Exception e) { ExceptionHandler.LogException(e);}
            }

            ProgressManager.SetProgress(0);

            CompilePalLogger.LogLine("Compile forcefully ended.");

            postCompile();
        }

        private static string lineBuffer = string.Empty;
        static bool CheckError(string text)
        {
            //The process of trying to sort the random spouts of letters back into lines. Hacky.

            if (text.Contains("\n"))
            {
                lineBuffer += text;

                List<string> lines = lineBuffer.Split('\n').ToList();

                lineBuffer = lines.Last();

                foreach (string line in lines)
                {
                    if (ErrorFinder.IsError(line))
                        return true;
                }
            }
            else
                lineBuffer += text;

            return false;
        }

    }
}
