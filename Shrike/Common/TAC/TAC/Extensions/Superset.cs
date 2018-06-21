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
        public static bool Superset<T>(this IEnumerable<T> enumeration, IEnumerable<T> subset)
        {
            return Superset(enumeration, subset, System.Collections.Generic.EqualityComparer<T>.Default.Equals);
        }


        public static bool Superset<T>(this IEnumerable<T> enumeration,
                                       IEnumerable<T> subset,
                                       Func<T, T, bool> equalityComparer)
        {
            using (IEnumerator<T> big = enumeration.GetEnumerator(), small = subset.GetEnumerator())
            {
                big.Reset();
                small.Reset();

                while (big.MoveNext())
                {
                    if (!small.MoveNext())
                        return true;

                    if (!equalityComparer(big.Current, small.Current))
                    {
                        small.Reset();


                        small.MoveNext();


                        if (!equalityComparer(big.Current, small.Current))
                            small.Reset();
                    }
                }


                if (!small.MoveNext())
                    return true;
            }

            return false;
        }
    }
}