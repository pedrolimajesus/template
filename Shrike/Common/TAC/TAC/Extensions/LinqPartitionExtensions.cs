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
    public static class EnumerablePartitionExtensions
    {
        public static IEnumerable<T> TakeFromCurrent<T>(this IEnumerator<T> enumerator, int count)
        {
            while (count > 0)
            {
                yield return enumerator.Current;
                if (--count > 0 && !enumerator.MoveNext()) yield break;
            }
        }

        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> seq, int partitionSize)
        {
            var enumerator = seq.GetEnumerator();

            while (enumerator.MoveNext())
            {
                yield return enumerator.TakeFromCurrent(partitionSize);
            }
        }

        public static IEnumerable<IEnumerable<T>> SplitBetween<T>(this IEnumerable<T> seq, Func<T, T, bool> detectSplit)
        {
            var batch = new List<T>();
            T last = default(T);
            bool first = true;
            foreach (var item in seq)
            {
                if (first)
                {
                    batch.Add(item);
                    first = true;
                }
                else
                {
                    if (detectSplit(last, item))
                    {
                        yield return batch;
                        batch = new List<T>();
                    }

                    batch.Add(item);
                }


                last = item;
            }

            if (batch.Count != 0)
                yield return batch;
        }

        public static IEnumerable<IEnumerable<T>> InBatchesOf<T>(this IEnumerable<T> items, int batchSize)
        {
            List<T> batch = new List<T>(batchSize);
            foreach (var item in items)
            {
                batch.Add(item);

                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>();
                }
            }

            if (batch.Count != 0)
            {
                //can't be batch size or would've yielded above
                batch.TrimExcess();
                yield return batch;
            }
        }
    }
}