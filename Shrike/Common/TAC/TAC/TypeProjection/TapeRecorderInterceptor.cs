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
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;

namespace AppComponents.Dynamic
{
    [Serializable]
    public class TapeRecorderInterceptor : AbstractInterceptor
    {
        public TapeRecorderInterceptor()
            : base(new PassthroughObject())
        {
            Recording = new List<Invocation>();
        }

        public TapeRecorderInterceptor(object target)
            : base(target)
        {
            Recording = new List<Invocation>();
        }

        protected TapeRecorderInterceptor(SerializationInfo info,
                                          StreamingContext context)
            : base(info, context)
        {
            Recording = info.GetValue<IList<Invocation>>("Recording");
        }

        public IList<Invocation> Recording { get; protected set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Recording", Recording);
        }

        public T ReplayOn<T>(T target)
        {
            foreach (var eachInvocation in Recording)
            {
                eachInvocation.InvokeWithStoredArgs(target);
            }

            return target;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (base.TryGetMember(binder, out result))
            {
                Recording.Add(new Invocation(InvocationKind.Get, binder.Name));
                return true;
            }
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (base.TrySetMember(binder, value))
            {
                Recording.Add(new Invocation(InvocationKind.Set, binder.Name, value));
                return true;
            }
            return false;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (base.TryInvokeMember(binder, args, out result))
            {
                Recording.Add(new Invocation(InvocationKind.InvokeMemberUnknown, binder.Name,
                                             TypeFactorization.MaybeRenameArguments(binder.CallInfo, args)));
                return true;
            }
            return false;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (base.TryGetIndex(binder, indexes, out result))
            {
                Recording.Add(new Invocation(InvocationKind.GetIndex, Invocation.IndexBinderName,
                                             TypeFactorization.MaybeRenameArguments(binder.CallInfo, indexes)));
                return true;
            }
            return false;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (base.TrySetIndex(binder, indexes, value))
            {
                var combinedArguments = indexes.Concat(new[] {value}).ToArray();
                Recording.Add(new Invocation(InvocationKind.GetIndex, Invocation.IndexBinderName,
                                             TypeFactorization.MaybeRenameArguments(binder.CallInfo, combinedArguments)));
                return true;
            }
            return false;
        }
    }
}