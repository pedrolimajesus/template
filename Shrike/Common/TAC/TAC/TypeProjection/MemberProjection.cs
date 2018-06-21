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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.Extensions.Predicate;

namespace AppComponents.Dynamic.Projection
{
    public class MemberProjection
    {
        private const int _magicZero = 39874292;
        private const int _magicOne = 1237982987;
        private int _argumentCount;
        private IEnumerable<string> _argumentNames;
        private IEnumerable<Type> _argumentTypes;
        private int _cachedNamesHC = _magicOne;
        private bool _cachedNamesHCInit;
        private int _cachedTypesHC = _magicOne;
        private Delegate _implementation;
        private MemberInfo _memberInfo;
        private MemberTypes _memberType;
        private Type _returnType;

        public MemberProjection(string name, object value)
        {
            Name = name;
            IsPublic = true;

            Delegate del = value as Delegate;
            if (null == del)
            {
                _memberType = MemberTypes.Property;
                _returnType = (value == null) ? null : value.GetType();
                InitialData = value;
            }
            else
            {
                ConsumeDelegate(del);
            }
        }

        public MemberProjection(string name, Type type)
        {
            Name = name;
            IsPublic = true;

            if (typeof (Delegate).IsAssignableFrom(type))
            {
                Delegate prototype = (Delegate) Activator.CreateInstance(type);
                ConsumeDelegate(prototype);
            }
            else
            {
                _memberType = MemberTypes.Property;
                _returnType = type;
            }
        }

        public MemberProjection(MemberInfo memberInfo, object value = null)
        {
            _memberInfo = memberInfo;
            Name = memberInfo.Name;
            MemberOf = memberInfo.ReflectedType.Name;
            _memberType = memberInfo.MemberType;
            _argumentNames = Enumerable.Empty<string>();
            _argumentTypes = Enumerable.Empty<Type>();
            _argumentCount = 0;


            switch (_memberType)
            {
                case MemberTypes.Method:
                    MethodInfo mi = _memberInfo as MethodInfo;
                    if (null != mi)
                    {
                        IsPublic = mi.IsPublic;
                        _argumentNames = mi.GetParameters().Select(pmi => pmi.Name);
                        _argumentTypes = mi.GetParameters().Select(pmi => pmi.ParameterType);
                        _argumentCount = _argumentNames.Count();
                        if (null != value)
                            _implementation = value as Delegate;
                        _returnType = mi.ReturnType;
                    }
                    break;

                case MemberTypes.Property:
                    PropertyInfo pi = _memberInfo as PropertyInfo;
                    if (null != pi)
                    {
                        IsPublic = true;
                        _returnType = pi.PropertyType;
                        _argumentNames = Enumerable.Empty<string>();
                        _argumentCount = 0;
                        if (null != value)
                            InitialData = value;
                    }
                    break;
            }
        }

        public object InitialData { get; set; }

        public string NestedName { get; set; }

        public Delegate Implementation
        {
            get { return _implementation; }
            set { _implementation = value; }
        }

        public bool IsPublic { get; private set; }

        public Type ReturnType
        {
            get { return _returnType; }
            private set { _returnType = value; }
        }

        public string Name { get; private set; }

        public string MemberOf { get; private set; }


        public int ArgumentCount
        {
            get { return _argumentCount; }
            private set { _argumentCount = value; }
        }

        public MemberInfo MemberInfo
        {
            get { return _memberInfo; }
        }

        public IEnumerable<string> ArgumentNames
        {
            get { return _argumentNames; }
            private set { _argumentNames = value; }
        }

        public IEnumerable<Type> ArgumentTypes
        {
            get { return _argumentTypes; }
            private set { _argumentTypes = value; }
        }

        public MemberTypes MemberType
        {
            get { return _memberType; }
            private set { _memberType = value; }
        }

        private void ConsumeDelegate(Delegate del)
        {
            var mi = del.Method;
            _memberType = MemberTypes.Method;
            _argumentCount = mi.GetParameters().Count();
            _argumentTypes = mi.GetParameters().Select(pi => pi.ParameterType);
            _memberInfo = mi;
            _returnType = mi.ReturnType;
            _implementation = del;
        }


        public override int GetHashCode()
        {
            int argNamesHash = GetArgumentNamesHashCode();
            int argTypesHash = _cachedTypesHC;

            return
                Hash.GetCombinedHashCodeForHashes(
                    Name.GetHashCode(),
                    (ArgumentCount == 0) ? _magicZero : ArgumentCount,
                    argNamesHash,
                    argTypesHash,
                    MemberType.GetHashCode(),
                    ReturnType.GetHashCode());
        }

        public int GetArgumentNamesHashCode()
        {
            if (!_cachedNamesHCInit)
            {
                _cachedNamesHC = HashArgumentNames(_argumentNames);
                _cachedTypesHC = HashArgumentTypes(_argumentTypes);
                _cachedNamesHCInit = true;
            }

            return _cachedNamesHC;
        }

        public static int HashArgumentTypes(IEnumerable<Type> argTypes)
        {
            int argTypesHash = _magicOne;
            if (argTypes.EmptyIfNull().Any())
                argTypesHash = Hash.GetCombinedHashCodeForCollection(argTypes);
            return argTypesHash;
        }

