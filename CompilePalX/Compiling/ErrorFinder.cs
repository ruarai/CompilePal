using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompilePalX.Compiling;

namespace CompilePalX
{
    static class ErrorFinder
    {
        private static string errorText = Path.Combine("Compiling", "errors.txt");
        private static List<string> errorList;
        public static void Init()
        {
            var lines = File.ReadAllLines(errorText);

            errorList = lines.Skip(1).ToList();

            errorList.RemoveAll(string.IsNullOrWhiteSpace);

            Logger.Log("Error trigger loaded: ");
            Logger.LogLine(string.Join(Environment.NewLine + "Error trigger loaded: ",errorList));
        }

        public static bool IsError(string line)
        {
            return errorList.Any(error => line.Contains(error));
        }
    }
}
