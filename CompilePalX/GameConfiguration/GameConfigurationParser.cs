using System;
using System.Collections.Generic;
using System.IO;
using CompilePalX.Compiling;

namespace CompilePalX
{
    class GameConfigurationParser
    {
        public static List<GameConfiguration> Parse(string filename)
        {
            Logger.LogLine("Parsing game configuration file {0}",filename);

            var lines = File.ReadAllLines(filename);

            //not as lazy parsing! woo!

            var gameInfos = new List<GameConfiguration>();
            for (int i = 4; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line == "	}" || line == "        }")
                    break;

                var game = new GameConfiguration { Name = GetKey(line) };

                game.BinFolder = Path.GetDirectoryName(filename);

                Logger.LogLine("Loading new game configuration:");
                Logger.LogLine(GetKey(line));

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
                                Logger.LogLine(GetKey(line) + ":" + GetValue(line));
                                break;
                            case "GameExe":
                                game.GameEXE = GetValue(line);
                                Logger.LogLine(GetKey(line) + ":" + GetValue(line));
                                break;
                            case "MapDir":
                                game.SDKMapFolder = GetValue(line);
                                Logger.LogLine(GetKey(line) + ":" + GetValue(line));
                                break;
                            case "BSP":
                                game.VBSP = GetValue(line);
                                Logger.LogLine(GetKey(line) + ":" + GetValue(line));
                                break;
                            case "Vis":
                                game.VVIS = GetValue(line);
                                Logger.LogLine(GetKey(line) + ":" + GetValue(line));
                                break;
                            case "Light":
                                game.VRAD = GetValue(line);
                                Logger.LogLine(GetKey(line) + ":" + GetValue(line));
                                break;
                            case "BSPDir":
                                game.MapFolder = GetValue(line);
                                Logger.LogLine(GetKey(line) + ":" + GetValue(line));
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