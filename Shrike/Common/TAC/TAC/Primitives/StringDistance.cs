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
using System.Linq;

namespace AppComponents
{
    public static class StringDistance
    {
        public static int DLDistance(string a, string b)
        {
            return DamerauLevenshteinDistance(a.AsEnumerable().Select(c => Convert.ToInt32(c)).ToArray(),
                                              b.AsEnumerable().Select(c => Convert.ToInt32(c)).ToArray(), int.MaxValue);
        }

        public static int SquaredEuclidian(string a, string b)
        {
            if (a.Length != b.Length)
            {
                if (a.Length > b.Length)
                    b = b.PadRight(a.Length, '\n');
                else
                    a = a.PadRight(b.Length, '\n');
            }

            return a.Zip(b, (x, y) => (x - y)*(x - y)).Sum();
        }

        public static int Euclidian(string a, string b)
        {
            return (int) Math.Sqrt(SquaredEuclidian(a, b));
        }

        public static int DMGDistance(string a, string b)
        {
            if (a.Length != b.Length)
            {
                if (a.Length > b.Length)
                    b = b.PadRight(a.Length, '\n');
                else
                    a = a.PadRight(b.Length, '\n');
            }

            int distance = 0;
            for (int i = 0; i != a.Length; i++)
            {
                int weight = a.Length - i;
                distance += weight*Math.Abs(a[i] - b[i]);
            }

            return distance;
        }


        public static int DamerauLevenshteinDistance(int[] source, int[] target, int threshold)
        {
            int length1 = source.Length;
            int length2 = target.Length;


            if (Math.Abs(length1 - length2) > threshold)
            {
                return int.MaxValue;
            }


            if (length1 > length2)
            {
                Swap(ref target, ref source);
                Swap(ref length1, ref length2);
            }

            int maxi = length1;
            int maxj = length2;

            int[] dCurrent = new int[maxi + 1];
            int[] dMinus1 = new int[maxi + 1];
            int[] dMinus2 = new int[maxi + 1];
            int[] dSwap;

            for (int i = 0; i <= maxi; i++)
            {
                dCurrent[i] = i;
            }

            int jm1 = 0, im1 = 0, im2 = -1;

            for (int j = 1; j <= maxj; j++)
            {
                // Rotate
                dSwap = dMinus2;
                dMinus2 = dMinus1;
                dMinus1 = dCurrent;
                dCurrent = dSwap;

                // Initialize
                int minDistance = int.MaxValue;
                dCurrent[0] = j;
                im1 = 0;
                im2 = -1;

                for (int i = 1; i <= maxi; i++)
                {
                    int cost = source[im1] == target[jm1] ? 0 : 1;

                    int del = dCurrent[im1] + 1;
                    int ins = dMinus1[i] + 1;
                    int sub = dMinus1[im1] + cost;


                    int min = (del > ins) ? (ins > sub ? sub : ins) : (del > sub ? sub : del);

                    if (i > 1 && j > 1 && source[im2] == target[jm1] && source[im1] == target[j - 2])
                        min = Math.Min(min, dMinus2[im2] + cost);

                    dCurrent[i] = min;
                    if (min < minDistance)
                    {
                        minDistance = min;
                    }
                    im1++;
                    im2++;
                }
                jm1++;
                if (minDistance > threshold)
                {
                    return int.MaxValue;
                }
            }

            int result = dCurrent[maxi];
            return (result > threshold) ? int.MaxValue : result;
        }

        private static void Swap<T>(ref T arg1, ref T arg2)
        {
            T temp = arg1;
            arg1 = arg2;
            arg2 = temp;
        }
    }
}