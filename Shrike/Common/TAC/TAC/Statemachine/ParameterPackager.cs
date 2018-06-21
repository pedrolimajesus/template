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

namespace AppComponents
{
    internal static class ParameterPackager
    {
        public static object Unpack(object[] args, Type argType, int index)
        {
            if (args.Length <= index)
                throw new ArgumentException(
                    string.Format("not enough parameters for arg type: {0} at position {1}", argType, index));

            var arg = args[index];

            if (arg != null && !argType.IsAssignableFrom(arg.GetType()))
                throw new ArgumentException(
                    string.Format("wrong type of argument: {0}. Have {1} but must have {2}", index, arg.GetType(),
                                  argType));

            return arg;
        }

        public static TArg Unpack<TArg>(object[] args, int index)
        {
            return (TArg) Unpack(args, typeof (TArg), index);
        }


        public static void Validate(object[] args, Type[] expected)
        {
            if (args.Length > expected.Length)
                throw new ArgumentException(
                    string.Format("Too many arguments, expected {0}, but there are {1}", expected.Length, args.Length));

            for (int i = 0; i < expected.Length; ++i)
                Unpack(args, expected[i], i);
        }
    }
}