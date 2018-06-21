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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using AppComponents.Build;
using Microsoft.CSharp.RuntimeBinder;

namespace AppComponents.Dynamic
{

    #region Classes

    public static class BuildProxy
    {
        private static AssemblyBuilder _ab;
        private static ModuleBuilder _builder;
        private static readonly IDictionary<TypeHasher, Type> _delegateCache = new Dictionary<TypeHasher, Type>();
        private static readonly object _delegateCacheLock = new object();
        internal static ModuleBuilder _tempBuilder;
        internal static AssemblyBuilder _tempSaveAssembly;
        private static readonly object _typeCacheLock = new object();
        private static readonly IDictionary<TypeHasher, Type> _typeHash = new Dictionary<TypeHasher, Type>();


        internal static ModuleBuilder Builder
        {
            get
            {
                if (_builder == null)
                {
                    var access = AssemblyBuilderAccess.Run;
                    var tPlainName = "TACInterfaceDynamicAssembly";


                    GenerateAssembly(tPlainName, access, ref _ab, ref _builder);
                }
                return _tempBuilder ?? _builder;
            }
        }


        private static ParameterAttributes AttributesForParam(ParameterInfo param)
        {
            return param.Attributes;
        }

        public static Type BuildType(Type contextType, Type mainInterface, params Type[] otherInterfaces)
        {
            lock (_typeCacheLock)
            {
                contextType = contextType.MaybeDetectArrayContext();
                var newHash = TypeHasher.Create(contextType, new[] {mainInterface}.Concat(otherInterfaces).ToArray());
                Type type;
                if (!_typeHash.TryGetValue(newHash, out type))
                {
                    type = BuildTypeHelper(Builder, contextType, new[] {mainInterface}.Concat(otherInterfaces).ToArray());
                    _typeHash[newHash] = type;
                }

                return _typeHash[newHash];
            }
        }

        public static Type BuildType(Type contextType, IDictionary<string, Type> informalInterface)
        {
            lock (_typeCacheLock)
            {
                var newHash = TypeHasher.Create(contextType, informalInterface);
                Type type;
                if (!_typeHash.TryGetValue(newHash, out type))
                {
                    type = BuildTypeHelper(Builder, contextType, informalInterface);

                    _typeHash[newHash] = type;
                }

                return _typeHash[newHash];
            }
        }

        private static Type BuildTypeHelper(ModuleBuilder builder, Type contextType,
                                            IDictionary<string, Type> informalInterface)
        {
            var theBuilder = builder.DefineType(
                string.Format("DressedAs_{0}_{1}", "InformalInterface", Guid.NewGuid().ToString("N")),
                TypeAttributes.Public | TypeAttributes.Class,
                typeof (AbstractTypeProjectionProxy));


            foreach (var tInterface in informalInterface)
            {
                MakePropertyDescribedProperty(builder, theBuilder, contextType, tInterface.Key, tInterface.Value);
            }
            var type = theBuilder.CreateType();
            return type;
        }

        private static Type BuildTypeHelper(ModuleBuilder builder, Type contextType, params Type[] interfaces)
        {
            var interfacesMainList = interfaces.Distinct().ToArray();
            var theBuilder = builder.DefineType(
                string.Format("DressedAs_{0}_{1}", interfacesMainList.First().Name, Guid.NewGuid().ToString("N")),
                TypeAttributes.Public | TypeAttributes.Class,
                typeof (AbstractTypeProjectionProxy), interfacesMainList);

            theBuilder.SetCustomAttribute(
                new CustomAttributeBuilder(
                    typeof (DressedAsAttribute).GetConstructor(new[] {typeof (Type).MakeArrayType(), typeof (Type)}),
                    new object[] {interfaces, contextType}));
            theBuilder.SetCustomAttribute(
                new CustomAttributeBuilder(typeof (SerializableAttribute).GetConstructor(Type.EmptyTypes),
                                           new object[] {}));


            var theInterfaces = interfacesMainList.Concat(interfacesMainList.SelectMany(it => it.GetInterfaces()));


            var propertyNameHashes = new HashSet<string>();
            var methodNameHashes = new HashSet<MethodSignatureHash>();

            object theAttribute = null;
            foreach (var itf in theInterfaces.Distinct())
            {
                if (itf != null && theAttribute == null)
                {
                    var customAttributes = itf.GetCustomAttributesData();
                    foreach (var eachCustomAttribute in
                        customAttributes.Where(
                            it => typeof (DefaultMemberAttribute).IsAssignableFrom(it.Constructor.DeclaringType)))
                    {
                        try
                        {
                            theBuilder.SetCustomAttribute(GetAttributeBuilder(eachCustomAttribute));
                        }
                        catch
                        {
                        }
                    }
                }


                foreach (var tInfo in itf.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    MakeProperty(builder, tInfo, theBuilder, contextType, defaultImp: propertyNameHashes.Add(tInfo.Name));
                }
                foreach (
                    var tInfo in
                        itf.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(it => !it.IsSpecialName))
                {
                    MakeMethod(builder, tInfo, theBuilder, contextType,
                               defaultImp: methodNameHashes.Add(new MethodSignatureHash(tInfo)));
                }
                foreach (
                    var tInfo in
                        itf.GetEvents(BindingFlags.Public | BindingFlags.Instance).Where(it => !it.IsSpecialName))
                {
                    MakeEvent(builder, tInfo, theBuilder, contextType, defaultImp: propertyNameHashes.Add(tInfo.Name));
                }
            }
            var type = theBuilder.CreateType();
            return type;
        }

        private static object CustomAttributeTypeArgument(CustomAttributeTypedArgument argument)
        {
            if (argument.Value is ReadOnlyCollection<CustomAttributeTypedArgument>)
            {
                var theValue = argument.Value as ReadOnlyCollection<CustomAttributeTypedArgument>;
                return
                    new ArrayList(theValue.Select(it => it.Value).ToList()).ToArray(
                        argument.ArgumentType.GetElementType());
            }

            return argument.Value;
        }

        private static TypeBuilder DefineBuilderForCallSite(ModuleBuilder builder, string callSiteInvokeName)
        {
            return builder.DefineType(callSiteInvokeName,
                                      TypeAttributes.NotPublic
                                      | TypeAttributes.Sealed
                                      | TypeAttributes.AutoClass
                                      | TypeAttributes.BeforeFieldInit
                                      | TypeAttributes.Abstract
                );
        }

        private static Type DefineCallsiteField(this TypeBuilder builder, string name, Type returnType,
                                                params Type[] argTypes)
        {
            Type tFuncType = GenerateCallSiteFuncType(argTypes, returnType);
            Type tReturnType = typeof (CallSite<>).MakeGenericType(tFuncType);

            builder.DefineField(name, tReturnType, FieldAttributes.Static | FieldAttributes.Public);
            return tFuncType;
        }

