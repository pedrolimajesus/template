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

namespace AppComponents.Extensions.EnumerableEx
{
    public static partial class EnumerableExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
                                                    Func<TKey, TValue> valueFactory)
        {
            var value = default(TValue);
            if (!dictionary.TryGetValue(key, out value))
            {
                lock (dictionary)
                {
                    if (!dictionary.TryGetValue(key, out value))
                    {
                        value = valueFactory(key);
                        dictionary[key] = value;
                    }
                }
            }

            return value;
        }

        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
                                                  IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            foreach (var pair in pairs)
            {
                dictionary.Add(pair.Key, pair.Value);
            }
        }

        public static void RemoveValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TValue value)
        {
            foreach (var key in (from pair in dictionary
                                 where
                                     System.Collections.Generic.EqualityComparer<TValue>.Default.Equals(value,
                                                                                                        pair.Value)
                                 select pair.Key).ToArray())
            {
                dictionary.Remove(key);
            }
        }

        public static void RemoveValueRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
                                                          IEnumerable<TValue> values)
        {
            foreach (var value in values.ToArray())
            {
                RemoveValue(dictionary, value);
            }
        }

        public static void RemoveRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys)
        {
            foreach (var key in keys.ToArray())
            {
                dictionary.Remove(key);
            }
        }
    }
}