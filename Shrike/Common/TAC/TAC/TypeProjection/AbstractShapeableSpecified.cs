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
using AppComponents.Extensions.SerializationEx;
using Microsoft.CSharp.RuntimeBinder;

namespace AppComponents.Dynamic
{
    public class ShapeableSpecified : ShapeableObject, ICloneable
    {
        protected Dictionary<SignatureKey, object> _dynamicMembers;
        protected ThisAction _initializer;
        protected HashSet<string> _memberNames;
        protected Dictionary<SignatureKey, SignatureKey> _nestedTargets;
        protected Dictionary<SignatureKey, MemberProjection> _specification;


        public ShapeableSpecified()
        {
            _dynamicMembers = new Dictionary<SignatureKey, object>();
            _nestedTargets = new Dictionary<SignatureKey, SignatureKey>();
            _specification = new Dictionary<SignatureKey, MemberProjection>();
            _memberNames = new HashSet<string>();

            if (null != _initializer)
                _initializer(this);
        }

        public ThisAction Initializer
        {
            set { _initializer = value; }
        }

        public IEnumerable<MemberProjection> Specification
        {
            get { return _specification.Values; }
        }

        #region ICloneable Members

        public object Clone()
        {
            // cloning: property data gets serialized to / fro, method delegates get cloned
            var clone = new ShapeableSpecified();
            _memberNames.ForEach(mn => clone._memberNames.Add(mn));
            _specification.ForEach(sp => clone._specification.Add(sp.Key, sp.Value));
            if (null != _initializer)
                clone._initializer = (ThisAction) _initializer.Clone();

            MemberDictionaryClone(_dynamicMembers, clone._dynamicMembers);
            clone._nestedTargets.AddRange(_nestedTargets);

            return clone;
        }

        #endregion

        public virtual ShapeableSpecified AddMember(string name, object initialValue)
        {
            var sk = SignatureKey.Create(name, initialValue);
            _dynamicMembers.Add(sk, initialValue);
            _specification.Add(sk, new MemberProjection(name, initialValue));
            return this;
        }

        public virtual ShapeableSpecified AddMember(MemberProjection mp)
        {
            var sk = SignatureKey.Create(mp);
            _specification.Add(sk, mp);
            switch (mp.MemberType)
            {
                case MemberTypes.Property:
                    object defaultValue = null;
                    if (Catalog.Factory.CanResolve(SpecialFactoryContexts.Prototype, mp.ReturnType))
                    {
                        defaultValue = Catalog.Factory.Resolve(SpecialFactoryContexts.Prototype, mp.ReturnType);
                    }
                    else
                    {
                        defaultValue = Activator.CreateInstance(mp.ReturnType);
                    }

                    _dynamicMembers.Add(sk, defaultValue);
                    break;

                case MemberTypes.Method:
                    _dynamicMembers.Add(sk, mp.Implementation);

                    break;
            }

            return this;
        }

        public virtual ShapeableSpecified AddMember(MemberProjection mp, object initialValue)
        {
            var sk = SignatureKey.Create(mp);
            _specification.Add(sk, mp);
            _dynamicMembers.Add(SignatureKey.Create(mp), initialValue);
            return this;
        }

        public virtual ShapeableSpecified AddMember(string name, Type type, string protoTypeFactoryContext = null)
        {
            if (String.IsNullOrEmpty(protoTypeFactoryContext))
                protoTypeFactoryContext = SpecialFactoryContexts.Prototype.ToString();

            var sk = SignatureKey.Create(name, type);
            object instancePrototype = null;

            if (Catalog.Factory.CanResolve(protoTypeFactoryContext, type))
            {
                instancePrototype = Catalog.Factory.Resolve(protoTypeFactoryContext, type);
            }
            else
            {
                instancePrototype = Activator.CreateInstance(type);
            }

            _dynamicMembers.Add(sk, instancePrototype);
            _specification.Add(sk, new MemberProjection(name, type));
            return this;
        }

