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
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;

namespace AppComponents.Dynamic
{
    [Serializable]
    public class InvocationCacheCompatible : Invocation
    {
        private int _argumentCount;
        private string[] _argumentNames;
        private CallSite _callSite;
        private CallSite _callSite2;
        private CallSite _callSite3;
        private CallSite _callSite4;
        private Type _context;
        private bool _convertExplict;
        private Type _convertType;
        private bool _staticContext;


        public InvocationCacheCompatible(InvocationKind kind,
                                         MemberInvocationMoniker name = null,
                                         int argCount = 0,
                                         string[] argNames = null,
                                         object context = null,
                                         Type convertType = null,
                                         bool convertExplict = false,
                                         object[] storedArgs = null)
            : base(kind, name, storedArgs)
        {
            _convertType = convertType;
            _convertExplict = convertExplict;

            ExtractArguments(argNames, storedArgs);

            DecideArgumentCountBasedOnType(kind, argCount, convertType);

            SetArgumentNames();

            InitializeContext(context);
        }

        public static InvocationCacheCompatible CreateConvert(Type convertType, bool convertExplict = false)
        {
            return new InvocationCacheCompatible(InvocationKind.Convert, convertType: convertType,
                                                 convertExplict: convertExplict);
        }


        public static InvocationCacheCompatible CreateCall(
            InvocationKind kind,
            MemberInvocationMoniker name = null,
            CallInfo callinfo = null,
            object context = null)
        {
            var argumentCount = callinfo != null ? callinfo.ArgumentCount : 0;
            var argumentNames = callinfo != null ? callinfo.ArgumentNames.ToArray() : null;

            return new InvocationCacheCompatible(kind, name, argumentCount, argumentNames, context);
        }

        private void InitializeContext(object context)
        {
            if (null != context)
            {
                context.GetInvocationContext(out _context, out _staticContext);
            }
            else
            {
                _context = typeof (object);
            }
        }

        private void SetArgumentNames()
        {
            if (_argumentCount > 0)
            {
                var newArgNames = new string[_argumentCount];
                if (_argumentNames.Length != 0)
                    Array.Copy(_argumentNames, 0, newArgNames, newArgNames.Length - _argumentNames.Length,
                               newArgNames.Length);
                else
                    newArgNames = null;
                _argumentNames = newArgNames;
            }
        }

        private void ExtractArguments(string[] argNames, object[] storedArgs)
        {
            _argumentNames = argNames ?? new string[] {};

            if (null != storedArgs)
            {
                _argumentCount = storedArgs.Length;
                string[] theArgumentNames;
                Arguments = TypeFactorization.ExtractArgumentNamesAndValues(storedArgs, out theArgumentNames);
                if (_argumentNames.Length < theArgumentNames.Length)
                {
                    _argumentNames = theArgumentNames;
                }
            }
        }

        private void DecideArgumentCountBasedOnType(InvocationKind kind, int argCount, Type convertType)
        {
            switch (kind)
            {
                case InvocationKind.GetIndex:
                    if (argCount < 1)
                    {
                        throw new ArgumentException("Arg Count must be at least 1 for a GetIndex", "argCount");
                    }
                    _argumentCount = argCount;
                    break;
                case InvocationKind.SetIndex:
                    if (argCount < 2)
                    {
                        throw new ArgumentException("Arg Count Must be at least 2 for a SetIndex", "argCount");
                    }
                    _argumentCount = argCount;
                    break;
                case InvocationKind.Convert:
                    _argumentCount = 0;
                    if (convertType == null)
                        throw new ArgumentNullException("convertType", " Convert Requires Convert Type ");
                    break;
                case InvocationKind.SubtractAssign:
                case InvocationKind.AddAssign:
                case InvocationKind.Set:
                    _argumentCount = 1;
                    break;
                case InvocationKind.Get:
                case InvocationKind.IsEvent:
                    _argumentCount = 0;
                    break;
                default:
                    _argumentCount = Math.Max(argCount, _argumentNames.Length);
                    break;
            }
        }

