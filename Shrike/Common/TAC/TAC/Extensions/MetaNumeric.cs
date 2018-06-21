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
using Ex = System.Linq.Expressions.Expression;

namespace AppComponents.Extensions.Numeric
{
    internal struct MetaNumeric<T> where T : struct
    {
        private static Func<T, T, T> addition, subtraction, multiplication, division;

        private T value;

        static MetaNumeric()
        {
            Type type = typeof (T);
            var left = Ex.Parameter(type, "left");
            var right = Ex.Parameter(type, "right");

            addition = Ex.Lambda<Func<T, T, T>>(Ex.Add(left, right), left, right).Compile();
            subtraction = Ex.Lambda<Func<T, T, T>>(Ex.Subtract(left, right), left, right).Compile();
            multiplication = Ex.Lambda<Func<T, T, T>>(Ex.Multiply(left, right), left, right).Compile();
            division = Ex.Lambda<Func<T, T, T>>(Ex.Divide(left, right), left, right).Compile();
        }

        public MetaNumeric(T value)
        {
            this.value = value;
        }

        public static T operator +(MetaNumeric<T> left, MetaNumeric<T> right)
        {
            return addition(left.value, right.value);
        }

        public static T operator -(MetaNumeric<T> left, MetaNumeric<T> right)
        {
            return subtraction(left.value, right.value);
        }

        public static T operator *(MetaNumeric<T> left, MetaNumeric<T> right)
        {
            return multiplication(left.value, right.value);
        }

        public static T operator /(MetaNumeric<T> left, MetaNumeric<T> right)
        {
            return division(left.value, right.value);
        }
    }
}