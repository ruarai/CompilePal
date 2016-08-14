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
        public CubemapProcess() : base("Parameters\\BuiltIn\\cubemaps.meta") { }

        bool HDR = false;
        bool LDR = false;

        string vbspInfo;
        string bspFile;
        

        public override void Run(CompileContext context)
        {
            vbspInfo = context.Configuration.VBSPInfo;
            bspFile = context.BSPFile;

            FetchHDRLevels();

            string mapname = System.IO.Path.GetFileName(context.BSPFile).Replace(".bsp", "");

            string args = "-game \"" + context.Configuration.GameFolder +"\" -windowed -novid -disconnect +mat_specular 0 -w 1024 -h 1024";
            string argsldr = " +mat_hdr_level 0 +map " + mapname + " -buildcubemaps";
            string argshdr = " +mat_hdr_level 2 +map " + mapname + " -buildcubemaps";
            string argssingle = " +map " + mapname + " -buildcubemaps";

            if (HDR && LDR)
            {
                CompilePalLogger.LogLine("Map requires two sets of cubemaps");

                CompilePalLogger.LogLine("Compiling LDR cubemaps...");
                var startInfo = new ProcessStartInfo(context.Configuration.GameEXE, args + argsldr);
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = false;

                var p = new Process { StartInfo = startInfo };
                p.Start();
                p.WaitForExit();

                CompilePalLogger.LogLine("Compiling HDR cubemaps...");
                startInfo = new ProcessStartInfo(context.Configuration.GameEXE, args + argshdr);
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = false;

                p = new Process { StartInfo = startInfo };
                p.Start();
                p.WaitForExit();
            }
            else
            {
                CompilePalLogger.LogLine("Map requires one set of cubemaps");
                CompilePalLogger.LogLine("Compiling cubemaps...");
                var startInfo = new ProcessStartInfo(context.Configuration.GameEXE, args + argssingle);
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = false;

                var p = new Process { StartInfo = startInfo };
                p.Start();
                p.WaitForExit();
            }
            CompilePalLogger.LogLine("Cubemaps compiled");

        }


        public void FetchHDRLevels()
        {
            CompilePalLogger.LogLine("Detecting HDR levels...");
            string arguments = "-treeinfo " + bspFile;
            var startInfo = new ProcessStartInfo(vbspInfo, arguments);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;

            var p = new Process { StartInfo = startInfo };
            p.Start();
            string output = p.StandardOutput.ReadToEnd();

            Regex re = new Regex(@"^faces\s+.*", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string LDRStats = re.Match(output).Value.Trim();
            re = new Regex(@"^faces\s+.*", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string HDRStats = re.Match(output).Value.Trim();
            LDR = !LDRStats.EndsWith("( 0.0%)");
            HDR = !HDRStats.EndsWith("( 0.0%)");

            
        }


    }
}
