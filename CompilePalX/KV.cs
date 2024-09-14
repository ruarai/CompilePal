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
}
