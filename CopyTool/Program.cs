using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyTool
{
    class Program
    {
        static void Main(string[] args)
        {
            string copyDestination = null;
            string copySource = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-copyDestination")
                {
                    i++;
                    copyDestination = args[i];
                }

                if (args[i] == "-copySource")
                {
                    i++;
                    copySource = args[i];
                }
            }

            if (copyDestination != null && copySource != null)
            {
                try
                {
                    File.Copy(copySource, copyDestination, true);
                    Console.WriteLine("File {0} copied to {1}", Path.GetFileName(copySource), Path.GetDirectoryName(copyDestination));
                }
                catch
                {
                    Console.WriteLine("File {0} failed to be copied to {1}", copySource, copyDestination);
                }
            }
            else
            {
                Console.WriteLine("Not enough arguments received to copy a file.");
            }
        }
    }
}
