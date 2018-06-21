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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using AppComponents.Internal;

namespace AppComponents.Dynamic
{
    public static class InvocationBinding
    {
        private static readonly Type ComObjectType;

        private static readonly dynamic ComBinder;
        private static readonly dynamic _invokeSetAll = new InvokeSetters();

        static InvocationBinding()
        {
            try
            {
                ComObjectType = typeof (object).Assembly.GetType("System.__ComObject");
                ComBinder = new LateBindingInterceptor(
                    "System.Dynamic.ComBinder, System.Dynamic, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            }
            catch
            {
            }
        }

        public static dynamic InvokeSetAll
        {
            get { return _invokeSetAll; }
        }

        public static CallSite CreateCallSite(
            Type delegateType,
            CallSiteBinder binder,
            MemberInvocationMoniker name,
            Type context,
            string[] argumentNames = null,
            bool staticContext = false,
            bool isEvent = false)
        {
            return InvocationMapping.CreateCallSite(delegateType, binder.GetType(), () => binder, name, context,
                                                    argumentNames, staticContext,
                                                    isEvent);
        }

        public static CallSite<T> CreateCallSite<T>(
            CallSiteBinder binder,
            MemberInvocationMoniker name,
            Type context,
            string[] argumentNames = null,
            bool staticContext = false,
            bool isEvent = false) where T : class
        {
            return InvocationMapping.CreateCallSite<T>(binder.GetType(), () => binder, name, context, argumentNames,
                                                       staticContext,
                                                       isEvent);
        }

        public static dynamic InvokeMember(
            object target,
            MemberInvocationMoniker name,
            params object[] arguments)
        {
            string[] argumentNames;
            Type context;
            bool staticContext;
            target = target.GetInvocationContext(out context, out staticContext);
            arguments = TypeFactorization.ExtractArgumentNamesAndValues(arguments, out argumentNames);
            CallSite theCallSite = null;

            return InvocationMapping.InvokeMemberCallSite(target, name, arguments, argumentNames, context, staticContext,
                                                          ref theCallSite);
        }

        public static dynamic Invoke(object target, params object[] arguments)
        {
            string[] argumentNames;
            Type context;
            bool staticContext;
            target = target.GetInvocationContext(out context, out staticContext);
            arguments = TypeFactorization.ExtractArgumentNamesAndValues(arguments, out argumentNames);
            CallSite theCallSite = null;

            return InvocationMapping.InvokeDirectCallSite(target, arguments, argumentNames, context, staticContext,
                                                          ref theCallSite);
        }

        public static dynamic InvokeGetIndex(object target, params object[] indexes)
        {
            string[] argumentNames;
            Type context;
            bool staticContext;
            target = target.GetInvocationContext(out context, out staticContext);
            indexes = TypeFactorization.ExtractArgumentNamesAndValues(indexes, out argumentNames);
            CallSite theCallSite = null;

            return InvocationMapping.InvokeGetIndexCallSite(target, indexes, argumentNames, context, staticContext,
                                                            ref theCallSite);
        }

        public static void InvokeSetIndex(object target, params object[] indexesThenValue)
        {
            string[] argNames;
            Type context;
            bool staticContext;
            target = target.GetInvocationContext(out context, out staticContext);
            indexesThenValue = TypeFactorization.ExtractArgumentNamesAndValues(indexesThenValue, out argNames);

            CallSite theCallSite = null;
            InvocationMapping.InvokeSetIndexCallSite(target, indexesThenValue, argNames, context, staticContext,
                                                     ref theCallSite);
        }

        public static void InvokeMemberAction(object target, MemberInvocationMoniker name, params object[] arguments)
        {
            string[] argumentNames;
            Type context;
            bool staticContext;

            target = target.GetInvocationContext(out context, out staticContext);
            arguments = TypeFactorization.ExtractArgumentNamesAndValues(arguments, out argumentNames);

            CallSite theCallSite = null;
            InvocationMapping.InvokeMemberActionCallSite(target, name, arguments, argumentNames, context, staticContext,
                                                         ref theCallSite);
        }

        public static void InvokeAction(object target, params object[] arguments)
        {
            string[] argumentNames;
            Type context;
            bool staticContext;

            target = target.GetInvocationContext(out context, out staticContext);
            arguments = TypeFactorization.ExtractArgumentNamesAndValues(arguments, out argumentNames);

            CallSite theCallSite = null;
            InvocationMapping.InvokeDirectActionCallSite(target, arguments, argumentNames, context, staticContext,
                                                         ref theCallSite);
        }

        public static object InvokeSet(object target, string name, object value)
        {
            Type context;
            bool staticContext;
            target = target.GetInvocationContext(out context, out staticContext);
            context = context.MaybeDetectArrayContext();

            CallSite theCallSite = null;
            return InvocationMapping.InvokeSetCallSite(target, name, value, context, staticContext, ref theCallSite);
        }

        public static object InvokeSetChain(object target, string propertyChain, object value)
        {
            var tProperties = propertyChain.Split('.');
            var tGetProperties = tProperties.Take(tProperties.Length - 1);
            var tSetProperty = tProperties.Last();
            var tSetTarget = tGetProperties.Aggregate(target, InvokeGet);
            return InvokeSet(tSetTarget, tSetProperty, value);
        }

        public static dynamic Curry(object target, int? totalArgCount = null)
        {
            if (target is Delegate && !totalArgCount.HasValue)
                return Curry((Delegate) target);
            return new Curry(target, totalArgCount);
        }

        public static dynamic Curry(Delegate target)
        {
            return new Curry(target, target.Method.GetParameters().Length);
        }

        public static dynamic InvokeGet(object target, string name)
        {
            Type context;
            bool staticContext;
            target = target.GetInvocationContext(out context, out staticContext);
            context = context.MaybeDetectArrayContext();
            CallSite theSite = null;
            return InvocationMapping.InvokeGetCallSite(target, name, context, staticContext, ref theSite);
        }

        public static dynamic InvokeGetChain(object target, string propertyChain)
        {
            var properties = propertyChain.Split('.');
            return properties.Aggregate(target, InvokeGet);
        }

        public static bool InvokeIsEvent(object target, string name)
        {
            Type context;
            bool staticContext;
            target = target.GetInvocationContext(out context, out staticContext);
            context = context.MaybeDetectArrayContext();
            CallSite theCallSite = null;
            return InvocationMapping.InvokeIsEventCallSite(target, name, context, ref theCallSite);
        }

        public static void InvokeAddAssign(object target, string name, object value)
        {
            CallSite callSiteAdd = null;
            CallSite callSiteGet = null;
            CallSite callSiteSet = null;
            CallSite callSiteIsEvent = null;

            Type context;
            bool staticContext;
            target = target.GetInvocationContext(out context, out staticContext);

            var args = new[] {value};
            string[] argNames;
            args = TypeFactorization.ExtractArgumentNamesAndValues(args, out argNames);

            InvocationMapping.InvokeAddAssignCallSite(target, name, args, argNames, context, staticContext,
                                                      ref callSiteIsEvent, ref callSiteAdd, ref callSiteGet,
                                                      ref callSiteSet);
        }

        public static void InvokeSubtractAssign(object target, string name, object value)
        {
            Type context;
            bool staticContext;
            target = target.GetInvocationContext(out context, out staticContext);

            var args = new[] {value};
            string[] argNames;

            args = TypeFactorization.ExtractArgumentNamesAndValues(args, out argNames);

            CallSite callSiteIsEvent = null;
            CallSite callSiteRemove = null;
            CallSite callSiteGet = null;
            CallSite callSiteSet = null;

            InvocationMapping.InvokeSubtractAssignCallSite(target, name, args, argNames, context, staticContext,
                                                           ref callSiteIsEvent, ref callSiteRemove, ref callSiteGet,
                                                           ref callSiteSet);
        }


        public static dynamic Conversion(object target, Type type, bool explict = false)
        {
            Type tContext;
            bool tDummy;
            target = target.GetInvocationContext(out tContext, out tDummy);

            CallSite tCallSite = null;
            return InvocationMapping.InvokeConvertCallSite(target, explict, type, tContext, ref tCallSite);
        }

        public static dynamic CreateInstance(Type type, params object[] arguments)
        {
            string[] argumentNames;
            bool isValue = type.IsValueType;
            if (isValue && arguments.Length == 0) //dynamic invocation doesn't see constructors of value types
            {
                return Activator.CreateInstance(type);
            }

            arguments = TypeFactorization.ExtractArgumentNamesAndValues(arguments, out argumentNames);
            CallSite theCallSite = null;

            var context = type.MaybeDetectArrayContext();

            return InvocationMapping.InvokeConstructorCallSite(type, isValue, arguments, argumentNames, context,
                                                               ref theCallSite);
        }

        public static object FastDynamicInvoke(this Delegate functor, params object[] arguments)
        {
            if (functor.Method.ReturnType == typeof (void))
            {
                InvocationMapping.DynamicInvokeAction(functor, arguments);
                return null;
            }
            return InvocationMapping.DynamicInvokeReturn(functor, arguments);
        }

        public static Type GenericDelegateType(int parameterCount, bool returnVoid = false)
        {
            var theParameterCount = returnVoid ? parameterCount : parameterCount - 1;
            if (theParameterCount > 16)
                throw new ArgumentException(
                    String.Format("{0} only handle at  most {1} parameters", returnVoid ? "Action" : "Func",
                                  returnVoid ? 16 : 17), "paramCount");
            if (theParameterCount < 0)
                throw new ArgumentException(
                    String.Format("{0} must have at least {1} parameter(s)", returnVoid ? "Action" : "Func",
                                  returnVoid ? 0 : 1), "paramCount");

            return returnVoid
                       ? InvocationMapping.ActionPrototypes[theParameterCount]
                       : InvocationMapping.FuncPrototypes[theParameterCount];
        }

        public static IEnumerable<string> GetMemberNames(object target, bool dynamicOnly = false)
        {
            var memberNames = new List<string>();
            if (!dynamicOnly)
            {
                memberNames.AddRange(target.GetType().GetProperties().Select(it => it.Name));
            }

            var dynamicTarget = target as IDynamicMetaObjectProvider;
            if (dynamicTarget != null)
            {
                memberNames.AddRange(
                    dynamicTarget.GetMetaObject(Expression.Constant(dynamicTarget)).GetDynamicMemberNames());
            }
            else
            {
                if (ComObjectType != null && ComObjectType.IsInstanceOfType(target))
                {
                    memberNames.AddRange(ComBinder.GetDynamicDataMemberNames(target));
                }
            }
            return memberNames;
        }

        public static dynamic InvokeCallSite(CallSite callSite, object target, params object[] arguments)
        {
            var parameters = new List<object> {callSite, target};
            parameters.AddRange(arguments);

            MulticastDelegate functor = ((dynamic) callSite).OriginalTarget;

            return functor.FastDynamicInvoke(parameters.ToArray());
        }

        public static dynamic Invoke(CallSite callSite, object target, params object[] arguments)
        {
            var parameters = new List<object> {callSite, target};
            parameters.AddRange(arguments);

            MulticastDelegate functor = ((dynamic) callSite).OriginalTarget;

            return functor.FastDynamicInvoke(parameters.ToArray());
        }

        public static TInterface DressedAs<TInterface>(this object originalDynamic, params Type[] otherInterfaces)
            where TInterface : class
        {
            Type context;
            bool _;
            originalDynamic = originalDynamic.GetInvocationContext(out context, out _);
            context = context.MaybeDetectArrayContext();

            var proxy = BuildProxy.BuildType(context, typeof (TInterface), otherInterfaces);

            return
                (TInterface)
                InitializeProxy(proxy, originalDynamic, new[] {typeof (TInterface)}.Concat(otherInterfaces));
        }

        public static dynamic DressedAs(this object originalDynamic, params Type[] otherInterfaces)
        {
            return new InterfaceProjectionCaster(originalDynamic, otherInterfaces);
        }

        public static dynamic SelectProperties(this object originalDynamic, IDictionary<string, Type> propertySpec)
        {
            Type context;
            bool _;
            originalDynamic = originalDynamic.GetInvocationContext(out context, out _);
            context = context.MaybeDetectArrayContext();

            var tProxy = BuildProxy.BuildType(context, propertySpec);

            return
                InitializeProxy(tProxy, originalDynamic, propertySpec: propertySpec);
        }

        internal static object InitializeProxy(
            Type proxytype,
            object original,
            IEnumerable<Type> interfaces = null,
            IDictionary<string, Type> propertySpec = null)
        {
            var proxy = (IDressAsProxyInitializer) Activator.CreateInstance(proxytype);
            proxy.Initialize(original, interfaces, propertySpec);
            return proxy;
        }

        public static IEnumerable<TInterface> SelectManyInterfaces<TInterface>(
            this IEnumerable<object> originalDynamic,
            params Type[] otherInterfaces) where TInterface : class
        {
            return originalDynamic.Select(it => it.DressedAs<TInterface>(otherInterfaces));
        }

        public static dynamic DynamicDressedAs(object originalDynamic, params Type[] otherInterfaces)
        {
            Type tContext;
            bool tDummy;
            originalDynamic = originalDynamic.GetInvocationContext(out tContext, out tDummy);
            tContext = tContext.MaybeDetectArrayContext();

            var tProxy = BuildProxy.BuildType(tContext, otherInterfaces.First(), otherInterfaces.Skip(1).ToArray());

            return InitializeProxy(tProxy, originalDynamic, otherInterfaces);
        }
    }
}