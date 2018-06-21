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

using System.Dynamic;
using System.Linq;

namespace AppComponents.Dynamic
{

    #region Classes

    public class AspectInterceptor<T> : AbstractInterceptor
        where T : class
    {
        private ChainingShapeableExpando _aspectExtensions = new ChainingShapeableExpando();
        private IAspectWeaver _aspectWeaver;

        public AspectInterceptor(object target, IAspectWeaver aspectWeaver)
            : base(target)
        {
            _aspectWeaver = aspectWeaver;
        }


        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var invocation = new Invocation(InvocationKind.Get, binder.Name);
            var before = _aspectWeaver.Before(binder);
            bool ok = true;
            result = null;

            foreach (var aspect in before)
            {
                ok = ok && aspect.InterceptBefore(invocation, OriginalTarget, _aspectExtensions, out result);
            }

            var instead = _aspectWeaver.InsteadOf(binder);
            if (instead.Any())
            {
                foreach (var aspect in instead)
                {
                    ok = ok && aspect.InterceptInstead(invocation, OriginalTarget, _aspectExtensions, out result);
                }
            }
            else
            {
                ok = ok && base.TryGetMember(binder, out result);
            }

            var after = _aspectWeaver.After(binder);
            foreach (var aspect in after)
            {
                ok = ok && aspect.InterceptAfter(invocation, OriginalTarget, _aspectExtensions, out result);
            }

            return ok;
        }


        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] arguments, out object result)
        {
            var invocation = new Invocation(InvocationKind.InvokeMemberUnknown, binder.Name,
                                            TypeFactorization.MaybeRenameArguments(binder.CallInfo, arguments));

            var before = _aspectWeaver.Before(binder);
            var ok = true;
            result = null;

            foreach (var aspect in before)
            {
                ok = ok && aspect.InterceptBefore(invocation, OriginalTarget, _aspectExtensions, out result);
            }

            var instead = _aspectWeaver.InsteadOf(binder);
            if (instead.Any())
            {
                foreach (var aspect in instead)
                {
                    ok = ok && aspect.InterceptInstead(invocation, OriginalTarget, _aspectExtensions, out result);
                }
            }
            else
            {
                ok = ok && base.TryInvokeMember(binder, invocation.Arguments, out result);
            }

            var after = _aspectWeaver.After(binder);
            foreach (var aspect in after)
            {
                ok = ok && aspect.InterceptAfter(invocation, OriginalTarget, _aspectExtensions, out result);
            }

            return ok;
        }


        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var invocation = new Invocation(InvocationKind.Set, binder.Name, value);

            var before = _aspectWeaver.Before(binder);
            bool ok = true;
            object result = null;

            foreach (var aspect in before)
            {
                ok = ok && aspect.InterceptBefore(invocation, OriginalTarget, _aspectExtensions, out result);
            }

            var instead = _aspectWeaver.InsteadOf(binder);
            if (instead.Any())
            {
                foreach (var aspect in instead)
                {
                    ok = ok && aspect.InterceptInstead(invocation, OriginalTarget, _aspectExtensions, out result);
                }
            }
            else
            {
                ok = ok && base.TrySetMember(binder, invocation.Arguments[0]);
            }

            var after = _aspectWeaver.After(binder);
            foreach (var aspect in after)
            {
                ok = ok && aspect.InterceptAfter(invocation, OriginalTarget, _aspectExtensions, out result);
            }

            return ok;
        }
    }

    #endregion Classes
}