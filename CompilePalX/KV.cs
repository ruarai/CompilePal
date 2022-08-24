using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompilePalX.KV
{
    public static class StringUtil
    {
        public static string GetUnquotedMaterial(string quoted)
        {
            var sgts = quoted.Split('\"');
            var unquoted = "";
            var i = 0;
            foreach (var s in sgts)
            {
                if (i++ % 2 != 0)
                {
                    continue;
                }
                unquoted += s;
            }
            return unquoted;
        }

        public static string GetFullPath(string line, string gameInfoDir)
        {
            if (!line.StartsWith("..") || !line.StartsWith(""))
            {
                return line;
            }

            var fullPath = Path.GetFullPath(Path.Combine(gameInfoDir, line));
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

            var startIndex = 0;
            var lineQuoteCount = 0;
            for (var i = 0; i < kv.Length; i++)
            {
                var c = kv[i];
                if (c == '{')
                {
                    if (i > startIndex)
                    {
                        formatted.AppendLine(kv.Substring(startIndex, i - startIndex));
                    }

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
                        {
                            formatted.AppendLine(beforeCloseBraceText);
                        }
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
                        {
                            formatted.Append(kv.Substring(startIndex, i - startIndex + 1).Trim());
                        }
                        startIndex = i + 1;
                    }
                    else if (lineQuoteCount == 4)
                    {
                        if (i > startIndex)
                        {
                            formatted.AppendLine(kv.Substring(startIndex, i - startIndex + 1).Trim());
                        }

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

    public static class KV
    {
        public static Regex rx = new Regex("\"(.*?)\"|([^\\s]+)", RegexOptions.Compiled);
    }

    public class DataBlock
    {
        public string name = "";
        public List<DataBlock> subBlocks = new List<DataBlock>();
        public Dictionary<string, string> values = new Dictionary<string, string>();

        private static DataBlock ParseDataBlock(ref StringReader reader, string name = "")
        {
            var block = new DataBlock
            {
                name = name.Trim()
            };

            string line;
            var prev = "";
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Split(new[]
                {
                    "//"
                }, StringSplitOptions.None)[0]; // ditch comments

                if (StringUtil.GetUnquotedMaterial(line).Contains("{"))
                {
                    var pname = prev;
                    //prev = prev.Replace("\"", "");
                    block.subBlocks.Add(ParseDataBlock(ref reader, pname));
                }
                if (StringUtil.GetUnquotedMaterial(line).Contains("}"))
                {
                    return block;
                }

                var _matches = KV.rx.Matches(line);
                var strings = new List<string>();

                for (var i = 0; i < _matches.Count; i++)
                {
                    strings.Add(_matches[i].Value.Replace("\"", ""));
                }

                if (strings.Count == 2)
                {
                    var keyname = strings[0];
                    var i = -1;
                    while (block.values.ContainsKey(++i > 0 ? keyname + i : keyname))
                    {
                        ;
                    }
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

        public void Serialize(ref StreamWriter stream, int depth = 0)
        {
            var indenta = "";
            for (var i = 0; i < depth; i++)
            {
                indenta += "\t";
            }
            var indentb = indenta + "\t";

            if (depth >= 0)
            {
                stream.Write(indenta + name + "\n" + indenta + "{\n");
            }

            foreach (var key in values.Keys)
            {
                stream.Write(indentb + "\"" + key + "\" \"" + values[key] + "\"\n");
            }

            for (var i = 0; i < subBlocks.Count; i++)
            {
                subBlocks[i].Serialize(ref stream, depth + 1);
            }

            if (depth >= 0)
            {
                stream.Write(indenta + "}\n");
            }
        }

        public DataBlock GetFirstByName(string _name)
        {
            for (var i = 0; i < subBlocks.Count; i++)
            {
                if (_name == subBlocks[i].name)
                {
                    return subBlocks[i];
                }
            }

            return null;
        }
        public DataBlock GetFirstByName(string[] names)
        {
            for (var i = 0; i < subBlocks.Count; i++)
            {
                if (names.Contains(subBlocks[i].name))
                {
                    return subBlocks[i];
                }
            }

            return null;
        }

        public List<DataBlock> GetAllByName(string _name)
        {
            var c = new List<DataBlock>();
            for (var i = 0; i < subBlocks.Count; i++)
            {
                if (_name == subBlocks[i].name)
                {
                    c.Add(subBlocks[i]);
                }
            }

            return c;
        }

        public string TryGetStringValue(string key, string defaultValue = "")
        {
            if (!values.ContainsKey(key))
            {
                return defaultValue;
            }
            return values[key];
        }

        public List<string> GetList(string key)
        {
            var list = new List<string>();
            var vc = -1;
            while (values.ContainsKey(key + (++vc > 0 ? vc.ToString() : "")))
            {
                list.Add(values[key + (vc > 0 ? vc.ToString() : "")]);
            }
            return list;
        }

        public override string ToString()
        {
            return $"DataBlock<\n\tname={name}\n\tvalues={{{string.Join("\n\t\t", values)}}}\n\tsubBlocks=[\n{string.Join(", ", subBlocks)}\n]>";
        }
    }
    public class FileData
    {
        public DataBlock headnode;

        public FileData(string filename)
        {
            var s = new StreamReader(filename);
            headnode = DataBlock.FromStream(ref s);
            s.Close();
        }
    }

}
