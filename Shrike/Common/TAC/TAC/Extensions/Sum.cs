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
using AppComponents.Extensions.Numeric;

namespace AppComponents.Extensions.EnumerableEx
{
    public static partial class EnumerableExtensions
    {
        public static T Sum<T>(this IEnumerable<T> enumeration)
            where T : struct
        {
            T sum = enumeration.Aggregate(default(T), (s, x) => new MetaNumeric<T>(s) + new MetaNumeric<T>(x));

            return sum;
        }
    }
}