        private static Type DefineCallsiteFieldForMethod(this TypeBuilder builder, string name, Type returnType,
                                                         IEnumerable<Type> argTypes, MethodInfo info)
        {
            Type tFuncType = GenerateCallSiteFuncType(argTypes, returnType, info, builder);
            Type tReturnType = typeof (CallSite<>).MakeGenericType(tFuncType);

            builder.DefineField(name, tReturnType, FieldAttributes.Static | FieldAttributes.Public);
            return tFuncType;
        }

        private static void EmitMethodBody(
            string name,
            Type[] paramTypes,
            ParameterInfo[] paramInfo,
            Type returnType,
            string convert,
            string invokeMethod,
            MethodBuilder methodBuilder,
            Type callSite,
            Type contextType,
            Type convertFuncType,
            Type invokeFuncType,
            IEnumerable<string> argumentNames
            )
        {
            var cilGenerationMachine = methodBuilder.GetILGenerator();

            var convertField = callSite.GetFieldEvenIfGeneric(convert);
            if (returnType != typeof (void))
            {
                using (cilGenerationMachine.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, convertField)))
                {
                    cilGenerationMachine.EmitDynamicConvertBinder(CSharpBinderFlags.None, returnType, contextType);
                    cilGenerationMachine.EmitCallsiteCreate(convertFuncType);
                    cilGenerationMachine.Emit(OpCodes.Stsfld, convertField);
                }
            }

            var invokeField = callSite.GetFieldEvenIfGeneric(invokeMethod);