        public virtual ShapeableSpecified ExposeMemberInner(string memberName, Type memberType, MemberProjection mp)
        {
            if (!_memberNames.Contains(memberName))
            {
                throw new ArgumentException(
                    string.Format("ShapeableSpecified cannot expose part of member {0}, that member doesn't exist.",
                                  memberName));
            }

            var sk = SignatureKey.Create(mp);

            switch (mp.MemberType)
            {
                case MemberTypes.Property:
                    {
                        var pi = memberType.GetProperty(mp.Name);
                        if (null == pi)
                            throw new ArgumentException(string.Format("Member {0} does not have property {1}",
                                                                      memberName, mp.Name));
                    }
                    break;

                case MemberTypes.Method:
                    {
                        var mi = memberType.GetMethod(mp.Name, mp.ArgumentTypes.ToArray());
                        if (null == mi)
                            throw new ArgumentException(string.Format("Member {0} does not have property {1}",
                                                                      memberName, mp.Name));
                    }
                    break;
            }

            _nestedTargets.Add(sk, SignatureKey.Create(memberName, memberType));
            mp.NestedName = memberName;
            _specification.Add(sk, mp);
            _memberNames.Add(memberName);

            return this;
        }

        private static void MemberDictionaryClone(
            Dictionary<SignatureKey, object> source,
            Dictionary<SignatureKey, object> target)
        {
            foreach (var item in source)
            {
                if (item.Value is Delegate)
                {
                    var del = item.Value as Delegate;
                    var copy = del.Clone();
                    target.Add(item.Key, copy);
                }
                else
                {
                    var copy = SerializationExtensions.FromBinary(item.Value.ToBinary());
                    target.Add(item.Key, copy);
                }
            }
        }

        public bool Equals(ShapeableSpecified other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other._dynamicMembers, _dynamicMembers) && Equals(other._specification, _specification);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ShapeableSpecified)) return _dynamicMembers.Equals(obj);
            return Equals((((ShapeableSpecified) obj)));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return base.GetDynamicMemberNames().Concat(_memberNames).Distinct();
        }

        public override int GetHashCode()
        {
            return _dynamicMembers.GetHashCode();
        }

        public override string ToString()
        {
            return _dynamicMembers.ToString();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var sk = SignatureKey.Create(binder);
            SignatureKey nested = null;
            if (_nestedTargets.TryGetValue(sk, out nested))
            {
                var member = _dynamicMembers[nested];
                result = InvocationBinding.InvokeGet(member, binder.Name);
                return this.WireUpForInterface(binder.Name, true, ref result);
            }

            if (_dynamicMembers.TryGetValue(sk, out result))
            {
                return this.WireUpForInterface(binder.Name, true, ref result);
            }

            result = null;
            return this.WireUpForInterface(binder.Name, false, ref result);
        }


        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var sk = SignatureKey.Create(binder, args);
            SignatureKey nested = null;
            if (_nestedTargets.TryGetValue(sk, out nested))
            {
                var member = _dynamicMembers[nested];
                try
                {
                    result = InvocationBinding.InvokeMember(member, binder.Name, args);
                }
                catch (RuntimeBinderException)
                {
                    result = null;
                    return false;
                }

                return this.WireUpForInterface(binder.Name, true, ref result);
            }

            if (_dynamicMembers.TryGetValue(sk, out result))
            {
                var functor = result as Delegate;
                if (result == null)
                    return false;
                if (!binder.CallInfo.ArgumentNames.Any() && functor != null)
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
                else
                {
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
            return this.WireUpForInterface(binder.Name, false, ref result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            object tOldValue;

            var sk = SignatureKey.Create(binder);
            SignatureKey nested = null;
            if (_nestedTargets.TryGetValue(sk, out nested))
            {
                var member = _dynamicMembers[nested];
                InvocationBinding.InvokeSet(member, binder.Name, value);
            }
            else if (!_dynamicMembers.TryGetValue(sk, out tOldValue) || value != tOldValue)
            {
                _dynamicMembers[sk] = value;
            }

            return true;
        }
    }
}