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

namespace AppComponents.Functional
{
    public static class ZipEnumerable
    {
        public static IEnumerable<T> Return<T>(T value)
        {
            while (true) yield return value;
        }

        public static IEnumerable<Tuple<T1, T2>>
            Merge<T1, T2>
            (
            this IEnumerable<T1> first,
            IEnumerable<T2> second
            )
        {
            return first.Zip(second, Tuple.Create);
        }


        public static IEnumerable<R> Select<T, R>(
            this IEnumerable<T> source,
            Func<T, R> func)
        {
            foreach (var el in source) yield return func(el);
        }
    }


    internal static class ExampleForZipEnumerable
    {
        private static IEnumerable<double> ExampleAveragePricesMerge()
        {
            var stockPrices = new[]
                                  {
                                      //  IBM    APL     TIX     NNTP
                                      new[] {10.0, 11.0, 15.0, 9.0}, // day 1
                                      new[] {4.0, 3.8, 4.1, 3.1}, // day 2
                                      new[] {9.0, 8.0, 18.3, 22.3} // day 3
                                  };

            // sum the columns

            var res = ZipEnumerable.Return(0.0); // provides an initial field to begin summing the 
            // columns against; merging against the 
            // rows in stockPrices will produce rows
            // with the same length.

            foreach (var day in stockPrices)
            {
                res = from pair in res.Merge(day)
                      select pair.Item1 + pair.Item2;
            }

            // result is the average of each column in a new row
            var averagePrices = from sum in res
                                select (sum/stockPrices.Count());

            return averagePrices;
        }


        private static IEnumerable<double> ExampleAveragePricesLINQ()
        {
            var stockPrices = new[]
                                  {
                                      //  IBM    APL     TIX     NNTP
                                      new[] {10.0, 11.0, 15.0, 9.0}, // day 1
                                      new[] {4.0, 3.8, 4.1, 3.1}, // day 2
                                      new[] {9.0, 8.0, 18.3, 22.3} // day 3
                                  };

            // sum the columns

            var res = ZipEnumerable.Return(0.0); // provides an initial field to begin summing the 
            // columns against; merging against the 
            // rows in stockPrices will produce rows
            // with the same length.

            foreach (var day in stockPrices)
            {
                res = from
                          runningSum in res
                      join price in day
                          on 1 equals 1
                      // hacking join this way is the same as Merge(),
                      // meaning we will get runningSum at position x 
                      // coupled with price at position x in the day
                      select runningSum + price;
            }

            // result is the average of each column in a new row
            var averagePrices = from sum in res
                                select (sum/stockPrices.Count());

            return averagePrices;
        }
    }
}