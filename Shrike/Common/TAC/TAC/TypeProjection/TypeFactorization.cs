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
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.CSharp.RuntimeBinder;

namespace AppComponents.Dynamic
{
    public static class TypeFactorization
    {
        public static readonly bool IsMonoRuntimeEnvironment;

        static TypeFactorization()
        {
            IsMonoRuntimeEnvironment = null != Type.GetType("Mono.Runtime");
        }

        public static Boolean IsAnonymousType(this Type type)
        {
            bool hasCompilerGeneratedAttribute =
                type.GetCustomAttributes(typeof (CompilerGeneratedAttribute), false).Count() > 0;
            bool nameContainsAnonymousType = type.FullName.Contains("AnonymousType");
            bool isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

            return isAnonymousType;
        }

        public static bool IsTypeAnonymous(Type targetType)
        {
            if (null == targetType)
                return false;

            return targetType.IsAnonymousType();
        }

        public static bool IsTypeAnonymous(object o)
        {
            if (null == o)
                return false;

            return IsTypeAnonymous(ForceTargetType(o));
        }

        public static Type ForceTargetType(object t)
        {
            Type theType = t as Type;
            if (null == theType)
                theType = t.GetType();

            return theType;
        }

        public static object[] MaybeRenameArguments(CallInfo ci, object[] args)
        {
            object[] retval;
            if (ci.ArgumentNames.Count == 0)
            {
                retval = args;
            }
            else
            {
                var diff = ci.ArgumentCount - ci.ArgumentNames.Count;
                retval = Enumerable.Repeat(default(string), diff)
                    .Concat(ci.ArgumentNames)
                    .Zip(args, (name, value) => MaybeCreateMethodInvocationArgument(name, value))
                    .ToArray();
            }
            return retval;
        }

        private static object MaybeCreateMethodInvocationArgument(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                return value;
            }
            else
            {
                return new MethodInvocationArgument(name, value);
            }
        }

        public static object GetInvocationContext(this object target, out Type context, out bool isStatic)
        {
            var invocationContext = target as InvocationContext;
            isStatic = false;
            if (invocationContext != null)
            {
                isStatic = invocationContext.StaticContext;
                context = invocationContext.Context;
                context = context.MaybeDetectArrayContext();
                return invocationContext.Target;
            }

            context = ForceTargetType(target);

            context = context.MaybeDetectArrayContext();
            return target;
        }

        public static Type MaybeDetectArrayContext(this Type context)
        {
            if (context.IsArray)
            {
                return typeof (object);
            }
            return context;
        }

        public static T GetValue<T>(this SerializationInfo info, string name)
        {
            return (T) info.GetValue(name, typeof (T));
        }

        internal static bool WireUpForInterface(this ShapeableObject bindSite, string binderName, bool found,
                                                ref object itf)
        {
            if (itf is InterceptorAddRemove)
                return true;

            Type theType;
            bool gotType = bindSite.GetTypeForPropertyNameFromInterface(binderName, out theType);

            if (gotType && theType == typeof (void))
            {
                return true;
            }

            if (found)
            {
                itf = WireUpForInterfaceUsingType(gotType, theType, bindSite, itf);
            }
            else
            {
                itf = null;
                if (!gotType)
                {
                    return false;
                }

                if (theType.IsValueType)
                {
                    itf = InvocationBinding.CreateInstance(theType);
                }
            }

            return true;
        }

        private static object WireUpForInterfaceUsingType(bool gotType, Type theType, ShapeableObject bindSite,
                                                          object itf)
        {
            if (IsDictionaryButNotExpando(itf, gotType, theType))
            {
                itf = new ShapeableExpando((IDictionary<string, object>) itf);
            }
            else if (gotType)
            {
                if (itf != null && !theType.IsAssignableFrom(itf.GetType()))
                {
                    if (theType.IsInterface)
                    {
                        itf = InterfaceProjection(theType, itf);
                    }
                    else
                    {
                        try
                        {
                            object tResult;

                            tResult = InvocationBinding.Conversion(bindSite, theType, explict: true);

                            itf = tResult;
                        }
                        catch (RuntimeBinderException)
                        {
                            itf = MaybeConvert(theType, itf);
                        }
                    }
                }
                else if (null == itf && theType.IsValueType)
                {
                    itf = InvocationBinding.CreateInstance(theType);
                }
            }

            return itf;
        }

        internal static object[] ExtractArgumentNamesAndValues(object[] arguments, out string[] argumentNames)
        {
            if (arguments == null)
                arguments = new object[] {null};

            argumentNames = new string[arguments.Length];

            var anArgumentHadAName = false;
            var newArguments = new object[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                string argumentName = null;

                if (argument is MethodInvocationArgument)
                {
                    argumentName = ((MethodInvocationArgument) argument).Name;
                    newArguments[i] = ((MethodInvocationArgument) argument).Value;
                    anArgumentHadAName = true;
                }
                else
                {
                    newArguments[i] = argument;
                }
                argumentNames[i] = argumentName;
            }

            if (!anArgumentHadAName)
                argumentNames = null;

            return newArguments;
        }

        private static object MaybeConvert(Type theType, object itf)
        {
            Type innerType = theType;
            if (theType.IsGenericType && theType.GetGenericTypeDefinition().Equals(typeof (Nullable<>)))
            {
                innerType = theType.GetGenericArguments().First();
            }

            if (itf is IConvertible && typeof (IConvertible).IsAssignableFrom(innerType))
            {
                itf = Convert.ChangeType(itf, innerType, Thread.CurrentThread.CurrentCulture);
            }
            else
            {
                var conversionHelper = TypeDescriptor.GetConverter(theType);

                if (null != conversionHelper && conversionHelper.CanConvertFrom(itf.GetType()))
                {
                    itf = conversionHelper.ConvertFrom(itf);
                }
            }
            return itf;
        }

        private static object InterfaceProjection(Type theType, object itf)
        {
            if (itf is IDictionary<string, object> && !(itf is AbstractShapeableExpando))
            {
                itf = new ShapeableExpando((IDictionary<string, object>) itf);
            }
            else
            {
                itf = new DynamicPropertiesToReflectablePropertiesProxy(itf);
            }

            itf = InvocationBinding.DynamicDressedAs(itf, theType);
            return itf;
        }

        private static bool IsDictionaryButNotExpando(object itf, bool gotType, Type theType)
        {
            return itf is IDictionary<string, object> &&
                   !(itf is AbstractShapeableExpando) &&
                   (!gotType || theType == typeof (object));
        }
    }
}