        public bool Equals(InvocationCacheCompatible other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other)
                   && other._argumentCount == _argumentCount
                   && Equals(other._argumentNames, _argumentNames)
                   && other._staticContext.Equals(_staticContext)
                   && Equals(other._context, _context)
                   && other._convertExplict.Equals(_convertExplict)
                   && Equals(other._convertType, _convertType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as InvocationCacheCompatible);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result*397) ^ _argumentCount;
                result = (result*397) ^ (_argumentNames != null ? _argumentNames.GetHashCode() : 0);
                result = (result*397) ^ _staticContext.GetHashCode();
                result = (result*397) ^ (_context != null ? _context.GetHashCode() : 0);
                result = (result*397) ^ _convertExplict.GetHashCode();
                result = (result*397) ^ (_convertType != null ? _convertType.GetHashCode() : 0);
                return result;
            }
        }


        public override object Invoke(object target, params object[] args)
        {
            var invocationContext = target as InvocationContext;
            if (invocationContext != null)
            {
                target = invocationContext.Target;
            }

            if (args == null)
            {
                args = new object[] {null};
            }

            ValidateInvocationArguments(args);

            switch (Kind)
            {
                case InvocationKind.Constructor:
                    var tTarget = (Type) target;
                    return InvocationMapping.InvokeConstructorCallSite(tTarget, tTarget.IsValueType, args,
                                                                       _argumentNames, _context,
                                                                       ref _callSite);
                case InvocationKind.Convert:
                    return InvocationMapping.InvokeConvertCallSite(target, _convertExplict, _convertType, _context,
                                                                   ref _callSite);
                case InvocationKind.Get:
                    return InvocationMapping.InvokeGetCallSite(target, Name.Name, _context, _staticContext,
                                                               ref _callSite);
                case InvocationKind.Set:
                    InvocationMapping.InvokeSetCallSite(target, Name.Name, args[0], _context, _staticContext,
                                                        ref _callSite);
                    return null;
                case InvocationKind.GetIndex:
                    return InvocationMapping.InvokeGetIndexCallSite(target, args, _argumentNames, _context,
                                                                    _staticContext, ref _callSite);
                case InvocationKind.SetIndex:
                    InvocationBinding.InvokeSetIndex(target, args);
                    return null;
                case InvocationKind.InvokeMember:
                    return InvocationMapping.InvokeMemberCallSite(target, Name, args, _argumentNames, _context,
                                                                  _staticContext, ref _callSite);
                case InvocationKind.InvokeMemberAction:
                    InvocationMapping.InvokeMemberActionCallSite(target, Name, args, _argumentNames, _context,
                                                                 _staticContext, ref _callSite);
                    return null;
                case InvocationKind.InvokeMemberUnknown:
                    {
                        try
                        {
                            var tObj = InvocationMapping.InvokeMemberCallSite(target, Name, args, _argumentNames,
                                                                              _context, _staticContext, ref _callSite);
                            return tObj;
                        }
                        catch (RuntimeBinderException)
                        {
                            InvocationMapping.InvokeMemberActionCallSite(target, Name, args, _argumentNames, _context,
                                                                         _staticContext, ref _callSite2);
                            return null;
                        }
                    }
                case InvocationKind.Invoke:
                    return InvocationMapping.InvokeDirectCallSite(target, args, _argumentNames, _context, _staticContext,
                                                                  ref _callSite);
                case InvocationKind.InvokeAction:
                    InvocationMapping.InvokeDirectActionCallSite(target, args, _argumentNames, _context, _staticContext,
                                                                 ref _callSite);
                    return null;
                case InvocationKind.InvokeUnknown:
                    {
                        try
                        {
                            var tObj = InvocationMapping.InvokeDirectCallSite(target, args, _argumentNames, _context,
                                                                              _staticContext, ref _callSite);
                            return tObj;
                        }
                        catch (RuntimeBinderException)
                        {
                            InvocationMapping.InvokeDirectActionCallSite(target, args, _argumentNames, _context,
                                                                         _staticContext, ref _callSite2);
                            return null;
                        }
                    }
                case InvocationKind.AddAssign:
                    InvocationMapping.InvokeAddAssignCallSite(target, Name.Name, args, _argumentNames, _context,
                                                              _staticContext, ref _callSite, ref _callSite2,
                                                              ref _callSite3, ref _callSite4);
                    return null;
                case InvocationKind.SubtractAssign:
                    InvocationMapping.InvokeSubtractAssignCallSite(target, Name.Name, args, _argumentNames, _context,
                                                                   _staticContext, ref _callSite, ref _callSite2,
                                                                   ref _callSite3, ref _callSite4);
                    return null;
                case InvocationKind.IsEvent:
                    return InvocationMapping.InvokeIsEventCallSite(target, Name.Name, _context, ref _callSite);
                default:
                    throw new InvalidOperationException("Unknown Invocation Kind: " + Kind);
            }
        }

        private void ValidateInvocationArguments(object[] args)
        {
            if (args.Length != _argumentCount)
            {
                switch (Kind)
                {
                    case InvocationKind.Convert:
                        if (args.Length > 0)
                        {
                            if (!Equals(args[0], _convertType))
                                throw new ArgumentException(
                                    "InvocationCacheCompatible can't change conversion type on invoke.", "args");
                        }
                        if (args.Length > 1)
                        {
                            if (!Equals(args[1], _convertExplict))
                                throw new ArgumentException(
                                    "InvocationCacheCompatible can't change explict/implict conversion on invoke.",
                                    "args");
                        }

                        if (args.Length > 2)
                            goto default;
                        break;
                    default:
                        throw new ArgumentException("args",
                                                    string.Format(
                                                        "Incorrect number of Arguments for InvocationCacheCompatible, Expected:{0}",
                                                        _argumentCount));
                }
            }
        }
    }
}