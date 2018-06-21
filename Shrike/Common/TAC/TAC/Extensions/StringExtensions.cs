// // 
// //  Copyright 2012 David Gressett
// // 
// //    Licensed under the Apache License, Version 2.0 (the "License");
// //    you may not use this file except in compliance with the License.
// //    You may obtain a copy of the License at
// // 
// //        http://www.apache.org/licenses/LICENSE-2.0
// // 
// //    Unless required by applicable law or agreed to in writing, software
// //    distributed under the License is distributed on an "AS IS" BASIS,
// //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //    See the License for the specific language governing permissions and
// //    limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AppComponents.Extensions.StringEx
{
    public static class StringExtensions
    {
        public static string SpacePascalCase(this string input)
        {
            string splitString = String.Empty;

            for (int idx = 0; idx < input.Length; idx++)
            {
                char c = input[idx];

                if (Char.IsUpper(c)
                    // keeps abbreviations together like "Number HEI"

                   // instead of making it "Number H E I"

                     && ((idx < input.Length - 1
                             && !Char.IsUpper(input[idx + 1]))
                         || (idx != 0
                             && !Char.IsUpper(input[idx - 1])))
                     && splitString.Length > 0)
                {
                    splitString += " ";
                }

                splitString += c;
            }

            return splitString;
        }

        public static int CountStringOccurrences(this string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
        }

        public static int CountStringOccurrences(this string text, IEnumerable<string> patterns)
        {
            return patterns.Select(p => text.CountStringOccurrences(p)).Sum();
        }

        public static IEnumerable<string> SplitWords(this string that)
        {
            return Regex.Split(that, @"\W+");
        }

        public static string ToSentenceCase(this string str)
        {
            return Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]));
        }


        public static string QuoteIfWhitespace(this string str)
        {
            if (!str.Any(char.IsWhiteSpace))
            {
                return str;
            }

            return "\"" + str + "\"";
        }

        public static IDictionary<string, string> ParseInitialization(this string s, char valueDelim = '=',
                                                                      char pairDelim = ';')
        {
            var segments = s.Split(new[] { pairDelim }, StringSplitOptions.RemoveEmptyEntries);
            var entries = segments.Select(item => item.Split(new[] { valueDelim }, StringSplitOptions.RemoveEmptyEntries));
            var kvps =
                entries.Select(kvp => new KeyValuePair<string, string>(kvp[0], kvp.Length > 1 ? kvp[1] : string.Empty));
            return kvps.ToDictionary(k => k.Key, v => v.Value);
        }

        public static string AssembleInitialization(char valueDelim, char pairDelim, params Tuple<string, string>[] items)
        {
            var sb = new StringBuilder();
            foreach (var it in items)
            {
                sb.AppendFormat("{0}{1}{2}{3}", it.Item1, valueDelim, it.Item2, pairDelim);
            }
            return sb.ToString();
        }

        public static Stream ToStream(this string that)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(that);
            var stream = new MemoryStream(byteArray);
            return stream;
        }







        public static string ToAscBase64(this string that)
        {
            var toEncodeAsBytes = ASCIIEncoding.ASCII.GetBytes(that);
            return System.Convert.ToBase64String(toEncodeAsBytes);
        }

        public static string FromAscBase64(this string that)
        {
            byte[] encodedDataAsBytes = System.Convert.FromBase64String(that);
            return System.Text.Encoding.ASCII.GetString(encodedDataAsBytes);
        }




    }
}