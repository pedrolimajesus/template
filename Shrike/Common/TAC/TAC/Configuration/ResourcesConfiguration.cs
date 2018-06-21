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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AppComponents
{
    public class ResourcesConfiguration : DictionaryConfigurationBase
    {
        private string _pattern;

        public ResourcesConfiguration(string pattern)
        {
            _pattern = pattern;
        }

        public ResourcesConfiguration()
        {
        }

        public override void FillDictionary()
        {
            var src = Catalog.Factory.Resolve<StringResourcesCache>();
            IEnumerable<string> keys = src.ResourceNames;
            if (!string.IsNullOrEmpty(_pattern))
            {
                var rx = new Regex(_pattern, RegexOptions.IgnoreCase);
                keys = from key in src.ResourceNames where rx.Match(key).Success select key;
            }

            foreach (var key in keys)
                _configurationCache.TryAdd(key, src[key]);
        }
    }
}