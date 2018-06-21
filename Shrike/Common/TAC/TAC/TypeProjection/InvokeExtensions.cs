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
using AppComponents.Dynamic;

namespace AppComponents.InvokeExtensions
{
    public static class InvokeExtensions
    {
        public static InvocationContext WithContext(this object target, Type context)
        {
            return new InvocationContext(target, context);
        }


        public static InvocationContext WithContext<TContext>(this object target)
        {
            return new InvocationContext(target, typeof (TContext));
        }


        public static InvocationContext WithContext(this object target, object context)
        {
            return new InvocationContext(target, context);
        }


        public static InvocationContext WithStaticContext(this Type target, object context = null)
        {
            return new InvocationContext(target, true, context);
        }


        public static InvokeMemberByName WithGenericArguments(this string name, params Type[] genericArgs)
        {
            return new InvokeMemberByName(name, genericArgs);
        }


        public static MethodInvocationArgument WithArgumentName(this object argument, string name)
        {
            return new MethodInvocationArgument(name, argument);
        }
    }
}