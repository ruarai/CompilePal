using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CompilePalX.KV
{
    public static class StringUtil {
        public static string GetUnquotedMaterial(string quoted) 
        {
            string[] sgts = quoted.Split('\"');
            string unquoted = "";
            int i = 0;
            foreach (string s in sgts) {
                if (i++ % 2 != 0) continue;
                unquoted += s;
            }
            return unquoted;
        }

        public static string GetFullPath(string line, string gameInfoDir) 
        {
            if (!line.StartsWith("..") || !line.StartsWith(""))
                return line;

            string fullPath = Path.GetFullPath(Path.Combine(gameInfoDir, line));
            return fullPath;
        }

        /// <summary>
        /// Cleans up an improperly formatted KV string
        /// </summary>
        /// <param name="kv">KV String to format</param>
        /// <returns>Formatted KV string</returns>
        public static string GetFormattedKVString(string kv)
        {
            var formatted = new StringBuilder();

            int startIndex = 0;
            int lineQuoteCount = 0;
            for (int i = 0; i < kv.Length; i++)
            {
                char c = kv[i];
                if (c == '{')
                {
                    if (i > startIndex)
                        formatted.AppendLine(kv.Substring(startIndex, i - startIndex));

                    formatted.AppendLine("{");

                    startIndex = i + 1;
                    lineQuoteCount = 0;
                } 
                else if (c == '}')
                {
                    if (i > startIndex)
                    {
                        var beforeCloseBraceText = kv.Substring(startIndex, i - startIndex).Trim();
                        if (beforeCloseBraceText != "")
                            formatted.AppendLine(beforeCloseBraceText);
                    }
                    
                    formatted.AppendLine("}");

                    startIndex = i + 1;
                    lineQuoteCount = 0;
                }
                else if (c == '\"')
                {
                    lineQuoteCount += 1;
                    if (lineQuoteCount == 2)
                    {
                        if (i > startIndex)
                            formatted.Append(kv.Substring(startIndex, i - startIndex + 1).Trim());
                        startIndex = i + 1;
                    }
                    else if (lineQuoteCount == 4)
                    {
                        if (i > startIndex)
                            formatted.AppendLine(kv.Substring(startIndex, i - startIndex + 1).Trim());

                        startIndex = i + 1;
                        lineQuoteCount = 0;
                    }
                }
                else if (c == '\n')
                {
                    startIndex = i + 1;
                    lineQuoteCount = 0;
                }
            }

            return formatted.ToString();
        }
    }

    public static class KV {
        public static Regex rx = new Regex("\"(.*?)\"|([^\\s]+)", RegexOptions.Compiled);
    }

    public class DataBlock {
        public string name = "";
        public List<DataBlock> subBlocks = new List<DataBlock>();
        public Dictionary<string, string> values = new Dictionary<string, string>();

        public DataBlock() { }

        private static DataBlock ParseDataBlock(ref StringReader reader, string name = "")
        {
            var block = new DataBlock
            {
                name = name.Trim()
            };

            string line;
            string prev = "";
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Split(new string[] { "//" }, StringSplitOptions.None)[0]; // ditch comments

                if (StringUtil.GetUnquotedMaterial(line).Contains("{"))
                {
                    string pname = prev;
                    //prev = prev.Replace("\"", "");
                    block.subBlocks.Add(ParseDataBlock(ref reader, pname));
                }
                if (StringUtil.GetUnquotedMaterial(line).Contains("}"))
                {
                    return block;
                }

                MatchCollection _matches = KV.rx.Matches(line);
                List<string> strings = new List<string>();

                for (int i = 0; i < _matches.Count; i++)
                {
                    strings.Add(_matches[i].Value.ToString().Replace("\"", ""));
                }

                if (strings.Count == 2)
                {
                    string keyname = strings[0];
                    int i = -1;
                    while (block.values.ContainsKey((++i > 0 ? keyname + i : keyname))) ;
                    block.values[i > 0 ? keyname + i : keyname] = strings[1];
                }

                prev = line;
            }

            return block;
        }

        public static DataBlock FromString(string block, string name = "")
        {
            var reader = new StringReader(block);
            return ParseDataBlock(ref reader, name);
        }

        public static DataBlock FromStream(ref StreamReader stream, string name = "") 
        {
            var reader = new StringReader(stream.ReadToEnd());
            return ParseDataBlock(ref reader, name);
        }

        public void Serialize(ref System.IO.StreamWriter stream, int depth = 0) 
        {
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

        public DataBlock GetFirstByName(string _name) 
        {
            for (int i = 0; i < this.subBlocks.Count; i++)
                if (_name == this.subBlocks[i].name)
                    return this.subBlocks[i];

            return null;
        }
        public DataBlock GetFirstByName(string[] names) 
        {
            for (int i = 0; i < this.subBlocks.Count; i++)
                if (names.Contains(this.subBlocks[i].name))
                    return this.subBlocks[i];

            return null;
        }

        public List<DataBlock> GetAllByName(string _name) 
        {
            List<DataBlock> c = new List<DataBlock>();
            for (int i = 0; i < this.subBlocks.Count; i++)
                if (_name == this.subBlocks[i].name)
                    c.Add(this.subBlocks[i]);

            return c;
        }

        public string TryGetStringValue(string key, string defaultValue = "") 
        {
            if (!this.values.ContainsKey(key)) return defaultValue;
            return this.values[key];
        }

        public List<string> GetList(string key) 
        {
            List<string> list = new List<string>();
            int vc = -1;
            while (this.values.ContainsKey(key + (++vc > 0 ? vc.ToString() : ""))) list.Add(this.values[key + (vc > 0 ? vc.ToString() : "")]);
            return list;
        }

        public override string ToString()
        {
            return $"DataBlock<\n\tname={this.name}\n\tvalues={{{string.Join("\n\t\t", this.values)}}}\n\tsubBlocks=[\n{string.Join(", ", this.subBlocks)}\n]>";
        }
    }
    public class FileData {
        public DataBlock headnode;

        public FileData(string filename) {
            StreamReader s = new StreamReader(filename);
            this.headnode = DataBlock.FromStream(ref s, "");
            s.Close();
        }
    }

}
