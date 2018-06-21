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
using System.Reflection;
using AppComponents.Dynamic.Projection;
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents.Dynamic
{
    public class SignatureKey
    {
        private IEnumerable<int> _hashSequence;

        private SignatureKey(IEnumerable<int> hashSequence)
        {
            _hashSequence = hashSequence;
        }


        public static SignatureKey Create(GetMemberBinder binder)
        {
            return new SignatureKey(
                EnumerableEx.OfThree(
                    MemberTypes.Property.GetHashCode(),
                    binder.Name.GetHashCode(),
                    binder.ReturnType.GetHashCode()));
        }

        public static SignatureKey Create(SetMemberBinder binder)
        {
            return new SignatureKey(
                EnumerableEx.OfThree(
                    MemberTypes.Property.GetHashCode(),
                    binder.Name.GetHashCode(),
                    binder.ReturnType.GetHashCode()));
        }

        public static SignatureKey Create(InvokeMemberBinder binder, object[] args)
        {
            return new SignatureKey(EnumerableEx.OfThree(
                MemberTypes.Method.GetHashCode(),
                binder.Name.GetHashCode(),
                binder.CallInfo.ArgumentCount)
                                        .Concat(args.Select(arg => arg.GetType().GetHashCode())));
        }

        public static SignatureKey Create(MemberProjection projection)
        {
            switch (projection.MemberType)
            {
                case MemberTypes.Property:
                    return new SignatureKey(EnumerableEx.OfThree(
                        MemberTypes.Property.GetHashCode(), projection.Name.GetHashCode(),
                        projection.ReturnType.GetHashCode()));
                    break;

                case MemberTypes.Method:
                    return new SignatureKey(EnumerableEx.OfThree(
                        MemberTypes.Method.GetHashCode(),
                        projection.Name.GetHashCode(),
                        projection.ArgumentCount
                                                ).Concat(projection.ArgumentTypes.Select(at => at.GetHashCode())));
                    break;

                default:
                    return null;
            }
        }

        public static SignatureKey Create(string name, object value)
        {
            Delegate del = value as Delegate;
            if (null == del)
            {
                return new SignatureKey(EnumerableEx.OfThree(
                    MemberTypes.Property.GetHashCode(), name.GetHashCode(), value.GetType().GetHashCode()));
            }
            else
            {
                var mi = del.Method;
                return new SignatureKey(EnumerableEx.OfThree(
                    MemberTypes.Method.GetHashCode(), name.GetHashCode(),
                    mi.GetParameters().Count()).Concat(mi.GetParameters()
                                                           .Select(pm => pm.ParameterType.GetHashCode())));
            }
        }

        public static SignatureKey Create(string name, Type type)
        {
            if (typeof (Delegate).IsAssignableFrom(type))
            {
                Delegate prototype = (Delegate) Activator.CreateInstance(type);
                var mi = prototype.Method;
                return new SignatureKey(EnumerableEx.OfThree(
                    MemberTypes.Method.GetHashCode(), name.GetHashCode(),
                    mi.GetParameters().Count()).Concat(mi.GetParameters()
                                                           .Select(pm => pm.ParameterType.GetHashCode())));
            }
            else
            {
                return new SignatureKey(EnumerableEx.OfThree(
                    MemberTypes.Property.GetHashCode(), name.GetHashCode(), type.GetHashCode()
                                            ));
            }
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof (SignatureKey))
                return false;

            var that = obj as SignatureKey;

            if (null == _hashSequence || null == that._hashSequence)
                return false;

            if (_hashSequence.Count() != that._hashSequence.Count())
                return false;

            return _hashSequence.SequenceEqual(that._hashSequence);
        }

        public override int GetHashCode()
        {
            if (null == _hashSequence)
                return GetType().GetHashCode();
            else
                return Hash.GetCombinedHashCodeForHashesNested(_hashSequence);
        }
    }
}