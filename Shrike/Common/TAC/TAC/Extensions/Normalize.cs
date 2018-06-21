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

namespace AppComponents.Extensions.Numeric
{
    public static class LinqExtensions
    {
        public static IEnumerable<double> Normalize<T>(this IEnumerable<T> enumeration)
            where T : struct
        {
            double sum =
                Convert.ToDouble(enumeration.Aggregate(default(T),
                                                       (s, x) => new MetaNumeric<T>(s) + new MetaNumeric<T>(x)));

            return from value in enumeration
                   let normalized = Convert.ToDouble(value)/sum
                   select normalized;
        }
    }
}