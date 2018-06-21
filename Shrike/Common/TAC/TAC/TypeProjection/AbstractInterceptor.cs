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

using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CSharp.RuntimeBinder;

namespace AppComponents.Dynamic
{

    #region Interfaces

    public interface IInterceptor
    {
        object OriginalTarget { get; }
    }

    #endregion Interfaces

    #region Classes

    public abstract class AbstractInterceptor : ShapeableObject, IInterceptor
    {
        protected AbstractInterceptor(object target)
        {
            OriginalTarget = target;
        }

        protected AbstractInterceptor(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            OriginalTarget = info.GetValue<IDictionary<string, object>>("Target");
        }

        protected virtual object CallTarget
        {
            get { return OriginalTarget; }
        }

        protected object OriginalTarget { get; set; }

        #region IInterceptor Members

        object IInterceptor.OriginalTarget
        {
            get { return OriginalTarget; }
        }

        #endregion

        public bool Equals(AbstractInterceptor other)
        {
            if (ReferenceEquals(null, other))
                return ReferenceEquals(null, CallTarget);
            if (ReferenceEquals(this, other))
                return true;
            return Equals(other.CallTarget, CallTarget);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return ReferenceEquals(null, CallTarget);
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof (AbstractInterceptor))
                return false;
            return Equals((AbstractInterceptor) obj);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            if (!KnownInterfaces.Any())
            {
                var dynamicMemberNames = InvocationBinding.GetMemberNames(CallTarget, dynamicOnly: true);
                if (!dynamicMemberNames.Any())
                {
                    return InvocationBinding.GetMemberNames(CallTarget);
                }
            }
            return base.GetDynamicMemberNames();
        }

        public override int GetHashCode()
        {
            return (CallTarget != null ? CallTarget.GetHashCode() : 0);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Target", OriginalTarget);
        }

        public override string ToString()
        {
            return string.Format("Target: {0}, CallTarget: {1}", OriginalTarget, CallTarget);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (CallTarget == null)
            {
                result = null;
                return false;
            }

            object[] theArguments = TypeFactorization.MaybeRenameArguments(binder.CallInfo, indexes);

            result = InvocationBinding.InvokeGetIndex(CallTarget, theArguments);
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (CallTarget == null)
            {
                result = null;
                return false;
            }

            if (InvocationBinding.InvokeIsEvent(CallTarget, binder.Name))
            {
                result = new InterceptorAddRemove();
                return true;
            }

            result = InvocationBinding.InvokeGet(CallTarget, binder.Name);

            return true;
        }

        public override bool TryInvoke(InvokeBinder binder, object[] arguments, out object result)
        {
            if (CallTarget == null)
            {
                result = null;
                return false;
            }

            var theArguments = TypeFactorization.MaybeRenameArguments(binder.CallInfo, arguments);

            try
            {
                result = InvocationBinding.Invoke(CallTarget, theArguments);
            }
            catch (RuntimeBinderException)
            {
                result = null;
                try
                {
                    InvocationBinding.InvokeAction(CallTarget, theArguments);
                }
                catch (RuntimeBinderException)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] arguments, out object result)
        {
            if (CallTarget == null)
            {
                result = null;
                return false;
            }

            object[] theArguments = TypeFactorization.MaybeRenameArguments(binder.CallInfo, arguments);

            try
            {
                result = InvocationBinding.InvokeMember(CallTarget, binder.Name, theArguments);
            }
            catch (RuntimeBinderException)
            {
                result = null;
                try
                {
                    InvocationBinding.InvokeMemberAction(CallTarget, binder.Name, theArguments);
                }
                catch (RuntimeBinderException)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (CallTarget == null)
            {
                return false;
            }

            var combinedArguments = indexes.Concat(new[] {value}).ToArray();
            object[] tArgs = TypeFactorization.MaybeRenameArguments(binder.CallInfo, combinedArguments);

            InvocationBinding.InvokeSetIndex(CallTarget, tArgs);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (CallTarget == null)
            {
                return false;
            }

            if (InvocationBinding.InvokeIsEvent(CallTarget, binder.Name) && value is InterceptorAddRemove)
            {
                var theValue = value as InterceptorAddRemove;

                if (theValue.IsAdding)
                {
                    InvocationBinding.InvokeAddAssign(CallTarget, binder.Name, theValue.Delegate);
                }
                else
                {
                    InvocationBinding.InvokeSubtractAssign(CallTarget, binder.Name, theValue.Delegate);
                }

                return true;
            }

            InvocationBinding.InvokeSet(CallTarget, binder.Name, value);

            return true;
        }
    }

    #endregion Classes
}