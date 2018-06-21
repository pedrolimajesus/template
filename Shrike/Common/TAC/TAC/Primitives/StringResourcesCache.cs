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

namespace AppComponents
{
    public class StringResourcesCache
    {
        private Dictionary<string, string> _stringCache = new Dictionary<string, string>();

        private string[] textResources = {".txt", ".json", ".xml", ".xhtml", ".html"};


        public StringResourcesCache()
        {
            //var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !a.FullName.StartsWith("System.") && !a.FullName.StartsWith("Microsoft.") && !a.FullName.StartsWith("DotNet"));

            foreach (var a in assemblies)
            {
                var resourceNames = from n in a.GetManifestResourceNames()
                                    where textResources.Any(tr => n.EndsWith(tr))
                                    select n;

                foreach (var rn in resourceNames)
                {
                    using (Stream resS = a.GetManifestResourceStream(rn))
                    {
                        StreamReader sr = new StreamReader(resS);
                        var data = sr.ReadToEnd();
                        if (!_stringCache.ContainsKey(rn))
                        {
                            _stringCache.Add(rn, data);
                        }
                        else
                        {
                            var newName = a.GetName() + "/" + rn;
                            _stringCache.Add(newName, data);
                        }
                    }
                }
            }
        }

        public IEnumerable<string> ResourceNames
        {
            get { return _stringCache.Keys.ToArray(); }
        }


        public string this[string key]
        {
            get { return _stringCache[key]; }
        }
    }
}