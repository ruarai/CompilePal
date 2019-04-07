using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public static class sutil {
    public static string get_unquoted_material(string ii) {
        string[] sgts = ii.Split('\"');
        string u = "";
        int i = 0;
        foreach (string s in sgts) {
            if (i++ % 2 != 0) continue;
            u += s;
        }
        return u;
    }

    public static string GetFullPath(string line, string gameInfoDir) {
        if (!line.StartsWith("..") || !line.StartsWith(""))
            return line;

        string fullPath = Path.GetFullPath(Path.Combine(gameInfoDir, line));
        return fullPath;
    }
}

namespace KV {
    public static class KV {
        public static Regex rx = new Regex("\"(.*?)\"|([^\\s]+)", RegexOptions.Compiled);
    }

    public class DataBlock {
        public string name = "";
        public List<DataBlock> subBlocks = new List<DataBlock>();
        public Dictionary<string, string> values = new Dictionary<string, string>();

        public DataBlock() { }

        public DataBlock(ref System.IO.StreamReader stream, string name = "") {
            this.name = name.Trim();

            string line = "";
            string prev = "";

            while((line = stream.ReadLine()) != null) {
                line = line.Split("//".ToCharArray())[0]; // ditch comments

                if (sutil.get_unquoted_material(line).Contains("{")) {
                    string pname = prev;
                    prev.Replace("\"", "");
                    this.subBlocks.Add(new DataBlock(ref stream, pname));
                }
                if (sutil.get_unquoted_material(line).Contains("}")) {
                    return;
                }

                MatchCollection _matches = KV.rx.Matches(line);
                List<string> strings = new List<string>();

                for(int i = 0; i < _matches.Count; i++) {
                    strings.Add(_matches[i].Value.ToString().Replace("\"", ""));
                }

                if(strings.Count == 2) {
                    string keyname = strings[0];
                    int i = -1;
                    while (this.values.ContainsKey((++i > 0 ? keyname + i : keyname))) ;
                    this.values[i > 0 ? keyname + i : keyname] = strings[1];
                }

                prev = line;
            }
        }

        public void Serialize(ref System.IO.StreamWriter stream, int depth = 0) {
            string indenta = "";
            for (int i = 0; i < depth; i++)
                indenta += "\t";
            string indentb = indenta + "\t";

            if (depth >= 0)
                stream.Write(indenta + this.name + "\n" + indenta + "{\n");

            foreach (string key in this.values.Keys)
                stream.Write(indentb + "\"" + key + "\" \"" + this.values[key] + "\"\n");

            for (int i = 0; i < this.subBlocks.Count; i++)
                this.subBlocks[i].Serialize(ref stream, depth + 1);

            if (depth >= 0)
                stream.Write(indenta + "}\n");
        }

        public DataBlock GetFirstByName(string _name) {
            for (int i = 0; i < this.subBlocks.Count; i++)
                if (_name == this.subBlocks[i].name)
                    return this.subBlocks[i];

            return null;
        }

        public List<DataBlock> GetAllByName(string _name) {
            List<DataBlock> c = new List<DataBlock>();
            for (int i = 0; i < this.subBlocks.Count; i++)
                if (_name == this.subBlocks[i].name)
                    c.Add(this.subBlocks[i]);

            return c;
        }

        public string tryGetStringValue(string key, string defaultValue = "") {
            if (!this.values.ContainsKey(key)) return defaultValue;
            return this.values[key];
        }

        public List<string> getList(string key) {
            List<string> list = new List<string>();
            int vc = -1;
            while (this.values.ContainsKey(key + (++vc > 0 ? vc.ToString() : ""))) list.Add(this.values[key + (vc > 0 ? vc.ToString() : "")]);
            return list;
        }
    }

    public class FileData {
        public DataBlock headnode;

        public FileData(string filename) {
            System.IO.StreamReader s = new System.IO.StreamReader(filename);
            this.headnode = new DataBlock(ref s, "");
            s.Close();
        }
    }
}

namespace CompilePalX {
    class GameConfigurationParser {
        public static List<GameConfiguration> Parse(string filename) {
            var lines = File.ReadAllLines(filename);
            var gameInfos = new List<GameConfiguration>();

            KV.FileData data = new KV.FileData(filename);

            foreach (KV.DataBlock gamedb in data.headnode.GetFirstByName("\"Configs\"").GetFirstByName("\"Games\"").subBlocks) {
                string binfolder = Path.GetDirectoryName(filename);
                KV.DataBlock hdb = gamedb.GetFirstByName("\"Hammer\"");

                GameConfiguration game = new GameConfiguration {
                    Name =          gamedb.name.Replace("\"", ""),
                    BinFolder =     binfolder,
                    GameFolder =    GetFullPath(gamedb.tryGetStringValue("GameDir"),    binfolder),
                    GameEXE =       GetFullPath(hdb.tryGetStringValue("GameExe"),       binfolder),
                    SDKMapFolder =  GetFullPath(hdb.tryGetStringValue("MapDir"),        binfolder),
                    VBSP =          GetFullPath(hdb.tryGetStringValue("BSP"),           binfolder),
                    VVIS =          GetFullPath(hdb.tryGetStringValue("Vis"),           binfolder),
                    VRAD =          GetFullPath(hdb.tryGetStringValue("Light"),         binfolder),
                    MapFolder =     GetFullPath(hdb.tryGetStringValue("BSPDir"),        binfolder)
                };

                gameInfos.Add(game);
            }

            return gameInfos;
        }

        static private string GetFullPath(string line, string gameInfoDir) {
            if (!line.StartsWith("..") || !line.StartsWith(""))
                return line;

            string fullPath = Path.GetFullPath(Path.Combine(gameInfoDir, line));
            return fullPath;
        }
    }
}