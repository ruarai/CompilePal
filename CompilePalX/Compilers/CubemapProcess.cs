using CompilePalX.Compiling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CompilePalX.Compilers
{
    class CubemapProcess : CompileProcess
    {
        public CubemapProcess() : base("CUBEMAPS") { }

        bool HDR = false;
        bool LDR = false;

        string vbspInfo;
        string bspFile;


        public override void Run(CompileContext context, CancellationToken cancellationToken)
        {
            CompileErrors = new List<Error>();

            if (!CanRun(context)) return;

            vbspInfo = context.Configuration.VBSPInfo;
            bspFile = context.CopyLocation;

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

            try
            {
                CompilePalLogger.LogLine("\nCompilePal - Cubemap Generator");

                if (!File.Exists(context.CopyLocation))
                {
                    throw new FileNotFoundException();
                }

                var addtionalParameters = Regex.Replace(GetParameterString(), "-hidden", "");
                addtionalParameters = Regex.Replace(addtionalParameters, @"-iterations \w", "");
                bool hidden = GetParameterString().Contains("-hidden");

                string buildCubemapCommand = "-buildcubemaps";
                if (GetParameterString().Contains("-iterations"))
                {
                    try
                    {
                        int iterations = int.Parse(Regex.Match(GetParameterString(), @"-iterations (\w)").Groups[1].Value);
                        buildCubemapCommand = $"{buildCubemapCommand} {iterations}";
                    } catch
                    {
                        CompilePalLogger.LogCompileError("-iterations must be an int\n", new Error("-iterations must be an int", "CompilePal Internal Error", ErrorSeverity.FatalError));
                        return;
                    }
                }

                FetchHDRLevels();

                string mapname = System.IO.Path.GetFileName(context.CopyLocation).Replace(".bsp", "");

                string args =
                    $"-steam -game \"{context.Configuration.GameFolder}\" -windowed -insecure -novid -nosound +mat_specular 0 %HDRevel% +map {mapname} {buildCubemapCommand} {addtionalParameters}";

                if (hidden)
                    args += " -noborder -x 4000 -y 2000";

                if (HDR && LDR)
                {
                    CompilePalLogger.LogLine("Map requires two sets of cubemaps");

                    if (cancellationToken.IsCancellationRequested) return;
                    CompilePalLogger.LogLine("Compiling LDR cubemaps...");
                    RunCubemaps(context.Configuration.GameEXE, args.Replace("%HDRevel%", "+mat_hdr_level 0"), cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;
                    CompilePalLogger.LogLine("Compiling HDR cubemaps...");
                    RunCubemaps(context.Configuration.GameEXE, args.Replace("%HDRevel%", "+mat_hdr_level 2"), cancellationToken);
                }
                else
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    CompilePalLogger.LogLine("Map requires one set of cubemaps");
                    CompilePalLogger.LogLine("Compiling cubemaps...");
                    RunCubemaps(context.Configuration.GameEXE, args.Replace("%HDRevel%", ""), cancellationToken);
                }
                if (cancellationToken.IsCancellationRequested) return;
                CompilePalLogger.LogLine("Cubemaps compiled");
            }
            catch (FileNotFoundException)
            {
                CompilePalLogger.LogCompileError($"Could not find file: {context.CopyLocation}", new Error($"Could not find file: {context.CopyLocation}", ErrorSeverity.Error));
            }
            catch (Exception exception)
            {
                CompilePalLogger.LogLine("Something broke:");
                CompilePalLogger.LogCompileError($"{exception}\n", new Error(exception.ToString(), "CompilePal Internal Error", ErrorSeverity.FatalError));
            }

        }

        public void RunCubemaps(string gameEXE, string args, CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo(gameEXE, args);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = false;

            Process = new Process { StartInfo = startInfo };
            Process.Start();
            Process.WaitForExit();
        }

        public void FetchHDRLevels()
        {
            CompilePalLogger.LogLine("Detecting HDR levels...");
            var startInfo = new ProcessStartInfo(vbspInfo, "\"" + bspFile + "\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            Process = new Process { StartInfo = startInfo };
            try
            {
                Process.Start();
            }
            catch (Exception e)
            {
                CompilePalLogger.LogDebug(e.ToString());
                CompilePalLogger.LogCompileError($"Failed to run executable: {Process.StartInfo.FileName}\n", new Error($"Failed to run executable: {Process.StartInfo.FileName}", ErrorSeverity.Warning));
                CompilePalLogger.LogLine("Could not read HDR levels, defaulting to one.");
                return;
            }

            string output = Process.StandardOutput.ReadToEnd();

            if (Process.ExitCode != 0)
                CompilePalLogger.LogLine("Could not read HDR levels, defaulting to one.");
            else{
                Regex re = new Regex(@"^LDR\sworldlights\s+.*", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                string LDRStats = re.Match(output).Value.Trim();
                re = new Regex(@"^HDR\sworldlights\s+.*", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                string HDRStats = re.Match(output).Value.Trim();
                LDR = !LDRStats.Contains(" 0/");
                HDR = !HDRStats.Contains(" 0/");
            }
        }
    }
}
