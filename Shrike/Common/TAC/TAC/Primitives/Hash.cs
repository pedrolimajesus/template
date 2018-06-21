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
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents
{
    using System;

    public static class Hash
    {

        public static UInt64 KnuthHash(string read)
        {
            UInt64 hashedValue = 3074457345618258791ul;
            for (int i = 0; i < read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }

        public static int GetCombinedHashCodeForValCollection<T>(IEnumerable<T> inputs) 
        {
            //if (inputs.Any(a=>a == null)) throw new ArgumentOutOfRangeException("inputs");
            return GetCombinedHashCodeForHashesNested(inputs.Select(h => h.GetHashCode()));
        }

        public static int GetCombinedHashCodeForCollection<T>(IEnumerable<T> inputs) where T: class
        {
            //if (inputs.Any(a=>a == null)) throw new ArgumentOutOfRangeException("inputs");
            return GetCombinedHashCodeForHashesNested(inputs.Select(h => null == h ? 1: h.GetHashCode()));
        }

        public static int GetCombinedHashCodeForHashesNested(IEnumerable<int> inputs)
        {
            int hash = 17;
            inputs.ForEach(i => hash = hash*31 + i.GetHashCode());
            return hash;
        }

        public static int GetCombinedHashCode<T>(params T[] inputs) where T : class
        {
            return GetCombinedHashCodeForCollection(inputs);
        }

        public static int GetCombinedHashCodeForHashes(params int[] inputs)
        {
            return GetCombinedHashCodeForHashesNested(inputs);
        }
    }
}