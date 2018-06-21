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
using System.Linq;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using AppComponents.Extensions.EnumEx;

namespace AppComponents.ControlFlow
{
    // Examples: 
    // -param1 value1 --param2 /param3:"Test-:-work" 
    //   /param4=happy -param5 '--=nice=--'
    public class Arguments
    {
        private readonly StringDictionary _parameters;


        public Arguments(string[] Args)
        {
            _parameters = new StringDictionary();

            var splitter = new Regex(@"^-{1,2}|^/|=|:",
                                    RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var remover = new Regex(@"^['""]?(.*?)['""]?$",
                                    RegexOptions.IgnoreCase | RegexOptions.Compiled);

            string parameter = null;


            foreach (var txt in Args)
            {
                var parts = splitter.Split(txt, 3).ToArray();

                switch (parts.Length)
                {
                    // Found a value (for the last parameter 
                    // found (space separator))
                    case 1:
                        if (parameter != null)
                        {
                            if (!_parameters.ContainsKey(parameter))
                            {
                                parts[0] =
                                    remover.Replace(parts[0], "$1");

                                _parameters.Add(parameter.ToLowerInvariant(), parts[0]);
                            }
                            parameter = null;
                        }

                        break;


                    case 2:

                        if (parameter != null)
                        {
                            if (!_parameters.ContainsKey(parameter))
                                _parameters.Add(parameter.ToLowerInvariant(), "true");
                        }
                        parameter = parts[1];
                        break;

                    // given value
                    case 3:

                        if (parameter != null)
                        {
                            if (!_parameters.ContainsKey(parameter))
                                _parameters.Add(parameter.ToLowerInvariant(), "true");
                        }

                        parameter = parts[1];


                        if (!_parameters.ContainsKey(parameter))
                        {
                            parts[2] = remover.Replace(parts[2], "$1");
                            _parameters.Add(parameter.ToLowerInvariant(), parts[2]);
                        }

                        parameter = null;
                        break;
                }
            }

            if (parameter != null)
            {
                if (!_parameters.ContainsKey(parameter))
                    _parameters.Add(parameter.ToLowerInvariant(), "true");
            }
        }

        public bool Has(string param)
        {
            return _parameters.ContainsKey(param.ToLowerInvariant());
        }

        public bool Has(Enum param)
        {
            return Has(param.EnumName());
        }

        public string this[string param]
        {
            get { return (_parameters[param.ToLowerInvariant()]); }
        }

        public string this[Enum param]
        {
            get { return (_parameters[param.EnumName().ToLowerInvariant()]); }
        }

        public static IEnumerable<string> ParseArguments(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                yield break;

            var sb = new StringBuilder();
            var inQuote = false;
            foreach (var c in commandLine)
            {
                if (c == '"' && !inQuote)
                {
                    inQuote = true;
                    continue;
                }
                if (c != '"' && !(char.IsWhiteSpace(c) && !inQuote))
                {
                    sb.Append(c);
                    continue;
                }

                if (sb.Length > 0)
                {
                    var result = sb.ToString();
                    sb.Clear();
                    inQuote = false;
                    yield return result;
                }
            }
            if (sb.Length > 0)
                yield return sb.ToString();
        }
    }
}