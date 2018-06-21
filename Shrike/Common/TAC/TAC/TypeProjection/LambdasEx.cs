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

namespace AppComponents.Dynamic.Lambdas
{
    public static class Return<TR>
    {
        public static Func<TR> Arguments(Func<TR> del)
        {
            return del;
        }


        public static ThisFunc<TR> ThisAndArguments(ThisFunc<TR> del)
        {
            return del;
        }


        public static Func<T1, TR> Arguments<T1>(Func<T1, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, TR> ThisAndArguments<T1>(ThisFunc<T1, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, TR> Arguments<T1, T2>(Func<T1, T2, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, TR> ThisAndArguments<T1, T2>(ThisFunc<T1, T2, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, TR> Arguments<T1, T2, T3>(Func<T1, T2, T3, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, T3, TR> ThisAndArguments<T1, T2, T3>(ThisFunc<T1, T2, T3, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, TR> Arguments<T1, T2, T3, T4>(Func<T1, T2, T3, T4, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, T3, T4, TR> ThisAndArguments<T1, T2, T3, T4>(ThisFunc<T1, T2, T3, T4, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, TR> Arguments<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, T3, T4, T5, TR> ThisAndArguments<T1, T2, T3, T4, T5>(
            ThisFunc<T1, T2, T3, T4, T5, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, T6, TR> Arguments<T1, T2, T3, T4, T5, T6>(
            Func<T1, T2, T3, T4, T5, T6, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, T3, T4, T5, T6, TR> ThisAndArguments<T1, T2, T3, T4, T5, T6>(
            ThisFunc<T1, T2, T3, T4, T5, T6, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, T6, T7, TR> Arguments<T1, T2, T3, T4, T5, T6, T7>(
            Func<T1, T2, T3, T4, T5, T6, T7, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, T3, T4, T5, T6, T7, TR> ThisAndArguments<T1, T2, T3, T4, T5, T6, T7>(
            ThisFunc<T1, T2, T3, T4, T5, T6, T7, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, TR> Arguments<T1, T2, T3, T4, T5, T6, T7, T8>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, TR> ThisAndArguments<T1, T2, T3, T4, T5, T6, T7, T8>(
            ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR> Arguments<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9>(ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR> del)
        {
            return del;
        }

        public static ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR> del)
        {
            return del;
        }

        public static ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR> del)
        {
            return del;
        }

        public static ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR> del)
        {
            return del;
        }


        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR> del)
        {
            return del;
        }


        public static ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR>
            ThisAndArguments<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
            ThisFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR> del)
        {
            return del;
        }
    }


    /// <summary>
    ///   Fluent class for writing inline lambdas that return void
    /// </summary>
    public static class ReturnVoid
    {
        public static Action Arguments(Action del)
        {
            return del;
        }


        public static ThisAction ThisAndArguments(ThisAction del)
        {
            return del;
        }


        public static Action<T1> Arguments<T1>(Action<T1> del)
        {
            return del;
        }


        public static ThisAction<T1> ThisAndArguments<T1>(ThisAction<T1> del)
        {
            return del;
        }

        public static Action<T1, T2> Arguments<T1, T2>(Action<T1, T2> del)
        {
            return del;
        }


        public static ThisAction<T1, T2> ThisAndArguments<T1, T2>(ThisAction<T1, T2> del)
        {
            return del;
        }


        public static Action<T1, T2, T3> Arguments<T1, T2, T3>(Action<T1, T2, T3> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3> ThisAndArguments<T1, T2, T3>(ThisAction<T1, T2, T3> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4> Arguments<T1, T2, T3, T4>(Action<T1, T2, T3, T4> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3, T4> ThisAndArguments<T1, T2, T3, T4>(ThisAction<T1, T2, T3, T4> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5> Arguments<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3, T4, T5> ThisAndArguments<T1, T2, T3, T4, T5>(
            ThisAction<T1, T2, T3, T4, T5> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5, T6> Arguments<T1, T2, T3, T4, T5, T6>(
            Action<T1, T2, T3, T4, T5, T6> del)
        {
            return del;
        }

        public static ThisAction<T1, T2, T3, T4, T5, T6> ThisAndArguments<T1, T2, T3, T4, T5, T6>(
            ThisAction<T1, T2, T3, T4, T5, T6> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5, T6, T7> Arguments<T1, T2, T3, T4, T5, T6, T7>(
            Action<T1, T2, T3, T4, T5, T6, T7> del)
        {
            return del;
        }

        public static ThisAction<T1, T2, T3, T4, T5, T6, T7> ThisAndArguments<T1, T2, T3, T4, T5, T6, T7>(
            ThisAction<T1, T2, T3, T4, T5, T6, T7> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5, T6, T7, T8> Arguments<T1, T2, T3, T4, T5, T6, T7, T8>(
            Action<T1, T2, T3, T4, T5, T6, T7, T8> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3, T4, T5, T6, T7, T8> ThisAndArguments<T1, T2, T3, T4, T5, T6, T7, T8>(
            ThisAction<T1, T2, T3, T4, T5, T6, T7, T8> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> Arguments<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9>(ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Arguments<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
            (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> del)
        {
            return del;
        }


        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Arguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> del)
        {
            return del;
        }


        public static ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> ThisAndArguments
            <T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
            ThisAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> del)
        {
            return del;
        }
    }
}