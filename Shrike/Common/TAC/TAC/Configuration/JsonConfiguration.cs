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
using Newtonsoft.Json;

namespace AppComponents
{
    /// <summary>
    ///   file format like: string json = @"{""key1"":""value1"",""key2"":""value2""}";
    /// </summary>
    public class JsonConfiguration : DictionaryConfigurationBase
    {
        private string _jsonContent;
        private Dictionary<string, string> _values;

        public JsonConfiguration(string jsonContent)
        {
            _jsonContent = jsonContent;
            _values = JsonConvert.DeserializeObject<Dictionary<string, string>>(_jsonContent);
        }

        public JsonConfiguration(Uri jsonFile)
        {
            _jsonContent = File.ReadAllText(jsonFile.ToString());
            _values = JsonConvert.DeserializeObject<Dictionary<string, string>>(_jsonContent);
        }

        public JsonConfiguration(StringResourcesCache src)
        {
            foreach (var file in src.ResourceNames.Where(rn => rn.EndsWith(".json")))
            {
                _jsonContent = src[file];
                var moreValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(_jsonContent);
                foreach (var k in moreValues.Keys)
                    _values.Add(k, moreValues[k]);
            }
        }

        public override void FillDictionary()
        {
            foreach (var k in _values.Keys)
                _configurationCache.TryAdd(k, _values[k]);
        }
    }
}