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
using System.Dynamic;
using System.Runtime.Serialization;

namespace AppComponents.Dynamic
{
    [Serializable]
    public class LateBindingInterceptor : AbstractInterceptor
    {
        public LateBindingInterceptor(Type type)
            : base(type)
        {
        }

        public LateBindingInterceptor(string typeName)
            : base(Type.GetType(typeName, false))
        {
        }

        public LateBindingInterceptor(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public dynamic @new
        {
            get { return new ConstuctorInterceptor((Type) OriginalTarget); }
        }

        public bool IsAvailableAtRuntime
        {
            get { return OriginalTarget != null; }
        }

        protected override object CallTarget
        {
            get { return InvocationContext.CreateStatic((Type) OriginalTarget); }
        }

        #region Nested type: ConstuctorInterceptor

        public class ConstuctorInterceptor : DynamicObject
        {
            private readonly Type _type;

            internal ConstuctorInterceptor(Type type)
            {
                _type = type;
            }

            public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
            {
                result = InvocationBinding.CreateInstance(_type,
                                                          TypeFactorization.MaybeRenameArguments(binder.CallInfo, args));
                return true;
            }
        }

        #endregion
    }
}