            using (cilGenerationMachine.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, invokeField)))
            {
                cilGenerationMachine.EmitDynamicMethodInvokeBinder(
                    returnType == typeof (void) ? CSharpBinderFlags.ResultDiscarded : CSharpBinderFlags.None, name,
                    contextType, paramInfo, argumentNames);
                cilGenerationMachine.EmitCallsiteCreate(invokeFuncType);
                cilGenerationMachine.Emit(OpCodes.Stsfld, invokeField);
            }

            if (returnType != typeof (void))
            {
                cilGenerationMachine.Emit(OpCodes.Ldsfld, convertField);
                cilGenerationMachine.Emit(OpCodes.Ldfld,
                                          typeof (CallSite<>).MakeGenericType(convertFuncType).GetFieldEvenIfGeneric(
                                              "Target"));
                cilGenerationMachine.Emit(OpCodes.Ldsfld, convertField);
            }

            cilGenerationMachine.Emit(OpCodes.Ldsfld, invokeField);
            cilGenerationMachine.Emit(OpCodes.Ldfld,
                                      typeof (CallSite<>).MakeGenericType(invokeFuncType).GetFieldEvenIfGeneric("Target"));
            cilGenerationMachine.Emit(OpCodes.Ldsfld, invokeField);
            cilGenerationMachine.Emit(OpCodes.Ldarg_0);
            cilGenerationMachine.Emit(OpCodes.Call,
                                      typeof (AbstractTypeProjectionProxy).GetProperty("OriginalTarget").GetGetMethod());
            for (var i = 1; i <= paramTypes.Length; i++)
            {
                cilGenerationMachine.EmitLoadArgument(i);
            }
            cilGenerationMachine.EmitCallInvokeFunc(invokeFuncType, returnType == typeof (void));
            if (returnType != typeof (void))
            {
                cilGenerationMachine.EmitCallInvokeFunc(convertFuncType);
            }

            cilGenerationMachine.Emit(OpCodes.Ret);
        }

        private static void EmitProperty(
            PropertyInfo info,
            string name,
            string convertGet,
            Type tGetReturnType,
            string invokeGet,
            Type[] indexParamTypes,
            MethodInfo setMethod,
            string invokeSet,
            Type[] setParamTypes,
            TypeBuilder typeBuilder,
            MethodBuilder getMethodBuilder,
            Type callSite,
            Type contextType,
            Type tConvertFuncType,
            Type invokeGetFuncType,
            PropertyBuilder tMp,
            Type invokeSetFuncType, bool defaultImp)
        {
            if (indexParamTypes == null) throw new ArgumentNullException("indexParamTypes");
            var tIlGen = getMethodBuilder.GetILGenerator();

            var tConvertCallsiteField = callSite.GetFieldEvenIfGeneric(convertGet);
            var tReturnLocal = tIlGen.DeclareLocal(tGetReturnType);


            using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tConvertCallsiteField)))
            {
                tIlGen.EmitDynamicConvertBinder(CSharpBinderFlags.None, tGetReturnType, contextType);
                tIlGen.EmitCallsiteCreate(tConvertFuncType);
                tIlGen.Emit(OpCodes.Stsfld, tConvertCallsiteField);
            }

            var tInvokeGetCallsiteField = callSite.GetFieldEvenIfGeneric(invokeGet);

            using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tInvokeGetCallsiteField)))
            {
                tIlGen.EmitDynamicGetBinder(CSharpBinderFlags.None, name, contextType, indexParamTypes);
                tIlGen.EmitCallsiteCreate(invokeGetFuncType);
                tIlGen.Emit(OpCodes.Stsfld, tInvokeGetCallsiteField);
            }


            tIlGen.Emit(OpCodes.Ldsfld, tConvertCallsiteField);
            tIlGen.Emit(OpCodes.Ldfld, tConvertCallsiteField.FieldType.GetFieldEvenIfGeneric("Target"));
            tIlGen.Emit(OpCodes.Ldsfld, tConvertCallsiteField);
            tIlGen.Emit(OpCodes.Ldsfld, tInvokeGetCallsiteField);
            tIlGen.Emit(OpCodes.Ldfld, tInvokeGetCallsiteField.FieldType.GetFieldEvenIfGeneric("Target"));
            tIlGen.Emit(OpCodes.Ldsfld, tInvokeGetCallsiteField);
            tIlGen.Emit(OpCodes.Ldarg_0);
            tIlGen.Emit(OpCodes.Call, typeof (AbstractTypeProjectionProxy).GetProperty("OriginalTarget").GetGetMethod());
            for (var i = 1; i <= indexParamTypes.Length; i++)
            {
                tIlGen.EmitLoadArgument(i);
            }
            tIlGen.EmitCallInvokeFunc(invokeGetFuncType);
            tIlGen.EmitCallInvokeFunc(tConvertFuncType);
            tIlGen.EmitStoreLocation(tReturnLocal.LocalIndex);
            var tReturnLabel = tIlGen.DefineLabel();
            tIlGen.Emit(OpCodes.Br_S, tReturnLabel);
            tIlGen.MarkLabel(tReturnLabel);
            tIlGen.EmitLoadLocation(tReturnLocal.LocalIndex);
            tIlGen.Emit(OpCodes.Ret);
            tMp.SetGetMethod(getMethodBuilder);

            if (setMethod != null)
            {
                MethodAttributes tPublicPrivate = MethodAttributes.Public;
                var tPrefixedSet = setMethod.Name;
                if (!defaultImp)
                {
                    tPublicPrivate = MethodAttributes.Private;
                    tPrefixedSet = String.Format("{0}.{1}", info.DeclaringType.FullName, tPrefixedSet);
                }


                var tSetMethodBuilder = typeBuilder.DefineMethod(tPrefixedSet,
                                                                 tPublicPrivate | MethodAttributes.SpecialName |
                                                                 MethodAttributes.HideBySig | MethodAttributes.Virtual |
                                                                 MethodAttributes.Final | MethodAttributes.NewSlot,
                                                                 null,
                                                                 setParamTypes);

                if (!defaultImp)
                {
                    typeBuilder.DefineMethodOverride(tSetMethodBuilder, info.GetSetMethod());
                }

                foreach (var tParam in info.GetSetMethod().GetParameters())
                {
                    tSetMethodBuilder.DefineParameter(tParam.Position + 1, AttributesForParam(tParam), tParam.Name);
                }

                tIlGen = tSetMethodBuilder.GetILGenerator();
                var tSetCallsiteField = callSite.GetFieldEvenIfGeneric(invokeSet);

                using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tSetCallsiteField)))
                {
                    tIlGen.EmitDynamicSetBinder(CSharpBinderFlags.None, name, contextType, setParamTypes);
                    tIlGen.EmitCallsiteCreate(invokeSetFuncType);
                    tIlGen.Emit(OpCodes.Stsfld, tSetCallsiteField);
                }
                tIlGen.Emit(OpCodes.Ldsfld, tSetCallsiteField);
                tIlGen.Emit(OpCodes.Ldfld, tSetCallsiteField.FieldType.GetFieldEvenIfGeneric("Target"));
                tIlGen.Emit(OpCodes.Ldsfld, tSetCallsiteField);
                tIlGen.Emit(OpCodes.Ldarg_0);
                tIlGen.Emit(OpCodes.Call, typeof (AbstractTypeProjectionProxy).GetProperty("OriginalTarget").GetGetMethod());
                for (var i = 1; i <= setParamTypes.Length; i++)
                {
                    tIlGen.EmitLoadArgument(i);
                }
                tIlGen.EmitCallInvokeFunc(invokeSetFuncType);
                tIlGen.Emit(OpCodes.Pop);
                tIlGen.Emit(OpCodes.Ret);
                tMp.SetSetMethod(tSetMethodBuilder);
            }
        }

        private static IEnumerable<Type> FlattenGenericParameters(Type type)
        {
            if (type.IsByRef || type.IsArray || type.IsPointer)
            {
                return FlattenGenericParameters(type.GetElementType());
            }

            if (type.IsGenericParameter)
                return new[] {type};
            if (type.ContainsGenericParameters)
            {
                return type.GetGenericArguments().SelectMany(FlattenGenericParameters);
            }
            return new Type[] {};
        }

        private static void GenerateAssembly(string name, AssemblyBuilderAccess access, ref AssemblyBuilder ab,
                                             ref ModuleBuilder mb)
        {
            var tName = new AssemblyName(name);

            ab =
                AppDomain.CurrentDomain.DefineDynamicAssembly(
                    tName,
                    access);

#if !SILVERLIGHT
            if (access == AssemblyBuilderAccess.RunAndSave || access == AssemblyBuilderAccess.Save)
                mb = ab.DefineDynamicModule("MainModule", string.Format("{0}.dll", tName.Name));
            else
#endif
                mb = ab.DefineDynamicModule("MainModule");
        }

        internal static Type GenerateCallSiteFuncType(IEnumerable<Type> argTypes, Type returnType,
                                                      MethodInfo methodInfo = null, TypeBuilder builder = null)
        {
            bool tIsFunc = returnType != typeof (void);


            var tList = new List<Type> {typeof (CallSite), typeof (object)};
            tList.AddRange(argTypes.Select(it => (it.IsNotPublic && !it.IsByRef) ? typeof (object) : it));


            lock (_delegateCacheLock)
            {
                TypeHasher tHash;

                if ((tList.Any(it => it.IsByRef) || tList.Count > 16) && methodInfo != null)
                {
                    tHash = TypeHasher.Create(strictOrder: true, moreTypes: methodInfo);
                }
                else
                {
                    tHash = TypeHasher.Create(strictOrder: true, moreTypes: tList.Concat(new[] {returnType}).ToArray());
                }

                Type tType = null;
                if (_delegateCache.TryGetValue(tHash, out tType))
                {
                    return tType;
                }

                if (tList.Any(it => it.IsByRef)
                    || (tIsFunc && tList.Count >= InvocationMapping.FuncPrototypes.Length)
                    || (!tIsFunc && tList.Count >= InvocationMapping.ActionPrototypes.Length))
                {
                    tType = GenerateFullDelegate(builder, returnType, tList, methodInfo);


                    _delegateCache[tHash] = tType;
                    return tType;
                }


                if (tIsFunc)
                    tList.Add(returnType);

                var tFuncGeneric = InvocationBinding.GenericDelegateType(tList.Count, !tIsFunc);


                var tFuncType = tFuncGeneric.MakeGenericType(tList.ToArray());

                _delegateCache[tHash] = tFuncType;

                return tFuncType;
            }
        }

        private static Type GenerateFullDelegate(TypeBuilder builder, Type returnType, IEnumerable<Type> types,
                                                 MethodInfo info = null)
        {
            var tBuilder = Builder.DefineType(
                string.Format("TAC_{0}_{1}", "Delegate", Guid.NewGuid().ToString("N")),
                TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.Public,
                typeof (MulticastDelegate));

            var tReplacedTypes = GetParamTypes(tBuilder, info);

            var tParamTypes = info == null
                                  ? types.ToList()
                                  : info.GetParameters().Select(it => it.ParameterType).ToList();

            if (tReplacedTypes != null)
            {
                tParamTypes = tReplacedTypes.Item2.ToList();
            }

            if (info != null)
            {
                tParamTypes.Insert(0, typeof (object));
                tParamTypes.Insert(0, typeof (CallSite));
            }

            var tCon = tBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig |
                MethodAttributes.RTSpecialName, CallingConventions.Standard,
                new[] {typeof (object), typeof (IntPtr)});

            tCon.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);

            var tMethod = tBuilder.DefineMethod("Invoke",
                                                MethodAttributes.Public | MethodAttributes.HideBySig |
                                                MethodAttributes.NewSlot |
                                                MethodAttributes.Virtual);

            tMethod.SetReturnType(returnType);
            tMethod.SetParameters(tParamTypes.ToArray());

            if (info != null)
            {
                foreach (var tParam in info.GetParameters())
                {
                    //+3 because of the callsite and target are added
                    tMethod.DefineParameter(tParam.Position + 3, AttributesForParam(tParam), tParam.Name);
                }
            }

            tMethod.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);


            return tBuilder.CreateType();
        }

        private static CustomAttributeBuilder GetAttributeBuilder(CustomAttributeData data)
        {
            var propertyInfos = new List<PropertyInfo>();
            var propertyValues = new List<object>();
            var fieldInfos = new List<FieldInfo>();
            var fieldValues = new List<object>();

            if (data.NamedArguments != null)
            {
                foreach (var namedArg in data.NamedArguments)
                {
                    var fi = namedArg.MemberInfo as FieldInfo;
                    var pi = namedArg.MemberInfo as PropertyInfo;

                    if (fi != null)
                    {
                        fieldInfos.Add(fi);
                        fieldValues.Add(namedArg.TypedValue.Value);
                    }
                    else if (pi != null)
                    {
                        propertyInfos.Add(pi);
                        propertyValues.Add(namedArg.TypedValue.Value);
                    }
                }
            }

            return new CustomAttributeBuilder(
                data.Constructor,
                data.ConstructorArguments.Select(CustomAttributeTypeArgument).ToArray(),
                propertyInfos.ToArray(),
                propertyValues.ToArray(),
                fieldInfos.ToArray(),
                fieldValues.ToArray());
        }

        private static Tuple<Type, Type[]> GetParamTypes(dynamic builder, MethodInfo info)
        {
            if (info == null)
                return null;


            var paramTypes = info.GetParameters().Select(it => it.ParameterType).ToArray();
            var returnType = typeof (void);
            if (info.ReturnParameter != null)
                returnType = info.ReturnParameter.ParameterType;

            var genericParameters = paramTypes
                .SelectMany(FlattenGenericParameters)
                .Distinct().ToDictionary(it => it.GenericParameterPosition,
                                         it => new {Type = it, Gen = default(GenericTypeParameterBuilder)});
            var parms = genericParameters;
            var returnParms =
                FlattenGenericParameters(returnType).Where(it => !parms.ContainsKey(it.GenericParameterPosition));
            foreach (var eachReturnParm in returnParms)
                genericParameters.Add(eachReturnParm.GenericParameterPosition,
                                      new {Type = eachReturnParm, Gen = default(GenericTypeParameterBuilder)});
            var orderedGenericParameters = genericParameters.OrderBy(it => it.Key).Select(it => it.Value.Type.Name);
            if (orderedGenericParameters.Any())
            {
                GenericTypeParameterBuilder[] builders =
                    builder.DefineGenericParameters(orderedGenericParameters.ToArray());
                var typeToBuilderMap = genericParameters.ToDictionary(param => param.Value.Type,
                                                                      param => builders[param.Key]);

                returnType = ReplaceTypeWithGenericBuilder(returnType, typeToBuilderMap);
                if (typeToBuilderMap.ContainsKey(returnType))
                {
                    returnType = typeToBuilderMap[returnType];
                }
                paramTypes = paramTypes.Select(it => ReplaceTypeWithGenericBuilder(it, typeToBuilderMap)).ToArray();
                return Tuple.Create(returnType, paramTypes);
            }
            return null;
        }

        private static void MakeEvent(ModuleBuilder builder, EventInfo info, TypeBuilder typeBuilder, Type contextType,
                                      bool defaultImp)
        {
            var name = info.Name;
            var addMethod = info.GetAddMethod();
            var removeMethod = info.GetRemoveMethod();
            var returnType = info.EventHandlerType;


            var callSiteInvokeName = string.Format("TAC_Callsite_{1}_{0}", Guid.NewGuid().ToString("N"), name);
            var callSiteBuilder = DefineBuilderForCallSite(builder, callSiteInvokeName);


            var invokeIsEvent = "Invoke_IsEvent";
            var invokeIsEventFuncType = callSiteBuilder.DefineCallsiteField(invokeIsEvent, typeof (bool));


            var invokeAddAssign = "Invoke_AddAssign";
            var invokeAddAssignFuncType = callSiteBuilder.DefineCallsiteField(invokeAddAssign, typeof (object),
                                                                              returnType);

            var invokeSubstractAssign = "Invoke_SubtractAssign";
            var invokeSubstractAssignFuncType = callSiteBuilder.DefineCallsiteField(invokeSubstractAssign,
                                                                                    typeof (object), returnType);

            var addParameterTypes = removeMethod.GetParameters().Select(it => it.ParameterType).ToArray();
            var invokeAdd = "Invoke_Add";
            var invokeAddFuncType = callSiteBuilder.DefineCallsiteField(invokeAdd, typeof (object), addParameterTypes);

            var removeParameterTypes = removeMethod.GetParameters().Select(it => it.ParameterType).ToArray();
            var invokeRemove = "Invoke_Remove";
            var invokeRemoveFuncType = callSiteBuilder.DefineCallsiteField(invokeRemove, typeof (object),
                                                                           removeParameterTypes);

            var invokeGet = "Invoke_Get";
            var invokeGetFuncType = callSiteBuilder.DefineCallsiteField(invokeGet, typeof (object));

            var invokeSet = "Invoke_Set";

            var invokeSetFuncType = callSiteBuilder.DefineCallsiteField(invokeSet, typeof (object), typeof (object));

            var theCallSite = callSiteBuilder.CreateType();

            var eventBuilder = typeBuilder.DefineEvent(name, EventAttributes.None, returnType);

            //AddMethod
            var tPublicPrivate = MethodAttributes.Public;
            var tAddPrefixName = addMethod.Name;
            if (!defaultImp)
            {
                tAddPrefixName = String.Format("{0}.{1}", info.DeclaringType.FullName, tAddPrefixName);

                tPublicPrivate = MethodAttributes.Private;
            }

            var tAddBuilder = typeBuilder.DefineMethod(tAddPrefixName,
                                                       tPublicPrivate | MethodAttributes.SpecialName |
                                                       MethodAttributes.HideBySig | MethodAttributes.Virtual |
                                                       MethodAttributes.Final | MethodAttributes.NewSlot,
                                                       typeof (void),
                                                       addParameterTypes);

            if (!defaultImp)
            {
                typeBuilder.DefineMethodOverride(tAddBuilder, info.GetAddMethod());
            }


            foreach (var tParam in addMethod.GetParameters())
            {
                tAddBuilder.DefineParameter(tParam.Position + 1, AttributesForParam(tParam), tParam.Name);
            }


            var tIlGen = tAddBuilder.GetILGenerator();

            var tIsEventField = theCallSite.GetFieldEvenIfGeneric(invokeIsEvent);

            using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tIsEventField)))
            {
                tIlGen.EmitDynamicIsEventBinder(CSharpBinderFlags.None, name, contextType);
                tIlGen.EmitCallsiteCreate(invokeIsEventFuncType);
                tIlGen.Emit(OpCodes.Stsfld, tIsEventField);
            }

            var tSetField = theCallSite.GetFieldEvenIfGeneric(invokeSet);
            var tGetField = theCallSite.GetFieldEvenIfGeneric(invokeGet);


            using (tIlGen.EmitBranchTrue(
                load => load.EmitInvocation(
                    target => target.EmitInvocation(
                        t => t.Emit(OpCodes.Ldsfld, tIsEventField),
                        i => i.Emit(OpCodes.Ldfld, tIsEventField.FieldType.GetFieldEvenIfGeneric("Target"))
                                  ),
                    invoke => invoke.EmitCallInvokeFunc(invokeIsEventFuncType),
                    param => param.Emit(OpCodes.Ldsfld, tIsEventField),
                    param => param.EmitInvocation(
                        t => t.Emit(OpCodes.Ldarg_0),
                        i =>
                        i.Emit(OpCodes.Call, typeof (AbstractTypeProjectionProxy).GetProperty("OriginalTarget").GetGetMethod())
                                 )
                            )
                )
                ) //if IsEvent Not True
            {
                using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tSetField)))
                {
                    tIlGen.EmitDynamicSetBinderDynamicParams(CSharpBinderFlags.ValueFromCompoundAssignment, name,
                                                             contextType, typeof (Object));
                    tIlGen.EmitCallsiteCreate(invokeSetFuncType);
                    tIlGen.Emit(OpCodes.Stsfld, tSetField);
                }

                var tAddAssigneField = theCallSite.GetFieldEvenIfGeneric(invokeAddAssign);

                using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tAddAssigneField)))
                {
                    tIlGen.EmitDynamicBinaryOpBinder(CSharpBinderFlags.None, ExpressionType.AddAssign, contextType,
                                                     returnType);
                    tIlGen.EmitCallsiteCreate(invokeAddAssignFuncType);
                    tIlGen.Emit(OpCodes.Stsfld, tAddAssigneField);
                }

                using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tGetField)))
                {
                    tIlGen.EmitDynamicGetBinder(CSharpBinderFlags.None, name, contextType);
                    tIlGen.EmitCallsiteCreate(invokeGetFuncType);
                    tIlGen.Emit(OpCodes.Stsfld, tGetField);
                }


                tIlGen.Emit(OpCodes.Ldsfld, tSetField);
                tIlGen.Emit(OpCodes.Ldfld, tSetField.FieldType.GetFieldEvenIfGeneric("Target"));
                tIlGen.Emit(OpCodes.Ldsfld, tSetField);
                tIlGen.Emit(OpCodes.Ldarg_0);
                tIlGen.Emit(OpCodes.Call, typeof (AbstractTypeProjectionProxy).GetProperty("OriginalTarget").GetGetMethod());

                tIlGen.Emit(OpCodes.Ldsfld, tAddAssigneField);
                tIlGen.Emit(OpCodes.Ldfld, tAddAssigneField.FieldType.GetFieldEvenIfGeneric("Target"));
                tIlGen.Emit(OpCodes.Ldsfld, tAddAssigneField);

                tIlGen.Emit(OpCodes.Ldsfld, tGetField);
                tIlGen.Emit(OpCodes.Ldfld, tGetField.FieldType.GetFieldEvenIfGeneric("Target"));
                tIlGen.Emit(OpCodes.Ldsfld, tGetField);
                tIlGen.Emit(OpCodes.Ldarg_0);
                tIlGen.Emit(OpCodes.Call, typeof (AbstractTypeProjectionProxy).GetProperty("OriginalTarget").GetGetMethod());

                tIlGen.EmitCallInvokeFunc(invokeGetFuncType);

                tIlGen.Emit(OpCodes.Ldarg_1);
                tIlGen.EmitCallInvokeFunc(invokeAddAssignFuncType);

                tIlGen.EmitCallInvokeFunc(invokeSetFuncType);
                tIlGen.Emit(OpCodes.Pop);
                tIlGen.Emit(OpCodes.Ret);
            }

            var tAddCallSiteField = theCallSite.GetFieldEvenIfGeneric(invokeAdd);

            using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tAddCallSiteField)))
            {
                tIlGen.EmitDynamicMethodInvokeBinder(
                    CSharpBinderFlags.InvokeSpecialName | CSharpBinderFlags.ResultDiscarded,
                    addMethod.Name,
                    contextType,
                    addMethod.GetParameters(),
                    Enumerable.Repeat(default(string),
                                      addParameterTypes.Length));
                tIlGen.EmitCallsiteCreate(invokeAddFuncType);
                tIlGen.Emit(OpCodes.Stsfld, tAddCallSiteField);
            }
            tIlGen.Emit(OpCodes.Ldsfld, tAddCallSiteField);
            tIlGen.Emit(OpCodes.Ldfld, tAddCallSiteField.FieldType.GetFieldEvenIfGeneric("Target"));
            tIlGen.Emit(OpCodes.Ldsfld, tAddCallSiteField);
            tIlGen.Emit(OpCodes.Ldarg_0);
            tIlGen.Emit(OpCodes.Call, typeof (AbstractTypeProjectionProxy).GetProperty("OriginalTarget").GetGetMethod());
            for (var i = 1; i <= addParameterTypes.Length; i++)
            {
                tIlGen.EmitLoadArgument(i);
            }
            tIlGen.EmitCallInvokeFunc(invokeAddFuncType);
            tIlGen.Emit(OpCodes.Pop);
            tIlGen.Emit(OpCodes.Ret);

            eventBuilder.SetAddOnMethod(tAddBuilder);

            var tRemovePrefixName = removeMethod.Name;
            if (!defaultImp)
            {
                tRemovePrefixName = String.Format("{0}.{1}", info.DeclaringType.FullName, tRemovePrefixName);
            }

            //Remove Method
            var tRemoveBuilder = typeBuilder.DefineMethod(tRemovePrefixName,
                                                          tPublicPrivate | MethodAttributes.SpecialName |
                                                          MethodAttributes.HideBySig | MethodAttributes.Virtual |
                                                          MethodAttributes.Final | MethodAttributes.NewSlot,
                                                          typeof (void),
                                                          addParameterTypes);
            if (!defaultImp)
            {
                typeBuilder.DefineMethodOverride(tRemoveBuilder, info.GetRemoveMethod());
            }

            foreach (var tParam in removeMethod.GetParameters())
            {
                tRemoveBuilder.DefineParameter(tParam.Position + 1, AttributesForParam(tParam), tParam.Name);
            }


            tIlGen = tRemoveBuilder.GetILGenerator();


            using (tIlGen.EmitBranchTrue(load => load.Emit(OpCodes.Ldsfld, tIsEventField)))
            {
                tIlGen.EmitDynamicIsEventBinder(CSharpBinderFlags.None, name, contextType);
                tIlGen.EmitCallsiteCreate(invokeIsEventFuncType);
                tIlGen.Emit(OpCodes.Stsfld, tIsEventField);
            }

            using (tIlGen.EmitBranchTrue(
                load => load.EmitInvocation(
                    target => target.EmitInvocation(
                        t => t.Emit(OpCodes.Ldsfld, tIsEventField),
                        i => i.Emit(OpCodes.Ldfld, tIsEventField.FieldType.GetFieldEvenIfGeneric("Target"))
                                  ),
                    invoke => invoke.EmitCallInvokeFunc(invokeIsEventFuncType),
                    param => param.Emit(OpCodes.Ldsfld, tIsEventField),
                    param => param.EmitInvocation(
                        t => t.Emit(OpCodes.Ldarg_0),
                        i =>
                        i.Emit(OpCodes.Call, typeof (AbstractTypeProjectionProxy).GetProperty("OriginalTarget").GetGetMethod())
                                 )
                            )
                )
                ) //if IsEvent Not True
            {
                using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tSetField)))
                {
                    tIlGen.EmitDynamicSetBinderDynamicParams(CSharpBinderFlags.ValueFromCompoundAssignment, name,
                                                             contextType, returnType);
                    tIlGen.EmitCallsiteCreate(invokeSetFuncType);
                    tIlGen.Emit(OpCodes.Stsfld, tSetField);
                }

                var tSubrtractAssignField = theCallSite.GetFieldEvenIfGeneric(invokeSubstractAssign);

                using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tSubrtractAssignField)))
                {
                    tIlGen.EmitDynamicBinaryOpBinder(CSharpBinderFlags.None, ExpressionType.SubtractAssign, contextType,
                                                     returnType);
                    tIlGen.EmitCallsiteCreate(invokeSubstractAssignFuncType);
                    tIlGen.Emit(OpCodes.Stsfld, tSubrtractAssignField);
                }


                using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tGetField)))
                {
                    tIlGen.EmitDynamicGetBinder(CSharpBinderFlags.None, name, contextType);
                    tIlGen.EmitCallsiteCreate(invokeGetFuncType);
                    tIlGen.Emit(OpCodes.Stsfld, tGetField);
                }

                tIlGen.Emit(OpCodes.Ldsfld, tSetField);
                tIlGen.Emit(OpCodes.Ldfld, tSetField.FieldType.GetFieldEvenIfGeneric("Target"));
                tIlGen.Emit(OpCodes.Ldsfld, tSetField);
                tIlGen.Emit(OpCodes.Ldarg_0);
                tIlGen.Emit(OpCodes.Call, typeof (AbstractTypeProjectionProxy).GetProperty("OriginalTarget").GetGetMethod());

                tIlGen.Emit(OpCodes.Ldsfld, tSubrtractAssignField);
                tIlGen.Emit(OpCodes.Ldfld, tSubrtractAssignField.FieldType.GetFieldEvenIfGeneric("Target"));
                tIlGen.Emit(OpCodes.Ldsfld, tSubrtractAssignField);

                tIlGen.Emit(OpCodes.Ldsfld, tGetField);
                tIlGen.Emit(OpCodes.Ldfld, tGetField.FieldType.GetFieldEvenIfGeneric("Target"));
                tIlGen.Emit(OpCodes.Ldsfld, tGetField);
                tIlGen.Emit(OpCodes.Ldarg_0);
                tIlGen.Emit(OpCodes.Call, typeof (AbstractTypeProjectionProxy).GetProperty("OriginalTarget").GetGetMethod());

                tIlGen.EmitCallInvokeFunc(invokeGetFuncType);

                tIlGen.Emit(OpCodes.Ldarg_1);
                tIlGen.EmitCallInvokeFunc(invokeSubstractAssignFuncType);

                tIlGen.EmitCallInvokeFunc(invokeSetFuncType);

                tIlGen.Emit(OpCodes.Pop);
                tIlGen.Emit(OpCodes.Ret);
            }

            var tRemoveCallSiteField = theCallSite.GetFieldEvenIfGeneric(invokeRemove);
            using (tIlGen.EmitBranchTrue(gen => gen.Emit(OpCodes.Ldsfld, tRemoveCallSiteField)))
            {
                tIlGen.EmitDynamicMethodInvokeBinder(
                    CSharpBinderFlags.InvokeSpecialName | CSharpBinderFlags.ResultDiscarded,
                    removeMethod.Name,
                    contextType,
                    removeMethod.GetParameters(),
                    Enumerable.Repeat(default(string),
                                      removeParameterTypes.Length));
                tIlGen.EmitCallsiteCreate(invokeRemoveFuncType);
                tIlGen.Emit(OpCodes.Stsfld, tRemoveCallSiteField);
            }
            tIlGen.Emit(OpCodes.Ldsfld, tRemoveCallSiteField);
            tIlGen.Emit(OpCodes.Ldfld, tRemoveCallSiteField.FieldType.GetFieldEvenIfGeneric("Target"));
            tIlGen.Emit(OpCodes.Ldsfld, tRemoveCallSiteField);
            tIlGen.Emit(OpCodes.Ldarg_0);
            tIlGen.Emit(OpCodes.Call, typeof (AbstractTypeProjectionProxy).GetProperty("OriginalTarget").GetGetMethod());
            tIlGen.Emit(OpCodes.Ldarg_1);
            tIlGen.EmitCallInvokeFunc(invokeRemoveFuncType);
            tIlGen.Emit(OpCodes.Pop);
            tIlGen.Emit(OpCodes.Ret);

            eventBuilder.SetRemoveOnMethod(tRemoveBuilder);
        }

        private static void MakeMethod(ModuleBuilder builder, MethodInfo info, TypeBuilder typeBuilder, Type contextType,
                                       bool defaultImp = true)
        {
            var methodName = info.Name;

            var allParameters = info.GetParameters();
            Type[] parameterTypes = allParameters.Select(it => it.ParameterType).ToArray();


            IEnumerable<string> argumentNames;
            if (info.GetCustomAttributes(typeof (UseNamedArgumentAttribute), false).Any())
            {
                argumentNames = allParameters.Select(it => it.Name).ToList();
            }
            else
            {
                var theParam = allParameters.Zip(Enumerable.Range(0, parameterTypes.Count()), (p, i) => new {i, p})
                    .FirstOrDefault(it => it.p.GetCustomAttributes(typeof (UseNamedArgumentAttribute), false).Any());

                argumentNames = theParam == null
                                    ? Enumerable.Repeat(default(string), parameterTypes.Length)
                                    : Enumerable.Repeat(default(string), theParam.i).Concat(
                                        allParameters.Skip(Math.Min(theParam.i - 1, 0)).Select(it => it.Name)).ToList();
            }


            var returnType = typeof (void);
            if (info.ReturnParameter != null)
                returnType = info.ReturnParameter.ParameterType;


            var callSiteInvotationName = string.Format("TAC_Callsite_{1}_{0}", Guid.NewGuid().ToString("N"), methodName);
            var callSiteBuilder = DefineBuilderForCallSite(builder, callSiteInvotationName);


            var replacedTypes = GetParamTypes(callSiteBuilder, info);
            if (replacedTypes != null)
            {
                returnType = replacedTypes.Item1;
                parameterTypes = replacedTypes.Item2;
            }

            var convertMethod = "Convert_Method";
            Type convertFuncType = null;
            if (returnType != typeof (void))
            {
                convertFuncType = callSiteBuilder.DefineCallsiteField(convertMethod, returnType);
            }

            var invokeMethod = "Invoke_Method";
            var invokeFuncType = callSiteBuilder.DefineCallsiteFieldForMethod(invokeMethod,
                                                                              returnType != typeof (void)
                                                                                  ? typeof (object)
                                                                                  : typeof (void), parameterTypes, info);


            var callSite = callSiteBuilder.CreateType();

            var methodAccess = MethodAttributes.Public;
            var prefixName = methodName;
            if (!defaultImp)
            {
                prefixName = String.Format("{0}.{1}", info.DeclaringType.FullName, prefixName);

                methodAccess = MethodAttributes.Private;
            }


            var methodBuilder = typeBuilder.DefineMethod(prefixName,
                                                         methodAccess | MethodAttributes.HideBySig |
                                                         MethodAttributes.Virtual | MethodAttributes.Final |
                                                         MethodAttributes.NewSlot);


            replacedTypes = GetParamTypes(methodBuilder, info);
            var reducedParameters = parameterTypes.Select(ReduceToElementType).ToArray();
            if (replacedTypes != null)
            {
                returnType = replacedTypes.Item1;
                parameterTypes = replacedTypes.Item2;

                reducedParameters = parameterTypes.Select(ReduceToElementType).ToArray();

                callSite = callSite.GetGenericTypeDefinition().MakeGenericType(reducedParameters);
                if (convertFuncType != null)
                    convertFuncType = UpdateCallsiteFuncType(convertFuncType, returnType);
                invokeFuncType = UpdateCallsiteFuncType(invokeFuncType,
                                                        returnType != typeof (void) ? typeof (object) : typeof (void),
                                                        reducedParameters);
            }

            methodBuilder.SetReturnType(returnType);
            methodBuilder.SetParameters(parameterTypes);

            foreach (var tParam in info.GetParameters())
            {
                var parameterBuilder = methodBuilder.DefineParameter(tParam.Position + 1, AttributesForParam(tParam),
                                                                     tParam.Name);


                var customAttributes = tParam.GetCustomAttributesData();
                foreach (var eachAtt in customAttributes)
                {
                    try
                    {
                        parameterBuilder.SetCustomAttribute(GetAttributeBuilder(eachAtt));
                    }
                    catch
                    {
                    }
                }
            }

            if (!defaultImp)
            {
                typeBuilder.DefineMethodOverride(methodBuilder, info);
            }

            EmitMethodBody(methodName, reducedParameters, allParameters, returnType, convertMethod, invokeMethod,
                           methodBuilder, callSite, contextType, convertFuncType, invokeFuncType, argumentNames);
        }

        private static void MakeProperty(ModuleBuilder builder, PropertyInfo info, TypeBuilder typeBuilder,
                                         Type contextType, bool defaultImp = true)
        {
            var name = info.Name;

            var getMethod = info.GetGetMethod();
            var setMethod = info.GetSetMethod();
            var returnType = getMethod.ReturnType;
            var getName = getMethod.Name;


            MakePropertyHelper(info, name, builder, returnType, setMethod, typeBuilder, getName, contextType, defaultImp);
        }

        private static void MakePropertyDescribedProperty(ModuleBuilder builder, TypeBuilder typeBuilder,
                                                          Type contextType, string tName, Type tReturnType)
        {
            var getName = "get_" + tName;


            MakePropertyHelper(null, tName, builder, tReturnType, null, typeBuilder, getName, contextType, true);
        }

        private static void MakePropertyHelper(PropertyInfo info, string tName, ModuleBuilder builder, Type tReturnType,
                                               MethodInfo tSetMethod, TypeBuilder typeBuilder, string tGetName,
                                               Type contextType, bool defaultImp)
        {
            var tIndexParamTypes = new Type[] {};
            if (info != null)
                tIndexParamTypes = info.GetIndexParameters().Select(it => it.ParameterType).ToArray();
            Type[] tSetParamTypes = null;
            Type tInvokeSetFuncType = null;

            var tCallSiteInvokeName = string.Format("TAC_Callsite_{1}_{0}", Guid.NewGuid().ToString("N"), tName);
            var tCStp = DefineBuilderForCallSite(builder, tCallSiteInvokeName);


            var tConvertGet = "Convert_Get";

            var tConvertFuncType = tCStp.DefineCallsiteField(tConvertGet, tReturnType);

            var tInvokeGet = "Invoke_Get";
            var tInvokeGetFuncType = tCStp.DefineCallsiteField(tInvokeGet, typeof (object), tIndexParamTypes);

            var tInvokeSet = "Invoke_Set";
            if (tSetMethod != null)
            {
                tSetParamTypes = tSetMethod.GetParameters().Select(it => it.ParameterType).ToArray();

                tInvokeSetFuncType = tCStp.DefineCallsiteField(tInvokeSet, typeof (object), tSetParamTypes);
            }

            var tCallSite = tCStp.CreateType();


            var tPublicPrivate = MethodAttributes.Public;
            var tPrefixedGet = tGetName;
            var tPrefixedName = tName;
            if (!defaultImp)
            {
                tPublicPrivate = MethodAttributes.Private;
                tPrefixedGet = String.Format("{0}.{1}", info.DeclaringType.FullName, tPrefixedGet);

                tPrefixedName = String.Format("{0}.{1}", info.DeclaringType.FullName, tPrefixedName);
            }


            var tMp = typeBuilder.DefineProperty(tPrefixedName, PropertyAttributes.None,
                                                 CallingConventions.HasThis,
                                                 tReturnType, tIndexParamTypes);


            //GetMethod
            var tGetMethodBuilder = typeBuilder.DefineMethod(tPrefixedGet,
                                                             tPublicPrivate
                                                             | MethodAttributes.SpecialName
                                                             | MethodAttributes.HideBySig
                                                             | MethodAttributes.Virtual
                                                             | MethodAttributes.Final
                                                             | MethodAttributes.NewSlot,
                                                             tReturnType,
                                                             tIndexParamTypes);


            if (!defaultImp)
            {
                typeBuilder.DefineMethodOverride(tGetMethodBuilder, info.GetGetMethod());
            }


            if (info != null)
            {
                foreach (var tParam in info.GetGetMethod().GetParameters())
                {
                    tGetMethodBuilder.DefineParameter(tParam.Position + 1, AttributesForParam(tParam), tParam.Name);
                }
            }

            EmitProperty(
                info,
                tName,
                tConvertGet,
                tReturnType,
                tInvokeGet,
                tIndexParamTypes,
                tSetMethod,
                tInvokeSet,
                tSetParamTypes,
                typeBuilder,
                tGetMethodBuilder,
                tCallSite,
                contextType,
                tConvertFuncType,
                tInvokeGetFuncType,
                tMp,
                tInvokeSetFuncType, defaultImp);
        }

        public static bool PreLoadProxiesFromAssembly(Assembly assembly)
        {
            var sucessfullyLoaded = true;
            var typesWithMyAttribute =
                from theType in assembly.GetTypes()
                let tAttributes = theType.GetCustomAttributes(typeof (DressedAsAttribute), inherit: false)
                where tAttributes != null && tAttributes.Length == 1
                select new {Type = theType, DressedAtt = tAttributes.Cast<DressedAsAttribute>().Single()};
            foreach (var tTypeCombo in typesWithMyAttribute)
            {
                lock (_typeCacheLock)
                {
                    if (!PreLoadProxy(tTypeCombo.Type, tTypeCombo.DressedAtt))
                        sucessfullyLoaded = false;
                }
            }
            return sucessfullyLoaded;
        }

        public static bool PreLoadProxy(Type proxyType, DressedAsAttribute attribute = null)
        {
            var sucessfullyLoaded = true;
            if (attribute == null)
                attribute =
                    proxyType.GetCustomAttributes(typeof (DressedAsAttribute), inherit: false).Cast<DressedAsAttribute>()
                        .FirstOrDefault();

            if (attribute == null)
                throw new Exception("Proxy Type must have DressedAsAttribute");

            if (!typeof (IDressAsProxyInitializer).IsAssignableFrom(proxyType))
                throw new Exception("Proxy Type must implement IDressAsProxyInitializer");

            foreach (var interfaceType in attribute.Interfaces)
            {
                if (!interfaceType.IsAssignableFrom(proxyType))
                {
                    throw new Exception(String.Format("Proxy Type {0} must implement declared interfaces {1}", proxyType,
                                                      interfaceType));
                }
            }

            lock (_typeCacheLock)
            {
                var newHash = TypeHasher.Create(attribute.Context, attribute.Interfaces);

                if (!_typeHash.ContainsKey(newHash))
                {
                    _typeHash[newHash] = proxyType;
                }
                else
                {
                    sucessfullyLoaded = false;
                }
            }
            return sucessfullyLoaded;
        }

        private static Type ReduceToElementType(Type type)
        {
            if (type.IsByRef || type.IsPointer || type.IsArray)
                return type.GetElementType();
            return type;
        }

        private static Type ReplaceTypeWithGenericBuilder(Type type, IDictionary<Type, GenericTypeParameterBuilder> dict)
        {
            var startType = type;
            Type returnType;
            if (type.IsByRef || type.IsArray || type.IsPointer)
            {
                startType = type.GetElementType();
            }


            if (startType.IsGenericTypeDefinition)
            {
                var tArgs = startType.GetGenericArguments().Select(it => ReplaceTypeWithGenericBuilder(it, dict));

                var tNewType = startType.MakeGenericType(tArgs.ToArray());
                returnType = tNewType;
            }
            else if (dict.ContainsKey(startType))
            {
                var newType = dict[startType];
                var theAttributes = startType.GenericParameterAttributes;
                newType.SetGenericParameterAttributes(theAttributes);
                foreach (var eachConstraint in startType.GetGenericParameterConstraints())
                {
                    if (eachConstraint.IsInterface)
                        newType.SetInterfaceConstraints(eachConstraint);
                    else
                        newType.SetBaseTypeConstraint(eachConstraint);
                }
                returnType = newType;
            }
            else
            {
                returnType = startType;
            }

            if (type.IsByRef)
            {
                return returnType.MakeByRefType();
            }

            if (type.IsArray)
            {
                return returnType.MakeArrayType();
            }

            if (type.IsPointer)
            {
                return returnType.MakePointerType();
            }

            return returnType;
        }

        public static IDisposable SaveAssemblyToFile(string name)
        {
            GenerateAssembly(name, AssemblyBuilderAccess.RunAndSave, ref _tempSaveAssembly, ref _tempBuilder);

            return new TempBuilder(name);
        }

        private static Type UpdateCallsiteFuncType(Type tFuncGeneric, Type returnType, params Type[] argTypes)
        {
            var tList = new List<Type> {typeof (CallSite), typeof (object)};
            tList.AddRange(argTypes);
            if (returnType != typeof (void))
                tList.Add(returnType);

            IEnumerable<Type> tTypeArguments = tList;


            var tDef = tFuncGeneric.GetGenericTypeDefinition();

            if (tDef.GetGenericArguments().Count() != tTypeArguments.Count())
            {
                tTypeArguments = tTypeArguments.Where(it => it.IsGenericParameter);
            }

            var tFuncType = tDef.MakeGenericType(tTypeArguments.ToArray());

            return tFuncType;
        }

        #region Nested type: MethodSignatureHash

        private class MethodSignatureHash
        {
            public readonly string Name;
            public readonly Type[] Parameters;

            public MethodSignatureHash(MethodInfo info)
            {
                Name = info.Name;
                Parameters = info.GetParameters().Select(it => it.ParameterType).ToArray();
            }

            public MethodSignatureHash(string name, Type[] parameters)
            {
                Name = name;
                Parameters = parameters;
            }


            public bool Equals(MethodSignatureHash other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(other.Name, Name) &&
                       StructuralComparisons.StructuralEqualityComparer.Equals(other.Parameters, Parameters);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof (MethodSignatureHash)) return false;
                return Equals((MethodSignatureHash) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Name.GetHashCode()*397) ^
                           StructuralComparisons.StructuralEqualityComparer.GetHashCode(Parameters);
                }
            }
        }

        #endregion

        #region Nested type: TempBuilder

        internal class TempBuilder : IDisposable
        {
            private readonly string _name;
            private bool _disposed;


            internal TempBuilder(string name)
            {
                _name = name;
            }

            #region IDisposable Members

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;


                _tempSaveAssembly.Save(string.Format("{0}.dll", _name));

                _tempSaveAssembly = null;
                _tempBuilder = null;
            }

            #endregion

            public void Close()
            {
                Dispose();
            }
        }

        #endregion
    }

    #endregion Classes
}