        public static int HashArgumentNames(IEnumerable<string> names)
        {
            int argNamesHash = _magicOne;
            if (names.EmptyIfNull().Any())
                argNamesHash = Hash.GetCombinedHashCodeForCollection(names);
            return argNamesHash;
        }


        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            if (GetHashCode() == obj.GetHashCode())
            {
                MemberProjection that = (MemberProjection) obj;
                if (
                    Name == that.Name &&
                    MemberOf == that.MemberOf &&
                    MemberType == that.MemberType &&
                    IsPublic == that.IsPublic &&
                    ReturnType == that.ReturnType &&
                    ArgumentCount == that.ArgumentCount &&
                    ArgumentNames.SequenceEqual(that.ArgumentNames)
                    )
                    return true;
            }

            return false;
        }


        public override string ToString()
        {
            string str = String.Empty;
            str = String.Concat(str, "IsPublic = ", IsPublic, "\r\n");
            str = String.Concat(str, "ReturnType = ", ReturnType, "\r\n");
            str = String.Concat(str, "Name = ", Name, "\r\n");
            str = String.Concat(str, "MemberOf = ", MemberOf, "\r\n");
            str = String.Concat(str, "ArgumentCount = ", ArgumentCount, "\r\n");
            str = String.Concat(str, "ArgumentNames = ", ArgumentNames, "\r\n");
            str = String.Concat(str, "MemberType = ", MemberType, "\r\n");
            return str;
        }
    }

    public static class From<T>
    {
        public static IEnumerable<MemberProjection> Property<R>(Expression<Func<T, R>> selector)
        {
            var pi = Evaluator.ExtractPropertyInfo(selector);
            return EnumerableEx.OfOne(new MemberProjection(pi));
        }

        public static IEnumerable<MemberProjection> Properties(params Expression<Func<T, object>>[] selections)
        {
            return
                from mp in selections
                select new MemberProjection(Evaluator.ExtractPropertyInfoFuzzy(mp));
        }

        public static IEnumerable<MemberProjection> Properties()
        {
            return typeof (T).GetProperties().Select(pi => new MemberProjection(pi));
        }

        public static IEnumerable<MemberProjection> Method(Expression<Action<T>> action)
        {
            var mi = Evaluator.ExtractMethodInfo(action);
            return EnumerableEx.OfOne(new MemberProjection(mi));
        }

        public static IEnumerable<MemberProjection> Method(Expression<Func<T, Delegate>> expr)
        {
            var mi = Evaluator.ExtractMethodInfoSpec(expr);
            return EnumerableEx.OfOne(new MemberProjection(mi));
        }

        public static IEnumerable<MemberProjection> Methods(params Expression<Action<T>>[] selections)
        {
            return
                selections
                    .Select(e => new MemberProjection(Evaluator.ExtractMethodInfo(e)));
        }

        public static IEnumerable<MemberProjection> Methods(params Expression<Func<T, Delegate>>[] selections)
        {
            return
                selections
                    .Select(e => new MemberProjection(Evaluator.ExtractMethodInfoSpec(e)));
        }

        public static IEnumerable<MemberProjection> Methods()
        {
            return typeof (T).GetProperties().Select(pi => new MemberProjection(pi));
        }
    }

    public class FromClass<T>
    {
        public IEnumerable<MemberProjection> Property<R>(Expression<Func<T, R>> selector)
        {
            return From<T>.Property(selector);
        }

        public IEnumerable<MemberProjection> Properties(params Expression<Func<T, object>>[] selections)
        {
            return From<T>.Properties(selections);
        }

        public IEnumerable<MemberProjection> Properties()
        {
            return From<T>.Properties();
        }

        public IEnumerable<MemberProjection> Method(Expression<Action<T>> action)
        {
            return From<T>.Method(action);
        }

        public IEnumerable<MemberProjection> Method(Expression<Func<T, Delegate>> expr)
        {
            return From<T>.Method(expr);
        }

        public IEnumerable<MemberProjection> Methods(params Expression<Action<T>>[] selections)
        {
            return From<T>.Methods(selections);
        }

        public IEnumerable<MemberProjection> Methods(params Expression<Func<T, Delegate>>[] selections)
        {
            return From<T>.Methods(selections);
        }

        public IEnumerable<MemberProjection> Methods()
        {
            return From<T>.Methods();
        }
    }

    public static class Implement<T>
    {
        public static MemberProjection For(Expression<Action<T>> action, Delegate functor)
        {
            var mp = From<T>.Method(action);
            mp.First().Implementation = functor;
            return mp.First();
        }

        public static MemberProjection For(Expression<Func<T, Delegate>> expr, Delegate functor)
        {
            var mp = From<T>.Method(expr);
            mp.First().Implementation = functor;
            return mp.First();
        }
    }

    public static class Initialize<T>
    {
        public static MemberProjection For<R>(Expression<Func<T, R>> selector, object initialValue)
        {
            var mp = From<T>.Property(selector);
            mp.First().InitialData = initialValue;
            return mp.First();
        }
    }

    public class FromProjectionOf<T>
    {
        public IEnumerable<MemberProjection> Property<R>(Expression<Func<T, R>> selector, object initialValue)
        {
            var mp = From<T>.Property(selector);
            mp.First().InitialData = initialValue;
            return mp;
        }


        public IEnumerable<MemberProjection> Properties()
        {
            return From<T>.Properties();
        }

        public IEnumerable<MemberProjection> Method(Expression<Action<T>> action, Delegate functor)
        {
            return EnumerableEx.OfOne(
                Implement<T>.For(action, functor));
        }

        public IEnumerable<MemberProjection> Method(Expression<Func<T, Delegate>> expr, Delegate functor)
        {
            return EnumerableEx.OfOne(
                Implement<T>.For(expr, functor));
        }
    }
}