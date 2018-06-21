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
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CSharp.RuntimeBinder;

namespace AppComponents.Dynamic
{

    #region Classes

    [Serializable]
    public class DynamicPropertiesToReflectablePropertiesProxy : AbstractInterceptor
    {
        public DynamicPropertiesToReflectablePropertiesProxy(object target)
            : base(target)
        {
        }

        protected DynamicPropertiesToReflectablePropertiesProxy(SerializationInfo info,
                                                                StreamingContext context)
            : base(info, context)
        {
        }

        public static T Create<T>(object target) where T : class
        {
            return new DynamicPropertiesToReflectablePropertiesProxy(target).DressedAs<T>();
        }

        public static dynamic Create(object target)
        {
            return new DynamicPropertiesToReflectablePropertiesProxy(target);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (base.TryGetIndex(binder, indexes, out result))
            {
                return this.WireUpForInterface(Invocation.IndexBinderName, true, ref result);
            }
            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (base.TryGetMember(binder, out result))
            {
                return this.WireUpForInterface(binder.Name, true, ref result);
            }
            return false;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (!base.TryInvokeMember(binder, args, out result))
            {
                result = InvocationBinding.InvokeGet(CallTarget, binder.Name);
                if (result == null)
                    return false;
                var functor = result as Delegate;
                if (!binder.CallInfo.ArgumentNames.Any() && null != functor)
                {
                    try
                    {
                        result = this.InvokeMethodDelegate(functor, args);
                    }
                    catch (RuntimeBinderException)
                    {
                        return false;
                    }
                }
                try
                {
                    result = InvocationBinding.Invoke(result,
                                                      TypeFactorization.MaybeRenameArguments(binder.CallInfo, args));
                }
                catch (RuntimeBinderException)
                {
                    return false;
                }
            }

            return this.WireUpForInterface(binder.Name, true, ref result);
        }
    }

    #endregion Classes
}