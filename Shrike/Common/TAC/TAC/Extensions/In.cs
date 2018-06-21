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

namespace AppComponents.Extensions.EnumerableEx
{
    public static partial class EnumerableExtensions
    {
        public static bool In<T>(this T source, params T[] values) where T : IEquatable<T>
        {
            return In(source, ((IEnumerable<T>) values));
        }


        public static bool In<T>(this T source, IEnumerable<T> values) where T : IEquatable<T>
        {
            foreach (T listValue in values)
            {
                if ((default(T).Equals(source) && default(T).Equals(listValue)) ||
                    (!default(T).Equals(source) && source.Equals(listValue)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}