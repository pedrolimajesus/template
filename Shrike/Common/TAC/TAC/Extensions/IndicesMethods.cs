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
        /// <summary>
        ///   Gets the indices where the predicate is true.
        /// </summary>
        public static IEnumerable<int> IndicesWhere<T>(this IEnumerable<T> enumeration, Func<T, bool> predicate)
        {
            int i = 0;

            foreach (T item in enumeration)
            {
                if (predicate(item))
                    yield return i;

                i++;
            }
        }

        public static int FirstIndexOf<T>(this IEnumerable<T> enumeration, Func<T, bool> predicate)
        {
            const int notFound = -1;
            var i = 0;
            foreach (var item in enumeration)
            {
                if (predicate(item))
                    return i;

                i++;
            }
            return notFound;
        }

        public static T FirstOrDefault<T>(this IList<T> list)
        {
            if (list.Count == 0)
                return default(T);

            return list[0];
        }

        public static T LastOrDefault<T>(this IList<T> list)
        {
            if (list.Count == 0)
                return default(T);

            return list[list.Count - 1];
        }

        public static T At<T>(this IEnumerable<T> enumeration, int index)
        {
            return enumeration.Skip(index).First();
        }

        public static IEnumerable<T> At<T>(this IEnumerable<T> enumeration, params int[] indices)
        {
            return At(enumeration, (IEnumerable<int>) indices);
        }

        public static IEnumerable<T> At<T>(this IEnumerable<T> enumeration, IEnumerable<int> indices)
        {
            int currentIndex = 0;

            foreach (int index in indices.OrderBy(i => i))
            {
                while (currentIndex != index)
                {
                    enumeration = enumeration.Skip(1);
                    currentIndex++;
                }

                yield return enumeration.First();
            }
        }

        public static IEnumerable<KeyValuePair<int, T>> AsIndexed<T>(this IEnumerable<T> enumeration)
        {
            int i = 0;

            foreach (var item in enumeration)
            {
                yield return new KeyValuePair<int, T>(i++, item);
            }
        }
    }
}