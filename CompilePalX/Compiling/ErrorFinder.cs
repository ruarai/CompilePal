using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilePalX
{
    static class ErrorFinder
    {
        private static string errorText = Path.Combine("Compiling", "errors.txt");
        private static List<string> errorList;
        public static void Init()
        {
            var lines = File.ReadAllLines(errorText);

            errorList = new List<string>(lines);

            errorList.RemoveAll(string.IsNullOrWhiteSpace);
        }

        public static bool IsError(string line)
        {
            return errorList.Any(error => line.Contains(error));
        }
    }
}
