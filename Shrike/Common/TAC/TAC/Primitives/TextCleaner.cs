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
using System.Linq;

namespace AppComponents
{
    public static class TextCleaner
    {
        private static string[] badWords =
            {
                "asshole",
                "a$$hole",
                "bunghole",
                "motherfucker",
                "motherfucking",
                "brotherfucker",
                "unclefucker",
                "fucker",
                "cock",
                "cocksucker",
                "cum",
                "cunnie",
                "cunny",
                "fag",
                "fudgepacker",
                "faggot",
                "nigger",
                "goddamn",
                "damn",
                "shitty",
                "fuck",
                "shit",
                "dick",
                "pussy",
                "pu$$y",
                "penis",
                "cunt",
                "twat",
                "ass hole"
            };

        private static readonly string standIn = "@!$*#!";

        public static bool BadWords(string input)
        {
            return (from w in badWords where input.ToLowerInvariant().Contains(w) select w).Any();
        }

        public static string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            string lower = string.Empty, retval = string.Empty;

            lower = input.ToLowerInvariant();
            retval = input;


            //var words = retval.Split(' ', '\n', '\t', ',', ';', '!', '.', '"', '(', ')', '&');

            int pos = 0;

            do
            {
                int each = 0;
                for (each = 0; each != badWords.Length; each++)
                {
                    pos = lower.IndexOf(badWords[each], StringComparison.OrdinalIgnoreCase);
                    if (pos >= 0)
                        break;
                }

                while (pos != -1)
                {
                    retval = retval.Replace(retval.Substring(pos, badWords[each].Length), standIn);
                    lower = retval.ToLowerInvariant();
                    pos = lower.IndexOf(badWords[each], StringComparison.OrdinalIgnoreCase);
                }
            } while (pos != -1);

            return retval;
        }
    }
}