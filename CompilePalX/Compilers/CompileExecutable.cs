﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers
{
    class CompileExecutable : CompileProcess
    {

        private static readonly string runningDirectory = ".";
        public CompileExecutable(string metadata)
            : base(metadata)
        {
            if (!Directory.Exists(runningDirectory))
            {
                Directory.CreateDirectory(runningDirectory);
            }
        }

        public override void Run(CompileContext c, CancellationToken cancellationToken)
        {
            CompileErrors = new List<Error>();

            if (!CanRun(c))
            {
                return;
            }

            // listen for cancellations
            cancellationToken.Register(() =>
            {
                try
                {
                    Cancel();
                }
                catch (InvalidOperationException) { }
                catch (Exception e) { ExceptionHandler.LogException(e); }
            });

            Process = new Process();
            if (Metadata.ReadOutput)
            {
                Process.StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            var args = GameConfigurationManager.SubstituteValues(GetParameterString(), c.MapFile);

            Process.StartInfo.FileName = GameConfigurationManager.SubstituteValues(Metadata.Path);
            Process.StartInfo.Arguments = string.Join(" ", args);
            Process.StartInfo.WorkingDirectory = runningDirectory;

            CompilePalLogger.LogLineDebug($"Running '{Process.StartInfo.FileName}' with args '{Process.StartInfo.Arguments}'");

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    CompilePalLogger.LogDebug($"Cancelled {Metadata.Name}");
                    return;
                }
                Process.Start();
            }
            catch (Exception e)
            {
                CompilePalLogger.LogDebug(e.ToString());
                CompilePalLogger.LogCompileError($"Failed to run executable: {Process.StartInfo.FileName}\n", new Error($"Failed to run executable: {Process.StartInfo.FileName}", ErrorSeverity.FatalError));
                return;
            }
            Process.PriorityClass = ProcessPriorityClass.BelowNormal;

            if (Metadata.ReadOutput)
            { 
                ReadOutput(cancellationToken);

                if (Metadata.CheckExitCode && Process.ExitCode != 0)
                    CompilePalLogger.LogCompileError($"{Name} exited with code: {Process.ExitCode}\n", new Error($"{Name} exited with code: {Process.ExitCode}", ErrorSeverity.Warning));
            }
        }

        private void ReadOutput(CancellationToken cancellationToken)
        {
            char[] buffer = new char [256];
            Task<int>? read = null;
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (read == null)
                    read = Process.StandardOutput.ReadAsync(buffer, 0, buffer.Length);

                read.Wait(100); // an arbitrary timeout

                if (read.IsCompleted)
                {
                    if (read.Result > 0)
                    {
                        string text = new (buffer, 0, read.Result);
                        CompilePalLogger.LogProgressive(text);

                        read = null; // task completed so we need to create a new one
                        continue;
                    }

                    // got -1, process ended
                    break;
                }

            }

            Process.WaitForExit();
        }
    }
}
