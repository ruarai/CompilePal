﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Windows.Media;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers
{
    class CompileExecutable(string metadata, string? parameterFolder = null) : CompileProcess(metadata, parameterFolder)
    {
        public override void Run(CompileContext c, CancellationToken cancellationToken)
        {
            CompileErrors = [];

            if (!CanRun(c)) return;

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
                        CreateNoWindow = true,
                    };
            }

            var args = GameConfigurationManager.SubstituteValues(GetParameterString(), c.MapFile);

            bool normalPriority = false;
            if (args.Contains("-normal_priority"))
            {
                args = args.Replace("-normal_priority", string.Empty);
                normalPriority = true;
            }

            Process.StartInfo.FileName = GameConfigurationManager.SubstituteValues(Metadata.Path);
            Process.StartInfo.Arguments = string.Join(" ", args);
            Process.StartInfo.WorkingDirectory = Metadata.WorkingDirectory != null ? GameConfigurationManager.SubstituteValues(Metadata.WorkingDirectory, quote: false) : ".";

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

            if (normalPriority)
            {
                Process.PriorityClass = ProcessPriorityClass.Normal;
                CompilePalLogger.LogLine($"Running {Name} with normal priority");
            }
            else 
                Process.PriorityClass = ProcessPriorityClass.BelowNormal;

            //VRAD can only use up to 16 cores, more cores reduces performance, and increased compile time.
            //By assigning affinity to the process significantly speed up compiles on CPUs with more than 16 cores.
            if (this.Name == "VRAD" &&
                Environment.ProcessorCount > 16)
            {
                //Hex 0xffff means core use core 0 through 15.
                Process.ProcessorAffinity = (IntPtr)0xffff;
            }
            
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

                read.Wait(100, cancellationToken); // an arbitrary timeout

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
