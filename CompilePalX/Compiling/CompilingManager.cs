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

namespace CompilePalX
{
    internal delegate void CompileWritten(string line);
    internal delegate void CompileCleared();
    static class CompilingManager
    {
        public static event CompileWritten OnWrite;
        public static event CompileCleared OnClear;

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
            IsCompiling = true;

            compileTimeStopwatch.Start();

            OnClear();

            writeLine(string.Format("Starting a '{0}' compile.", ConfigurationManager.CurrentPreset));

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
                    writeLine(string.Format("Starting compilation of {0}", mapFile));

                    foreach (var compileProcess in ConfigurationManager.CompileProcesses.Where(c => c.DoRun))
                    {
                        compileProcess.Process = new Process { StartInfo = { RedirectStandardOutput = true, RedirectStandardInput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } };


                        compileProcess.Process.StartInfo.FileName = compileProcess.Path;
                        compileProcess.Process.StartInfo.Arguments = FinaliseParameterString(compileProcess.GetParameterString(), mapFile);
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
                                    write(text);

                                    if (CheckError(text))
                                    {
                                        writeLine("An error cancelled the compile.");
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
            writeLine(string.Format("'{0}' compile finished in {1}", ConfigurationManager.CurrentPreset, compileTimeStopwatch.Elapsed.ToString(@"hh\:mm\:ss")));
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
                }
                catch { }
            }

            ProgressManager.SetProgress(0);

            writeLine("Compile forcefully ended.");

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

        public static string FinaliseParameterString(string paramString, string mapFile)
        {
            paramString = paramString.Replace("$vmfFile$", string.Format("\"{0}\"", mapFile));
            paramString = paramString.Replace("$map$", string.Format("\"{0}\"", Path.GetFileNameWithoutExtension(mapFile)));
            paramString = paramString.Replace("$bsp$", string.Format("\"{0}\"", Path.ChangeExtension(mapFile, "bsp")));

            paramString = paramString.Replace("$mapCopyLocation$", string.Format("\"{0}\"", Path.Combine(GameConfigurationManager.GameConfiguration.MapFolder, Path.GetFileName(mapFile))));

            paramString = paramString.Replace("$game$", string.Format("\"{0}\"", GameConfigurationManager.GameConfiguration.GameFolder));
            paramString = paramString.Replace("$gameEXE$", string.Format("\"{0}\"", GameConfigurationManager.GameConfiguration.GameEXE));
            paramString = paramString.Replace("$binFolder$", string.Format("\"{0}\"", GameConfigurationManager.GameConfiguration.BinFolder));
            paramString = paramString.Replace("$mapFolder$", string.Format("\"{0}\"", GameConfigurationManager.GameConfiguration.MapFolder));
            paramString = paramString.Replace("$gameName$", string.Format("\"{0}\"", GameConfigurationManager.GameConfiguration.Name));
            paramString = paramString.Replace("$sdkFolder$", string.Format("\"{0}\"", GameConfigurationManager.GameConfiguration.SDKMapFolder));

            paramString = paramString.Replace("$bspZip$", string.Format("\"{0}\"", GameConfigurationManager.GameConfiguration.BSPZip));

            paramString = paramString.Replace("$keys$", string.Format("\"{0}\"",Path.Combine(Environment.CurrentDirectory,"Keys")));

            return paramString;
        }

        private static void write(string text)
        {
            OnWrite(text);
        }
        private static void writeLine(string line)
        {
            OnWrite(line + Environment.NewLine);
        }
    }
}
