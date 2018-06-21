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
        public static bool AllTheSameAs<T1, T2>(this IEnumerable<T1> left,
                                                IEnumerable<T2> right,
                                                Func<T1, T2, bool> comparer)
        {
            using (IEnumerator<T1> leftE = left.GetEnumerator())
            {
                using (IEnumerator<T2> rightE = right.GetEnumerator())
                {
                    bool leftNext = leftE.MoveNext(), rightNext = rightE.MoveNext();

                    while (leftNext && rightNext)
                    {
                        // If one of the items isn't the same...
                        if (!comparer(leftE.Current, rightE.Current))
                            return false;

                        leftNext = leftE.MoveNext();
                        rightNext = rightE.MoveNext();
                    }

                    // If left or right is longer
                    if (leftNext || rightNext)
                        return false;
                }
            }

            return true;
        }
    }
}