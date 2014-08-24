using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilePal
{
    public class GameInfo
    {
        public string GameFolder { get; set; }

        public string VBSP { get; set; }
        public string VVIS { get; set; }
        public string VRAD { get; set; }

        public string GameEXE { get; set; }

        public string MapFolder { get; set; }

        public string BinFolder
        {
            get { return VBSP.Replace("vbsp.exe", ""); }
        }

        public string Name { get; set; }


    }

    public class GameConfigs
    {

        public static List<GameInfo> Parse(string filename)
        {
            var lines = File.ReadAllLines(filename);

            //not as lazy parsing! woo!

            var gameInfos = new List<GameInfo>();
            for (int i = 4; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line == "	}" || line == "        }")
                    break;

                var game = new GameInfo();
                game.Name = GetKey(line);
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine(GetKey(line));

                i++;
                for (; i < lines.Length; i++)
                {
                    line = lines[i];
                    if (IsValid(line))
                    {
                        switch (GetKey(line))
                        {
                            case "GameDir":
                                game.GameFolder = GetValue(line);
                                Console.WriteLine(GetKey(line) + ":" + GetValue(line));
                                break;
                            case "GameExe":
                                game.GameEXE = GetValue(line);
                                Console.WriteLine(GetKey(line) + ":" + GetValue(line));
                                break;
                            case "BSP":
                                game.VBSP = GetValue(line);
                                Console.WriteLine(GetKey(line) + ":" + GetValue(line));
                                break;
                            case "Vis":
                                game.VVIS = GetValue(line);
                                Console.WriteLine(GetKey(line) + ":" + GetValue(line));
                                break;
                            case "Light":
                                game.VRAD = GetValue(line);
                                Console.WriteLine(GetKey(line) + ":" + GetValue(line));
                                break;
                            case "BSPDir":
                                game.MapFolder = GetValue(line);
                                Console.WriteLine(GetKey(line) + ":" + GetValue(line));
                                break;
                        }
                    }

                    if (line == "		}" || line == "                }")
                    {
                        gameInfos.Add(game);
                        break;
                    }
                }
            }

            return gameInfos;
        }

        static private bool IsValid(string line)
        {
            return line.Contains("\"");
        }

        static private string GetValue(string line)
        {
            return line.Split('"')[3];
        }

        static private string GetKey(string line)
        {
            return line.Split('"')[1];
        }
    }
}
