using CompilePalX.Compiling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        bool hidden;
        

        public override void Run(CompileContext context)
        {
            vbspInfo = context.Configuration.VBSPInfo;
            bspFile = context.CopyLocation;

            CompilePalLogger.LogLine("\nCompilePal - Cubemap Generator");

            hidden = GetParameterString().Contains("-hidden");
            FetchHDRLevels();

            string mapname = System.IO.Path.GetFileName(context.BSPFile).Replace(".bsp", "");

            string args = "-game \"" + context.Configuration.GameFolder +"\" -windowed -novid -nosound +mat_specular 0 %HDRevel% +map " + mapname + " -buildcubemaps";

            if (hidden)
                args += " -noborder -x 4000 -y 2000";

            if (HDR && LDR)
            {
                CompilePalLogger.LogLine("Map requires two sets of cubemaps");

                CompilePalLogger.LogLine("Compiling LDR cubemaps...");
                RunCubemaps(context.Configuration.GameEXE, args.Replace("%HDRevel%", "+mat_hdr_level 0"));

                CompilePalLogger.LogLine("Compiling HDR cubemaps...");
                RunCubemaps(context.Configuration.GameEXE, args.Replace("%HDRevel%", "+mat_hdr_level 2"));
            }
            else
            {
                CompilePalLogger.LogLine("Map requires one set of cubemaps");
                CompilePalLogger.LogLine("Compiling cubemaps...");
                RunCubemaps(context.Configuration.GameEXE, args.Replace("%HDRevel%", ""));
            }
            CompilePalLogger.LogLine("Cubemaps compiled");

        }

        public void RunCubemaps(string gameEXE, string args)
        {
            var startInfo = new ProcessStartInfo(gameEXE, args);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = false;

            var p = new Process { StartInfo = startInfo };
            p.Start();
            p.WaitForExit();
        }

        public void FetchHDRLevels()
        {
            CompilePalLogger.LogLine("Detecting HDR levels...");
            var startInfo = new ProcessStartInfo(vbspInfo, "\"" + bspFile + "\"");
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;

            var p = new Process { StartInfo = startInfo };
            p.Start();
            string output = p.StandardOutput.ReadToEnd();

            if (p.ExitCode != 0)
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
