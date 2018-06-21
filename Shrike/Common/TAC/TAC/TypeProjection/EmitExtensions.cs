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
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace AppComponents.Build
{

    #region Classes

    public static class EmitExtensions
    {
        public static void EmitArray(this ILGenerator generator, Type arrayType, IList<Action<ILGenerator>> emitElements)
        {
            var local = generator.DeclareLocal(arrayType.MakeArrayType());
            generator.Emit(OpCodes.Ldc_I4, emitElements.Count);
            generator.Emit(OpCodes.Newarr, arrayType);
            generator.EmitStoreLocation(local.LocalIndex);

            for (var i = 0; i < emitElements.Count; i++)
            {
                generator.EmitLoadLocation(local.LocalIndex);
                generator.Emit(OpCodes.Ldc_I4, i);
                emitElements[i](generator);
                generator.Emit(OpCodes.Stelem_Ref);
            }
            generator.EmitLoadLocation(local.LocalIndex);
        }

        public static BranchTrueOverBlock EmitBranchTrue(this ILGenerator generator, Action<ILGenerator> condition)
        {
            condition(generator);
            return new BranchTrueOverBlock(generator);
        }

        public static void EmitCallInvokeFunc(this ILGenerator generator, Type funcType, bool isAction = false)
        {
            generator.Emit(OpCodes.Callvirt, funcType.GetMethodEvenIfGeneric("Invoke"));
        }

        public static void EmitCallsiteCreate(this ILGenerator generator, Type funcType)
        {
            generator.Emit(OpCodes.Call, typeof (CallSite<>).MakeGenericType(funcType)
                                             .GetMethodEvenIfGeneric("Create", new[] {typeof (CallSiteBinder)}));
        }

        public static void EmitCreateCSharpArgumentInfo(this ILGenerator generator, CSharpArgumentInfoFlags flag,
                                                        string name = null)
        {
            generator.Emit(OpCodes.Ldc_I4, (int) flag);
            if (String.IsNullOrEmpty(name))
                generator.Emit(OpCodes.Ldnull);
            else
                generator.Emit(OpCodes.Ldstr, name);
            generator.Emit(OpCodes.Call,
                           typeof (CSharpArgumentInfo).GetMethod("Create",
                                                                 new[]
                                                                     {typeof (CSharpArgumentInfoFlags), typeof (string)}));
        }

        public static void EmitDynamicBinaryOpBinder(this ILGenerator generator, CSharpBinderFlags flag,
                                                     ExpressionType exprType, Type context, params Type[] argTypes)
        {
            generator.Emit(OpCodes.Ldc_I4, (int) flag);
            generator.Emit(OpCodes.Ldc_I4, (int) exprType);
            generator.EmitTypeOf(context);
            var argumentList = new List<Action<ILGenerator>>
                                   {gen => gen.EmitCreateCSharpArgumentInfo(CSharpArgumentInfoFlags.None)};
            argumentList.AddRange(
                argTypes.Select(
                    tArg =>
                    (Action<ILGenerator>)
                    (gen => gen.EmitCreateCSharpArgumentInfo(CSharpArgumentInfoFlags.UseCompileTimeType))));
            generator.EmitArray(typeof (CSharpArgumentInfo), argumentList);

            generator.Emit(OpCodes.Call,
                           typeof (Binder).GetMethod("BinaryOperation",
                                                     new[]
                                                         {
                                                             typeof (CSharpBinderFlags), typeof (ExpressionType),
                                                             typeof (Type), typeof (CSharpArgumentInfo[])
                                                         }));
        }

        public static void EmitDynamicConvertBinder(this ILGenerator generator, CSharpBinderFlags flag, Type returnType,
                                                    Type context)
        {
            generator.Emit(OpCodes.Ldc_I4, (int) flag);
            generator.EmitTypeOf(returnType);
            generator.EmitTypeOf(context);
            generator.Emit(OpCodes.Call,
                           typeof (Binder).GetMethod("Convert",
                                                     new[] {typeof (CSharpBinderFlags), typeof (Type), typeof (Type)}));
        }

        public static void EmitDynamicGetBinder(this ILGenerator generator, CSharpBinderFlags flag, string name,
                                                Type context, params Type[] argTypes)
        {
            generator.Emit(OpCodes.Ldc_I4, (int) flag);
            if (!argTypes.Any())
                generator.Emit(OpCodes.Ldstr, name);
            generator.EmitTypeOf(context);
            var argumentList = new List<Action<ILGenerator>>
                                   {gen => gen.EmitCreateCSharpArgumentInfo(CSharpArgumentInfoFlags.None)};
            argumentList.AddRange(
                argTypes.Select(
                    tArg =>
                    (Action<ILGenerator>)
                    (gen => gen.EmitCreateCSharpArgumentInfo(CSharpArgumentInfoFlags.UseCompileTimeType))));
            generator.EmitArray(typeof (CSharpArgumentInfo), argumentList);
            if (!argTypes.Any())
                generator.Emit(OpCodes.Call,
                               typeof (Binder).GetMethod("GetMember",
                                                         new[]
                                                             {
                                                                 typeof (CSharpBinderFlags), typeof (string),
                                                                 typeof (Type)
                                                                 , typeof (CSharpArgumentInfo[])
                                                             }));
            else
                generator.Emit(OpCodes.Call,
                               typeof (Binder).GetMethod("GetIndex",
                                                         new[]
                                                             {
                                                                 typeof (CSharpBinderFlags), typeof (Type),
                                                                 typeof (CSharpArgumentInfo[])
                                                             }));
        }

        public static void EmitDynamicIsEventBinder(this ILGenerator generator, CSharpBinderFlags flag, string name,
                                                    Type context)
        {
            generator.Emit(OpCodes.Ldc_I4, (int) flag);
            generator.Emit(OpCodes.Ldstr, name);
            generator.EmitTypeOf(context);
            generator.Emit(OpCodes.Call,
                           typeof (Binder).GetMethod("IsEvent",
                                                     new[] {typeof (CSharpBinderFlags), typeof (string), typeof (Type)}));
        }

        public static void EmitDynamicMethodInvokeBinder(this ILGenerator generator, CSharpBinderFlags flag, string name,
                                                         Type context, ParameterInfo[] argInfo,
                                                         IEnumerable<string> argNames)
        {
            generator.Emit(OpCodes.Ldc_I4, (int) flag);
            generator.Emit(OpCodes.Ldstr, name);
            generator.Emit(OpCodes.Ldnull);
            generator.EmitTypeOf(context);
            var argumentList = new List<Action<ILGenerator>>
                                   {gen => gen.EmitCreateCSharpArgumentInfo(CSharpArgumentInfoFlags.None)};

            argumentList.AddRange(
                argInfo.Zip(argNames, (p, n) => new {p, n}).Select(arg => (Action<ILGenerator>) (gen =>
                                                                                                     {
                                                                                                         var start =
                                                                                                             CSharpArgumentInfoFlags
                                                                                                                 .
                                                                                                                 UseCompileTimeType;

                                                                                                         if (
                                                                                                             arg.p.
                                                                                                                 IsDefined
                                                                                                                 (typeof
                                                                                                                      (
                                                                                                                      DynamicAttribute
                                                                                                                      ),
                                                                                                                  true))
                                                                                                         {
                                                                                                             start =
                                                                                                                 CSharpArgumentInfoFlags
                                                                                                                     .
                                                                                                                     None;
                                                                                                         }

                                                                                                         if (arg.p.IsOut)
                                                                                                         {
                                                                                                             start |=
                                                                                                                 CSharpArgumentInfoFlags
                                                                                                                     .
                                                                                                                     IsOut;
                                                                                                         }
                                                                                                         else if (
                                                                                                             arg.p.
                                                                                                                 ParameterType
                                                                                                                 .
                                                                                                                 IsByRef)
                                                                                                         {
                                                                                                             start
                                                                                                                 |=
                                                                                                                 CSharpArgumentInfoFlags
                                                                                                                     .
                                                                                                                     IsRef;
                                                                                                         }

                                                                                                         if (
                                                                                                             !String.
                                                                                                                  IsNullOrEmpty
                                                                                                                  (arg.n))
                                                                                                         {
                                                                                                             start |=
                                                                                                                 CSharpArgumentInfoFlags
                                                                                                                     .
                                                                                                                     NamedArgument;
                                                                                                         }

                                                                                                         gen.
                                                                                                             EmitCreateCSharpArgumentInfo
                                                                                                             (start,
                                                                                                              arg.n);
                                                                                                         return;
                                                                                                     })));
            generator.EmitArray(typeof (CSharpArgumentInfo), argumentList);
            generator.Emit(OpCodes.Call,
                           typeof (Binder).GetMethod("InvokeMember",
                                                     new[]
                                                         {
                                                             typeof (CSharpBinderFlags), typeof (string),
                                                             typeof (IEnumerable<Type>), typeof (Type),
                                                             typeof (CSharpArgumentInfo[])
                                                         }));
        }

        public static void EmitDynamicSetBinder(this ILGenerator generator, CSharpBinderFlags flag, string name,
                                                Type context, params Type[] argTypes)
        {
            generator.Emit(OpCodes.Ldc_I4, (int) flag);
            if (argTypes.Length == 1)
                generator.Emit(OpCodes.Ldstr, name);
            generator.EmitTypeOf(context);
            var argumentList = new List<Action<ILGenerator>>
                                   {gen => gen.EmitCreateCSharpArgumentInfo(CSharpArgumentInfoFlags.None)};
            argumentList.AddRange(
                argTypes.Select(
                    tArg =>
                    (Action<ILGenerator>)
                    (gen => gen.EmitCreateCSharpArgumentInfo(CSharpArgumentInfoFlags.UseCompileTimeType))));
            generator.EmitArray(typeof (CSharpArgumentInfo), argumentList);

            if (argTypes.Length == 1)
                generator.Emit(OpCodes.Call,
                               typeof (Binder).GetMethod("SetMember",
                                                         new[]
                                                             {
                                                                 typeof (CSharpBinderFlags), typeof (string),
                                                                 typeof (Type)
                                                                 , typeof (CSharpArgumentInfo[])
                                                             }));
            else
                generator.Emit(OpCodes.Call,
                               typeof (Binder).GetMethod("SetIndex",
                                                         new[]
                                                             {
                                                                 typeof (CSharpBinderFlags), typeof (Type),
                                                                 typeof (CSharpArgumentInfo[])
                                                             }));
        }

        public static void EmitDynamicSetBinderDynamicParams(this ILGenerator generator, CSharpBinderFlags flag,
                                                             string name, Type context, params Type[] argTypes)
        {
            generator.Emit(OpCodes.Ldc_I4, (int) flag);
            if (argTypes.Length == 1)
                generator.Emit(OpCodes.Ldstr, name);
            generator.EmitTypeOf(context);
            var argumentList = new List<Action<ILGenerator>>
                                   {gen => gen.EmitCreateCSharpArgumentInfo(CSharpArgumentInfoFlags.None)};
            argumentList.AddRange(
                argTypes.Select(
                    tArg =>
                    (Action<ILGenerator>) (gen => gen.EmitCreateCSharpArgumentInfo(CSharpArgumentInfoFlags.None))));
            generator.EmitArray(typeof (CSharpArgumentInfo), argumentList);

            if (argTypes.Length == 1)
                generator.Emit(OpCodes.Call,
                               typeof (Binder).GetMethod("SetMember",
                                                         new[]
                                                             {
                                                                 typeof (CSharpBinderFlags), typeof (string),
                                                                 typeof (Type)
                                                                 , typeof (CSharpArgumentInfo[])
                                                             }));
            else
                generator.Emit(OpCodes.Call,
                               typeof (Binder).GetMethod("SetIndex",
                                                         new[]
                                                             {
                                                                 typeof (CSharpBinderFlags), typeof (Type),
                                                                 typeof (CSharpArgumentInfo[])
                                                             }));
        }

        public static void EmitInvocation(
            this ILGenerator generator,
            Action<ILGenerator> target,
            Action<ILGenerator> call,
            params Action<ILGenerator>[] parameters)
        {
            target(generator);
            foreach (var tParameter in parameters)
            {
                tParameter(generator);
            }
            call(generator);
        }

        public static void EmitLoadArgument(this ILGenerator generator, int location)
        {
            switch (location)
            {
                case 0:
                    generator.Emit(OpCodes.Ldarg_0);
                    return;
                case 1:
                    generator.Emit(OpCodes.Ldarg_1);
                    return;
                case 2:
                    generator.Emit(OpCodes.Ldarg_2);
                    return;
                case 3:
                    generator.Emit(OpCodes.Ldarg_3);
                    return;
                default:
                    generator.Emit(OpCodes.Ldarg, location);
                    return;
            }
        }

        public static void EmitLoadLocation(this ILGenerator generator, int location)
        {
            switch (location)
            {
                case 0:
                    generator.Emit(OpCodes.Ldloc_0);
                    return;
                case 1:
                    generator.Emit(OpCodes.Ldloc_1);
                    return;
                case 2:
                    generator.Emit(OpCodes.Ldloc_2);
                    return;
                case 3:
                    generator.Emit(OpCodes.Ldloc_3);
                    return;
                default:
                    generator.Emit(OpCodes.Ldloc, location);
                    return;
            }
        }

        public static void EmitStoreLocation(this ILGenerator generator, int location)
        {
            switch (location)
            {
                case 0:
                    generator.Emit(OpCodes.Stloc_0);
                    return;
                case 1:
                    generator.Emit(OpCodes.Stloc_1);
                    return;
                case 2:
                    generator.Emit(OpCodes.Stloc_2);
                    return;
                case 3:
                    generator.Emit(OpCodes.Stloc_3);
                    return;
                default:
                    generator.Emit(OpCodes.Stloc, location);
                    return;
            }
        }

        public static void EmitTypeOf(this ILGenerator generator, Type type)
        {
            generator.Emit(OpCodes.Ldtoken, type);
            var tTypeMeth = typeof (Type).GetMethod("GetTypeFromHandle", new[] {typeof (RuntimeTypeHandle)});
            generator.Emit(OpCodes.Call, tTypeMeth);
        }

        public static void EmitTypeOf(this ILGenerator generator, TypeToken type)
        {
            generator.Emit(OpCodes.Ldtoken, type.Token);
            var tTypeMeth = typeof (Type).GetMethod("GetTypeFromHandle", new[] {typeof (RuntimeTypeHandle)});
            generator.Emit(OpCodes.Call, tTypeMeth);
        }

        public static FieldInfo GetFieldEvenIfGeneric(this Type type, string fieldName)
        {
            if (type is TypeBuilder ||
                type.GetType().Name.Contains("TypeBuilder") ||
                type.GetType().Name.Contains("MonoGenericClass")
                )
            {
                var genericDefinition = type.GetGenericTypeDefinition();
                var field = genericDefinition.GetField(fieldName);
                return TypeBuilder.GetField(type, field);
            }
            return type.GetField(fieldName);
        }

        public static MethodInfo GetMethodEvenIfGeneric(this Type type, string methodName, Type[] argTypes)
        {
            if (type is TypeBuilder ||
                type.GetType().Name.Contains("TypeBuilder") ||
                type.GetType().Name.Contains("MonoGenericClass")
                )
            {
                var genericDefinition = type.GetGenericTypeDefinition();
                var methodInfo = genericDefinition.GetMethod(methodName, argTypes);
                return TypeBuilder.GetMethod(type, methodInfo);
            }
            return type.GetMethod(methodName, argTypes);
        }

        public static MethodInfo GetMethodEvenIfGeneric(this Type type, string methodName)
        {
            if (type is TypeBuilder ||
                type.GetType().Name.Contains("TypeBuilder") ||
                type.GetType().Name.Contains("MonoGenericClass")
                )
            {
                var genericDefinition = type.GetGenericTypeDefinition();
                var methodInfo = genericDefinition.GetMethod(methodName);
                return TypeBuilder.GetMethod(type, methodInfo);
            }
            return type.GetMethod(methodName);
        }

        #region Nested type: BranchTrueOverBlock

        public class BranchTrueOverBlock : IDisposable
        {
            private readonly ILGenerator _generator;
            private readonly Label _label;


            public BranchTrueOverBlock(ILGenerator generator)
            {
                _generator = generator;
                _label = generator.DefineLabel();
                _generator.Emit(OpCodes.Brtrue, _label);
            }

            #region IDisposable Members

            public void Dispose()
            {
                _generator.MarkLabel(_label);
            }

            #endregion
        }

        #endregion
    }

    #endregion Classes
}