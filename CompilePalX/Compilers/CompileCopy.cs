using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers
{
    class CompileCopy : CompileProcess
    {
        public CompileCopy()
            : base("Parameters\\BuiltIn\\copy.meta")
        {

        }

        public override void Run(CompileContext context)
        {
            string copyDestination = context.CopyLocation;
            string copySource = context.BSPFile;
            try
            {
                File.Copy(copySource, copyDestination, true);
                CompilePalLogger.LogLine("File {0} copied to {1}", System.IO.Path.GetFileName(copySource), System.IO.Path.GetDirectoryName(copyDestination));
            }
            catch
            {
                CompilePalLogger.LogLine("File {0} failed to be copied to {1}", copySource, copyDestination);
            }
        }
    }
}
