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
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> enumeration, Func<T, T, bool> comparer)
        {
            return Distinct(enumeration, comparer, null);
        }


        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> enumeration, Func<T, T, bool> comparer,
                                                 Func<T, int> hasher)
        {
            return enumeration.Distinct(new EqualityComparer<T> {Comparer = comparer, Hasher = hasher});
        }

        #region Nested type: EqualityComparer

        internal class EqualityComparer<T> : IEqualityComparer<T>
        {
            public Func<T, T, bool> Comparer { get; internal set; }
            public Func<T, int> Hasher { get; internal set; }

            #region IEqualityComparer<T> Members

            bool IEqualityComparer<T>.Equals(T x, T y)
            {
                return Comparer(x, y);
            }

            int IEqualityComparer<T>.GetHashCode(T obj)
            {
                if (Hasher == null)
                    return 0;

                return Hasher(obj);
            }

            #endregion
        }

        #endregion
    }
}