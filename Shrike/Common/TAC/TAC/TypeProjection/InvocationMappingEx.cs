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
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace AppComponents.Dynamic
{
    internal static partial class InvocationMapping
    {
        #region Delegates

        public delegate void DynamicAction(params object[] arguments);

        public delegate TReturn DynamicFunc<out TReturn>(params object[] arguments);

        #endregion

        private static readonly IDictionary<BinderHashGenerator, CallSite> _binderCache =
            new Dictionary<BinderHashGenerator, CallSite>();

        private static readonly object _binderCacheLock = new object();

        internal static readonly IDictionary<Type, CallSite<DynamicInvokeMemberConstructorValueType>>
            _dynamicInvokeMemberSite = new Dictionary<Type, CallSite<DynamicInvokeMemberConstructorValueType>>();

        internal static readonly IDictionary<Type, CallSite<DynamicInvokeWrapFunc>> _dynamicInvokeWrapFunc =
            new Dictionary<Type, CallSite<DynamicInvokeWrapFunc>>();

        public static bool IsActionOrFunc(object target)
        {
            if (target == null)
                return false;

            var type = TypeFactorization.ForceTargetType(target);

            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }

            return FuncPrototypeArgCount.ContainsKey(type) || ActionPrototypeArgCount.ContainsKey(type);
        }

        internal static object InvokeMethodDelegate(
            this object target,
            Delegate func,
            object[] arguments)
        {
            object result;

            try
            {
                result = func.FastDynamicInvoke(
                    func.IsSpecialThisDelegate()
                        ? new[] {target}.Concat(arguments).ToArray()
                        : arguments);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                    throw ex.InnerException;
                throw;
            }
            return result;
        }

        internal static IEnumerable<CSharpArgumentInfo> GetBindingArgumentList(
            object[] arguments,
            string[] argumentNames,
            bool staticContext)
        {
            var targetFlag = CSharpArgumentInfoFlags.None;
            if (staticContext)
            {
                targetFlag |= CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType;
            }

            var argumentInfoList = new FixedArray<CSharpArgumentInfo>(arguments.Length + 1)
                                       {
                                           CSharpArgumentInfo.Create(targetFlag, null)
                                       };

            for (int i = 0; i < arguments.Length; i++)
            {
                var flag = CSharpArgumentInfoFlags.None;
                string argumentName = null;
                if (argumentNames != null && argumentNames.Length > i)
                    argumentName = argumentNames[i];

                if (!String.IsNullOrEmpty(argumentName))
                {
                    flag |= CSharpArgumentInfoFlags.NamedArgument;
                }

                argumentInfoList.Add(CSharpArgumentInfo.Create(flag, argumentName));
            }

            return argumentInfoList;
        }

        internal static CallSite CreateCallSite(
            Type delegateType,
            Type specificBinderType,
            LazyBinder binder,
            MemberInvocationMoniker name,
            Type context,
            string[] argumentNames = null,
            bool staticContext = false,
            bool isEvent = false)
        {
            var hash = BinderHashGenerator.Create(delegateType, name, context, argumentNames, specificBinderType,
                                                  staticContext, isEvent);
            lock (_binderCacheLock)
            {
                CallSite tOut;
                if (!_binderCache.TryGetValue(hash, out tOut))
                {
                    tOut = CallSite.Create(delegateType, binder());
                    _binderCache[hash] = tOut;
                }
                return tOut;
            }
        }

        internal static CallSite<T> CreateCallSite<T>(
            Type specificBinderType,
            LazyBinder binder,
            MemberInvocationMoniker name,
            Type context,
            string[] argumentNames = null,
            bool staticContext = false,
            bool isEvent = false)
            where T : class
        {
            var hashCode = BinderHashGenerator<T>.Create(name, context, argumentNames, specificBinderType, staticContext,
                                                         isEvent);
            lock (_binderCacheLock)
            {
                CallSite callSite;
                if (!_binderCache.TryGetValue(hashCode, out callSite))
                {
                    callSite = CallSite<T>.Create(binder());
                    _binderCache[hashCode] = callSite;
                }
                return (CallSite<T>) callSite;
            }
        }

        internal static dynamic DynamicInvokeStaticMember(
            Type returnType,
            ref CallSite callSite,
            Type binderType,
            LazyBinder binder,
            string name,
            bool staticContext,
            Type context,
            string[] argumentNames,
            Type target, params object[] arguments)
        {
            CallSite<DynamicInvokeMemberConstructorValueType> site;
            if (!_dynamicInvokeMemberSite.TryGetValue(returnType, out site))
            {
                site = CallSite<DynamicInvokeMemberConstructorValueType>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None,
                        "InvokeMemberTargetType",
                        new[] {typeof (Type), returnType},
                        typeof (InvocationMapping),
                        new[]
                            {
                                CSharpArgumentInfo.Create(
                                    CSharpArgumentInfoFlags.IsStaticType |
                                    CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                CSharpArgumentInfo.Create(
                                    CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsRef, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            }));
                _dynamicInvokeMemberSite[returnType] = site;
            }

            return site.Target(site, typeof (InvocationMapping), ref callSite, binderType, binder, name, staticContext,
                               context, argumentNames, target, arguments);
        }

        internal static TReturn InvokeMember<TReturn>(ref CallSite callsite, Type binderType, LazyBinder binder,
                                                      MemberInvocationMoniker name,
                                                      bool staticContext,
                                                      Type context,
                                                      string[] argumentNames,
                                                      object target, params object[] arguments)
        {
            return MemberTargetTypeInvoke<object, TReturn>(ref callsite, binderType, binder, name, staticContext,
                                                           context, argumentNames, target, arguments);
        }

        internal static object InvokeGetCallSite(object target, string name, Type context, bool staticContext,
                                                 ref CallSite callsite)
        {
            if (callsite == null)
            {
                var targeflag = CSharpArgumentInfoFlags.None;
                LazyBinder binder;
                Type binderType;
                if (staticContext) //CSharp Binder won't call Static properties, grrr.
                {
                    var staticFlag = CSharpBinderFlags.None;
                    if (TypeFactorization.IsMonoRuntimeEnvironment)
                        //Mono only works if InvokeSpecialName is set and .net only works if it isn't
                        staticFlag |= CSharpBinderFlags.InvokeSpecialName;

                    binder = () => Binder.InvokeMember(staticFlag, "get_" + name,
                                                       null,
                                                       context,
                                                       new List<CSharpArgumentInfo>
                                                           {
                                                               CSharpArgumentInfo.Create(
                                                                   CSharpArgumentInfoFlags.IsStaticType |
                                                                   CSharpArgumentInfoFlags.UseCompileTimeType,
                                                                   null)
                                                           });

                    binderType = typeof (InvokeMemberBinder);
                }
                else
                {
                    binder = () => Binder.GetMember(CSharpBinderFlags.None, name,
                                                    context,
                                                    new List<CSharpArgumentInfo>
                                                        {
                                                            CSharpArgumentInfo.Create(
                                                                targeflag, null)
                                                        });
                    binderType = typeof (GetMemberBinder);
                }

                callsite = CreateCallSite<Func<CallSite, object, object>>(binderType, binder, name, context);
            }
            var callSite = (CallSite<Func<CallSite, object, object>>) callsite;
            return callSite.Target(callSite, target);
        }

        internal static object InvokeSetCallSite(object target, string name, object value, Type context,
                                                 bool staticContext, ref CallSite callSite)
        {
            if (callSite == null)
            {
                LazyBinder binder;
                Type binderType;
                if (staticContext)
                {
                    binder = () =>
                                 {
                                     var staticFlage = CSharpBinderFlags.ResultDiscarded;

                                     //Mono only works if InvokeSpecialName is set and .net only works if it isn't
                                     if (TypeFactorization.IsMonoRuntimeEnvironment)
                                         staticFlage |= CSharpBinderFlags.InvokeSpecialName;

                                     return Binder.InvokeMember(staticFlage, "set_" + name,
                                                                null,
                                                                context,
                                                                new List<CSharpArgumentInfo>
                                                                    {
                                                                        CSharpArgumentInfo.Create(
                                                                            CSharpArgumentInfoFlags.IsStaticType |
                                                                            CSharpArgumentInfoFlags.UseCompileTimeType,
                                                                            null),
                                                                        CSharpArgumentInfo.Create(
                                                                            CSharpArgumentInfoFlags.None,
                                                                            null)
                                                                    });
                                 };

                    binderType = typeof (InvokeMemberBinder);
                    callSite = CreateCallSite<Action<CallSite, object, object>>(binderType, binder, name, context);
                }
                else
                {
                    binder = () => Binder.SetMember(CSharpBinderFlags.None, name,
                                                    context,
                                                    new List<CSharpArgumentInfo>
                                                        {
                                                            CSharpArgumentInfo.Create(
                                                                CSharpArgumentInfoFlags.None, null),
                                                            CSharpArgumentInfo.Create(
                                                                CSharpArgumentInfoFlags.None,
                                                                null)
                                                        });

                    binderType = typeof (SetMemberBinder);
                    callSite = CreateCallSite<Func<CallSite, object, object, object>>(binderType, binder, name, context);
                }
            }

            if (staticContext)
            {
                var staticContextheCallSite = (CallSite<Action<CallSite, object, object>>) callSite;
                staticContextheCallSite.Target(callSite, target, value);
                return value;
            }
            else
            {
                var cs = (CallSite<Func<CallSite, object, object, object>>) callSite;
                var tResult = cs.Target(callSite, target, value);
                return tResult;
            }
        }

        internal static object InvokeMemberCallSite(object target, MemberInvocationMoniker name, object[] arguments,
                                                    string[] argumentNames, Type context, bool staticContext,
                                                    ref CallSite callSite)
        {
            LazyBinder binder = null;
            Type binderType = null;
            if (callSite == null)
            {
                binder = () =>
                             {
                                 var bindingArgumentList = GetBindingArgumentList(arguments, argumentNames,
                                                                                  staticContext);
                                 var flag = CSharpBinderFlags.None;
                                 if (name.IsNameSpecial)
                                 {
                                     flag |= CSharpBinderFlags.InvokeSpecialName;
                                 }
                                 return Binder.InvokeMember(flag, name.Name, name.GenericArguments,
                                                            context, bindingArgumentList);
                             };
                binderType = typeof (InvokeMemberBinder);
            }

            return InvokeMember<object>(ref callSite, binderType, binder, name, staticContext, context, argumentNames,
                                        target, arguments);
        }

        internal static object InvokeDirectCallSite(object target, object[] arguments, string[] argumentNames,
                                                    Type context, bool staticContext, ref CallSite callSite)
        {
            LazyBinder binder = null;
            Type binderType = null;

            if (callSite == null)
            {
                binder = () =>
                             {
                                 var bindingArgumentList = GetBindingArgumentList(arguments, argumentNames,
                                                                                  staticContext);
                                 var flag = CSharpBinderFlags.None;
                                 return Binder.Invoke(flag, context, bindingArgumentList);
                             };
                binderType = typeof (InvokeBinder);
            }

            return InvokeMember<object>(ref callSite, binderType, binder, String.Empty, staticContext, context,
                                        argumentNames, target, arguments);
        }

        internal static object InvokeGetIndexCallSite(object target, object[] indexes, string[] argumentNames,
                                                      Type context, bool staticContext, ref CallSite callSite)
        {
            LazyBinder binder = null;
            Type binderType = null;
            if (callSite == null)
            {
                binder = () =>
                             {
                                 var bindingArgumentList = GetBindingArgumentList(indexes, argumentNames,
                                                                                  staticContext);
                                 return Binder.GetIndex(CSharpBinderFlags.None, context, bindingArgumentList);
                             };
                binderType = typeof (GetIndexBinder);
            }

            return InvokeMember<object>(ref callSite, binderType, binder, Invocation.IndexBinderName, staticContext,
                                        context, argumentNames, target, indexes);
        }

        internal static void InvokeSetIndexCallSite(object target, object[] indexesThenValue, string[] argumentNames,
                                                    Type context, bool staticContext, ref CallSite callSite)
        {
            LazyBinder binder = null;
            Type binderType = null;
            if (callSite == null)
            {
                binder = () =>
                             {
                                 var bindingArgumentList = GetBindingArgumentList(indexesThenValue, argumentNames,
                                                                                  staticContext);
                                 return Binder.SetIndex(CSharpBinderFlags.None, context, bindingArgumentList);
                             };

                binderType = typeof (SetIndexBinder);
            }

            MemberActionInvoke(ref callSite, binderType, binder, Invocation.IndexBinderName, staticContext, context,
                               argumentNames, target, indexesThenValue);
        }

        internal static void InvokeMemberActionCallSite(object target, MemberInvocationMoniker name, object[] arguments,
                                                        string[] argumentNames, Type context, bool staticContext,
                                                        ref CallSite callSite)
        {
            LazyBinder binder = null;
            Type binderType = null;
            if (callSite == null)
            {
                binder = () =>
                             {
                                 var argumentBindingList = GetBindingArgumentList(arguments, argumentNames,
                                                                                  staticContext);

                                 var flag = CSharpBinderFlags.ResultDiscarded;
                                 if (name.IsNameSpecial)
                                 {
                                     flag |= CSharpBinderFlags.InvokeSpecialName;
                                 }

                                 return Binder.InvokeMember(flag, name.Name, name.GenericArguments,
                                                            context, argumentBindingList);
                             };
                binderType = typeof (InvokeMemberBinder);
            }

            MemberActionInvoke(ref callSite, binderType, binder, name, staticContext, context, argumentNames, target,
                               arguments);
        }

        internal static void InvokeDirectActionCallSite(object target, object[] arguments, string[] argumentNames,
                                                        Type context, bool staticContext, ref CallSite callSite)
        {
            LazyBinder binder = null;
            Type binderType = null;

            if (callSite == null)
            {
                binder = () =>
                             {
                                 var bindingArgumentList = GetBindingArgumentList(arguments, argumentNames,
                                                                                  staticContext);

                                 var flag = CSharpBinderFlags.ResultDiscarded;

                                 return Binder.Invoke(flag, context, bindingArgumentList);
                             };
                binderType = typeof (InvokeBinder);
            }

            MemberActionInvoke(ref callSite, binderType, binder, String.Empty, staticContext, context, argumentNames,
                               target, arguments);
        }

        internal static bool InvokeIsEventCallSite(object target, string name, Type context, ref CallSite callSite)
        {
            if (callSite == null)
            {
                LazyBinder binder = () => Binder.IsEvent(CSharpBinderFlags.None, name, context);
                var binderType = typeof (IsEventBinderDefault);
                callSite = CreateCallSite<Func<CallSite, object, bool>>(binderType, binder, name, context, isEvent: true);
            }
            var theCallSite = (CallSite<Func<CallSite, object, bool>>) callSite;

            return theCallSite.Target(theCallSite, target);
        }

        internal static void InvokeAddAssignCallSite(object target, string name, object[] arguments,
                                                     string[] argumentNames, Type context, bool staticContext,
                                                     ref CallSite callSiteIsEvent, ref CallSite callSiteAdd,
                                                     ref CallSite callSiteGet, ref CallSite callSiteSet)
        {
            if (InvokeIsEventCallSite(target, name, context, ref callSiteIsEvent))
            {
                InvokeMemberActionCallSite(target, InvokeMemberByName.CreateSpecialName("add_" + name), arguments,
                                           argumentNames, context, staticContext, ref callSiteAdd);
            }
            else
            {
                dynamic theGet = InvokeGetCallSite(target, name, context, staticContext, ref callSiteGet);
                theGet += (arguments[0]);
                InvokeSetCallSite(target, name, theGet, context, staticContext, ref callSiteSet);
            }
        }

        internal static void InvokeSubtractAssignCallSite(object target, string name, object[] arguments,
                                                          string[] argumentNames, Type context, bool staticContext,
                                                          ref CallSite callSiteIsEvent, ref CallSite callSiteRemove,
                                                          ref CallSite callSiteGet, ref CallSite callSiteSet)
        {
            if (InvokeIsEventCallSite(target, name, context, ref callSiteIsEvent))
            {
                InvokeMemberActionCallSite(target, InvokeMemberByName.CreateSpecialName("remove_" + name), arguments,
                                           argumentNames, context, staticContext, ref callSiteRemove);
            }
            else
            {
                dynamic tGet = InvokeGetCallSite(target, name, context, staticContext, ref callSiteGet);
                tGet -= (arguments[0]);
                InvocationMapping.InvokeSetCallSite(target, name, tGet, context, staticContext, ref callSiteSet);
            }
        }

        internal static object InvokeConvertCallSite(object target, bool explict, Type type, Type context,
                                                     ref CallSite callSite)
        {
            if (callSite == null)
            {
                LazyBinder binder = () =>
                                        {
                                            var flags = explict
                                                            ? CSharpBinderFlags.ConvertExplicit
                                                            : CSharpBinderFlags.None;

                                            return Binder.Convert(flags, type, context);
                                        };
                Type binderType = typeof (ConvertBinder);

                var func = BuildProxy.GenerateCallSiteFuncType(new Type[] {}, type);

                callSite = CreateCallSite(func, binderType, binder,
                                          explict
                                              ? Invocation.ExplicitConvertBinderName
                                              : Invocation.ImplicitConvertBinderName, context);
            }
            dynamic theDynamicCallSite = callSite;
            return theDynamicCallSite.OriginalTarget(callSite, target);
        }

        internal static object InvokeConstructorCallSite(Type type, bool isValueType, object[] arguments,
                                                         string[] argumentNames, Type context, ref CallSite callSite)
        {
            LazyBinder binder = null;
            Type binderType = typeof (InvokeConstructorDummy);
            if (callSite == null || isValueType)
            {
                if (isValueType && arguments.Length == 0)
                    //dynamic invocation doesn't see no argument constructors of value types
                {
                    return Activator.CreateInstance(type);
                }

                binder = () =>
                             {
                                 var tList = GetBindingArgumentList(arguments, argumentNames, true);
                                 return Binder.InvokeConstructor(CSharpBinderFlags.None, type, tList);
                             };
            }

            if (isValueType || TypeFactorization.IsMonoRuntimeEnvironment)
            {
                CallSite dummyCallSite = null;
                return DynamicInvokeStaticMember(type, ref dummyCallSite, binderType, binder,
                                                 Invocation.ConstructorBinderName, true, type,
                                                 argumentNames, type, arguments);
            }

            return MemberTargetTypeInvoke<Type, object>(ref callSite, binderType, binder,
                                                        Invocation.ConstructorBinderName, true, type, argumentNames,
                                                        type, arguments);
        }

        internal static Delegate WrapFunc(Type returnType, object functor, int length)
        {
            CallSite<DynamicInvokeWrapFunc> theCallSite;
            if (!_dynamicInvokeWrapFunc.TryGetValue(returnType, out theCallSite))
            {
                var methodName = "WrapFunctionToDelegateMono";

#if !__MonoCS__

                if (!TypeFactorization.IsMonoRuntimeEnvironment)
                {
                    methodName = "WrapFunctionToDelegate";
                }
#endif
                theCallSite = CallSite<DynamicInvokeWrapFunc>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None,
                        methodName,
                        new[] {returnType},
                        typeof (InvocationMapping),
                        new[]
                            {
                                CSharpArgumentInfo.Create(
                                    CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType,
                                    null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            }));
                _dynamicInvokeWrapFunc[returnType] = theCallSite;
            }
            return (Delegate) theCallSite.Target(theCallSite, typeof (InvocationMapping), functor, length);
        }

        #region Nested type: DynamicInvokeMemberConstructorValueType

        internal delegate object DynamicInvokeMemberConstructorValueType(
            CallSite funcSite,
            Type funcTarget,
            ref CallSite callsite,
            Type binderType,
            LazyBinder binder,
            MemberInvocationMoniker name,
            bool staticContext,
            Type context,
            string[] argumentNames,
            Type target,
            object[] arguments);

        #endregion

        #region Nested type: DynamicInvokeWrapFunc

        internal delegate object DynamicInvokeWrapFunc(
            CallSite funcSite,
            Type funcTarget,
            object invokable,
            int length);

        #endregion

        #region Nested type: InvokeConstructorDummy

        internal class InvokeConstructorDummy
        {
        };

        #endregion

        #region Nested type: IsEventBinderDefault

        internal class IsEventBinderDefault
        {
        }

        #endregion

        #region Nested type: LazyBinder

        internal delegate CallSiteBinder LazyBinder();

        #endregion
    }
}