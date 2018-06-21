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
    public class PassthroughObject : ShapeableObject
    {
        public PassthroughObject()
        {
        }

        protected PassthroughObject(SerializationInfo info,
                                    StreamingContext context)
            : base(info, context)
        {
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            return this.WireUpForInterface(binder.Name, true, ref result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;
            return this.WireUpForInterface(binder.Name, true, ref result);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;
            return this.WireUpForInterface(Invocation.IndexBinderName, true, ref result);
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            return true;
        }
    }
}