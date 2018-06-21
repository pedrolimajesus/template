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
using System.Reflection;
using System.Reflection.Emit;

namespace AppComponents.TypeProxy
{
    internal class MethodMappings
    {
        private bool m_OverridesInterfaceMethods;
        private MethodInfo m_Target;
        private ArrayList m_TargetOverrides;

        public MethodMappings(MethodInfo Target, bool OverridesInterfaceMethods)
        {
            m_Target = Target;
            m_OverridesInterfaceMethods = OverridesInterfaceMethods;
            m_TargetOverrides = new ArrayList();
        }

        public bool OverridesInterfaceMethods
        {
            get { return m_OverridesInterfaceMethods; }
        }

        public ArrayList MappedMethods
        {
            get { return m_TargetOverrides; }
        }
    }

    public struct MethodContextInfo
    {
        private AssemblyName m_AsmName;
        private String m_MethodName;
        private String m_TypeName;

        public MethodContextInfo(AssemblyName AssemblyInformation, String TypeName, String MethodName)
        {
            m_AsmName = (AssemblyName) AssemblyInformation.Clone();
            m_MethodName = MethodName;
            m_TypeName = TypeName;
        }

        public AssemblyName AssemblyInformation
        {
            get { return (AssemblyName) m_AsmName.Clone(); }
        }

        public String TypeName
        {
            get { return m_TypeName; }
        }

        public String MethodName
        {
            get { return m_MethodName; }
        }
    }

    public interface InvocationHandler
    {
        void BeforeMethodInvocation(MethodContextInfo CurrentMethod,
                                    ref Object[] MethodArgs, ref bool CallMethod,
                                    ref bool BubbleException);

        void AfterMethodInvocation(MethodContextInfo CurrentMethod,
                                   ref Object[] MethodArgs,
                                   ref Object ReturnValue,
                                   ref bool BubbleException);

        void AfterMethodInvocationWithException(MethodContextInfo CurrentMethod,
                                                Exception GeneratedException,
                                                ref bool BubbleGeneratedException,
                                                ref bool BubbleException);
    }

    public sealed class Proxy
    {
        #region Constants

        private const String AFTER_INVOKE = "AfterMethodInvocation";
        private const String AFTER_INVOKE_EX = "AfterMethodInvocationWithException";
        private const String BASE_OBJECT_ARG_NAME = "BaseObject";
        private const String BEFORE_INVOKE = "BeforeMethodInvocation";
        private const String EQUALS = "Equals";
        private const String ERROR_BASE_OBJECT_ABSTRACT = "The base object cannot be abstract.";
        private const String ERROR_BASE_OBJECT_NULL = "The base object cannot be null.";
        private const String ERROR_BASE_OBJECT_NO_METHODS = "There are no available methods to hook.";
        private const String ERROR_BASE_OBJECT_SEALED = "The base object cannot be sealed.";
        private const String ERROR_HANDLERS_NONE = "There are no hanlders given.";
        private const String ERROR_HANDLERS_NULL = "The handler array cannot be null.";
        private const String GET_ASSEMBLY_PROP = "get_Assembly";
        private const String GET_FULL_NAME_PROP = "get_FullName";
        private const String GET_METHOD = "GetMethod";
        private const String GET_METHOD_FROM_HANDLE = "GetMethodFromHandle";
        private const String GET_NAME = "GetName";
        private const String GET_NAME_PROP = "get_Name";
        private const String GET_TYPE = "GetType";
        private const String GET_TYPE_FROM_HANDLE = "GetTypeFromHandle";
        private const String HANDLERS_ARG_NAME = "Handlers";
        private const String INVOKE = "Invoke";
        private const String INVOKE_METHOD = "InvokeMethod";
        private const String INVOKE_HANDLER_FIELD = "invokeHandlers";
        private const String WRAPPED_OBJECT_FIELD = "wrappedObject";

        #endregion

        #region Static and Instance Members

#if DEBUG
        private static Hashtable createdAssemblies = new Hashtable();
#endif
        private static Hashtable createdModules = new Hashtable();
        private static Hashtable createdTypes = new Hashtable();

        #endregion

        #region Static and Instance Initializers

        private Proxy()
        {
        }

        #endregion

        #region Private Static Methods

        private static MethodInfo BuildInvokeMethod(TypeBuilder BaseProxy, FieldBuilder InvokeHandlers,
                                                    FieldBuilder WrappedObject)
        {
            #region CIL Setup

            Type[] invokeArgTypes = new Type[3];
            invokeArgTypes[0] = typeof (MethodInfo);
            invokeArgTypes[1] = Type.GetType("System.Object[]&");
            invokeArgTypes[2] = typeof (MethodContextInfo);

            MethodBuilder invokeMethod = BaseProxy.DefineMethod(INVOKE_METHOD,
                                                                MethodAttributes.Private | MethodAttributes.HideBySig,
                                                                CallingConventions.HasThis,
                                                                typeof (Object), invokeArgTypes);

            invokeMethod.DefineParameter(2,
                                         (ParameterAttributes.In | ParameterAttributes.Out),
                                         "methodArgs");

            ILGenerator invokeMethodIL = invokeMethod.GetILGenerator();

            LocalBuilder raiseException = invokeMethodIL.DeclareLocal(typeof (Boolean));
            LocalBuilder doInvoke = invokeMethodIL.DeclareLocal(typeof (Boolean));
            LocalBuilder bubbleEx = invokeMethodIL.DeclareLocal(typeof (Boolean));
            LocalBuilder bubbleGenEx = invokeMethodIL.DeclareLocal(typeof (Boolean));
            LocalBuilder success = invokeMethodIL.DeclareLocal(typeof (Boolean));
            LocalBuilder retVal = invokeMethodIL.DeclareLocal(typeof (Object));
            LocalBuilder invokeEx = invokeMethodIL.DeclareLocal(typeof (Exception));
            LocalBuilder i = invokeMethodIL.DeclareLocal(typeof (Int32));

            Type[] argTypes = null;

            #endregion

            #region CIL Locals Setup

            //  Initialize all of the local variables.
            //  raiseException = false
            invokeMethodIL.Emit(OpCodes.Ldc_I4_0);
            invokeMethodIL.Emit(OpCodes.Stloc, raiseException);
            //  doInvoke = true
            invokeMethodIL.Emit(OpCodes.Ldc_I4_1);
            invokeMethodIL.Emit(OpCodes.Stloc, doInvoke);
            //  bubbleEx = false
            invokeMethodIL.Emit(OpCodes.Ldc_I4_0);
            invokeMethodIL.Emit(OpCodes.Stloc, bubbleEx);
            //  bubbleGenEx = false
            invokeMethodIL.Emit(OpCodes.Ldc_I4_0);
            invokeMethodIL.Emit(OpCodes.Stloc, bubbleGenEx);
            //  success = false
            invokeMethodIL.Emit(OpCodes.Ldc_I4_0);
            invokeMethodIL.Emit(OpCodes.Stloc, success);
            //  retVal = null
            invokeMethodIL.Emit(OpCodes.Ldnull);
            invokeMethodIL.Emit(OpCodes.Stloc, retVal);
            //  invokeEx = null
            invokeMethodIL.Emit(OpCodes.Ldnull);
            invokeMethodIL.Emit(OpCodes.Stloc, invokeEx);
            //  i = 0
            invokeMethodIL.Emit(OpCodes.Ldc_I4_0);
            invokeMethodIL.Emit(OpCodes.Stloc, i);

            #endregion

            #region BeforeInvocationHandler Iteration

            //  Call BeforeInvocationHandler on each IL ref
            //  in invokeHandlers.
            Label doNextHandlerBefore = invokeMethodIL.DefineLabel();
            Label finishBefore = invokeMethodIL.DefineLabel();
            Label incrementIBefore = invokeMethodIL.DefineLabel();
            Label preventInvokeBefore = invokeMethodIL.DefineLabel();
            Label ignoreBeforeEx = invokeMethodIL.DefineLabel();

            invokeMethodIL.BeginExceptionBlock();
            invokeMethodIL.MarkLabel(doNextHandlerBefore);
            //  Reset the by-ref booleans.
            invokeMethodIL.Emit(OpCodes.Ldc_I4_1);
            invokeMethodIL.Emit(OpCodes.Stloc, doInvoke);
            invokeMethodIL.Emit(OpCodes.Ldc_I4_0);
            invokeMethodIL.Emit(OpCodes.Stloc, raiseException);
            //  Load the next handler.
            invokeMethodIL.Emit(OpCodes.Ldarg_0);
            invokeMethodIL.Emit(OpCodes.Ldfld, InvokeHandlers);
            invokeMethodIL.Emit(OpCodes.Ldloc, i);
            invokeMethodIL.Emit(OpCodes.Ldelem_Ref);
            //  Load the handler's arguments.
            invokeMethodIL.Emit(OpCodes.Ldarg_3);
            invokeMethodIL.Emit(OpCodes.Ldarg_2);
            invokeMethodIL.Emit(OpCodes.Ldloca, doInvoke);
            invokeMethodIL.Emit(OpCodes.Ldloca, raiseException);
            //  BeforeInvocationHandler.
            invokeMethodIL.Emit(OpCodes.Callvirt,
                                typeof (InvocationHandler).GetMethod(BEFORE_INVOKE));
            //  If doInvoke == false, stop loop.
            //  Note that the "br finishBefore" will keep
            //  doInvoke == false.
            invokeMethodIL.Emit(OpCodes.Ldloc, doInvoke);
            invokeMethodIL.Emit(OpCodes.Brtrue, incrementIBefore);
            invokeMethodIL.Emit(OpCodes.Br, finishBefore);
            //  Increment i.
            invokeMethodIL.MarkLabel(incrementIBefore);
            invokeMethodIL.Emit(OpCodes.Ldloc, i);
            invokeMethodIL.Emit(OpCodes.Ldc_I4_1);
            invokeMethodIL.Emit(OpCodes.Add);
            invokeMethodIL.Emit(OpCodes.Stloc, i);
            //  See if i < invokeHandlers.Length.
            invokeMethodIL.Emit(OpCodes.Ldloc, i);
            invokeMethodIL.Emit(OpCodes.Ldarg_0);
            invokeMethodIL.Emit(OpCodes.Ldfld, InvokeHandlers);
            invokeMethodIL.Emit(OpCodes.Ldlen);
            invokeMethodIL.Emit(OpCodes.Conv_I4);
            invokeMethodIL.Emit(OpCodes.Blt, doNextHandlerBefore);
            //  Exit loop.
            invokeMethodIL.MarkLabel(finishBefore);
            invokeMethodIL.BeginCatchBlock(typeof (Exception));
            //  If raiseException == true, rethrow exception.
            invokeMethodIL.Emit(OpCodes.Ldloc, raiseException);
            invokeMethodIL.Emit(OpCodes.Brfalse, ignoreBeforeEx);
            //  Note that the exception is on the stack here...
            invokeMethodIL.Emit(OpCodes.Throw);
            invokeMethodIL.MarkLabel(ignoreBeforeEx);
            invokeMethodIL.EndExceptionBlock();

            #endregion

            #region Target Method Invocation

            //  OK, all the Before...() methods were called,
            //  and we got here.  If doInvoke == true,
            //  call the method
            Label performInvoke = invokeMethodIL.DefineLabel();
            Label doNotPerformInvoke = invokeMethodIL.DefineLabel();
            invokeMethodIL.Emit(OpCodes.Ldloc, doInvoke);
            invokeMethodIL.Emit(OpCodes.Brtrue, performInvoke);
            //  Set success to false, since the method will not be invoked.
            invokeMethodIL.Emit(OpCodes.Ldc_I4_0);
            invokeMethodIL.Emit(OpCodes.Stloc, success);
            invokeMethodIL.Emit(OpCodes.Br, doNotPerformInvoke);
            invokeMethodIL.MarkLabel(performInvoke);
            invokeMethodIL.BeginExceptionBlock();
            //  Load the target method and its' associated object.
            invokeMethodIL.Emit(OpCodes.Ldarg_1);
            invokeMethodIL.Emit(OpCodes.Ldarg_0);
            invokeMethodIL.Emit(OpCodes.Ldfld, WrappedObject);
            invokeMethodIL.Emit(OpCodes.Ldarg_2);
            invokeMethodIL.Emit(OpCodes.Ldind_Ref);
            //  Invoke...
            argTypes = new Type[2];
            argTypes[0] = typeof (Object);
            argTypes[1] = typeof (Object[]);
            invokeMethodIL.Emit(OpCodes.Callvirt,
                                typeof (MethodInfo).GetMethod(INVOKE, argTypes));
            invokeMethodIL.Emit(OpCodes.Stloc, retVal);
            //  Set success to true.
            invokeMethodIL.Emit(OpCodes.Ldc_I4_1);
            invokeMethodIL.Emit(OpCodes.Stloc, success);

            #region AfterMethodInvocationWithException Iteration

            invokeMethodIL.BeginCatchBlock(typeof (Exception));
            invokeMethodIL.Emit(OpCodes.Stloc, invokeEx);
            //  The invoked method caused an exception.
            //  We need to iterate through each After...Ex() method
            //  and throw the appropriate exception.
            invokeMethodIL.BeginExceptionBlock();
            Label doNextHandlerAfterEx = invokeMethodIL.DefineLabel();
            Label incrementIAfterEx = invokeMethodIL.DefineLabel();
            //  Reset i = 0;
            invokeMethodIL.Emit(OpCodes.Ldc_I4_0);
            invokeMethodIL.Emit(OpCodes.Stloc, i);
            invokeMethodIL.MarkLabel(doNextHandlerAfterEx);
            //  Reset the by-ref booleans.
            invokeMethodIL.Emit(OpCodes.Ldc_I4_1);
            invokeMethodIL.Emit(OpCodes.Stloc, bubbleEx);
            invokeMethodIL.Emit(OpCodes.Ldc_I4_0);
            invokeMethodIL.Emit(OpCodes.Stloc, bubbleGenEx);
            //  Load the next handler.
            invokeMethodIL.Emit(OpCodes.Ldarg_0);
            invokeMethodIL.Emit(OpCodes.Ldfld, InvokeHandlers);
            invokeMethodIL.Emit(OpCodes.Ldloc, i);
            invokeMethodIL.Emit(OpCodes.Ldelem_Ref);
            //  Load the handler's arguments.
            invokeMethodIL.Emit(OpCodes.Ldarg_3);
            invokeMethodIL.Emit(OpCodes.Ldloc, invokeEx);
            invokeMethodIL.Emit(OpCodes.Ldloca, bubbleGenEx);
            invokeMethodIL.Emit(OpCodes.Ldloca, bubbleEx);
            //  AfterInvocationHandlerWithException.
            invokeMethodIL.Emit(OpCodes.Callvirt,
                                typeof (InvocationHandler).GetMethod(AFTER_INVOKE_EX));
            //  If bubbleEx == true or bubbleGenEx == true, rethrow invokeEx.
            //  Note that this will put us into the catch block - 
            //  we have to rehandle it there.
            Label rethrowEx = invokeMethodIL.DefineLabel();
            invokeMethodIL.Emit(OpCodes.Ldloc, bubbleGenEx);
            invokeMethodIL.Emit(OpCodes.Brtrue, rethrowEx);
            invokeMethodIL.Emit(OpCodes.Ldloc, bubbleEx);
            invokeMethodIL.Emit(OpCodes.Brfalse, incrementIAfterEx);
            invokeMethodIL.MarkLabel(rethrowEx);
            invokeMethodIL.Emit(OpCodes.Rethrow);
            //  Increment i.
            invokeMethodIL.MarkLabel(incrementIAfterEx);
            invokeMethodIL.Emit(OpCodes.Ldloc, i);
            invokeMethodIL.Emit(OpCodes.Ldc_I4_1);
            invokeMethodIL.Emit(OpCodes.Add);
            invokeMethodIL.Emit(OpCodes.Stloc, i);
            //  See if i < invokeHandlers.Length.
            invokeMethodIL.Emit(OpCodes.Ldloc, i);
            invokeMethodIL.Emit(OpCodes.Ldarg_0);
            invokeMethodIL.Emit(OpCodes.Ldfld, InvokeHandlers);
            invokeMethodIL.Emit(OpCodes.Ldlen);
            invokeMethodIL.Emit(OpCodes.Conv_I4);
            invokeMethodIL.Emit(OpCodes.Blt, doNextHandlerAfterEx);
            //  Exit loop.
            invokeMethodIL.BeginCatchBlock(typeof (Exception));
            //  If bubbleEx == true or bubbleGenEx == true, rethrow exception.
            Label throwIt = invokeMethodIL.DefineLabel();
            Label doNotThrowIt = invokeMethodIL.DefineLabel();
            invokeMethodIL.Emit(OpCodes.Ldloc, bubbleGenEx);
            invokeMethodIL.Emit(OpCodes.Brtrue, throwIt);
            invokeMethodIL.Emit(OpCodes.Ldloc, bubbleEx);
            invokeMethodIL.Emit(OpCodes.Brfalse, doNotThrowIt);
            //  Note that the exception is on the stack here...
            invokeMethodIL.MarkLabel(throwIt);
            invokeMethodIL.Emit(OpCodes.Rethrow);
            invokeMethodIL.MarkLabel(doNotThrowIt);
            invokeMethodIL.EndExceptionBlock();
            invokeMethodIL.EndExceptionBlock();
            invokeMethodIL.MarkLabel(doNotPerformInvoke);

            #endregion

            #endregion

            #region AfterInvocationHandler Iteration

            //  If success == true, then we iterate through
            //  all of the After...() methods, throwing
            //  exceptions appropriately.
            invokeMethodIL.Emit(OpCodes.Ldloc, success);
            Label doNotCallAfters = invokeMethodIL.DefineLabel();
            invokeMethodIL.Emit(OpCodes.Brfalse, doNotCallAfters);
            //  Now call AfterInvocationHandler on each IL ref
            //  in invokeHandlers.
            Label doNextHandlerAfter = invokeMethodIL.DefineLabel();
            invokeMethodIL.BeginExceptionBlock();
            //  Reset i = 0.
            invokeMethodIL.Emit(OpCodes.Ldc_I4_0);
            invokeMethodIL.Emit(OpCodes.Stloc, i);
            invokeMethodIL.MarkLabel(doNextHandlerAfter);
            //  Reset the by-ref boolean.
            invokeMethodIL.Emit(OpCodes.Ldc_I4_0);
            invokeMethodIL.Emit(OpCodes.Stloc, bubbleEx);
            //  Load the next handler.
            invokeMethodIL.Emit(OpCodes.Ldarg_0);
            invokeMethodIL.Emit(OpCodes.Ldfld, InvokeHandlers);
            invokeMethodIL.Emit(OpCodes.Ldloc, i);
            invokeMethodIL.Emit(OpCodes.Ldelem_Ref);
            //  Load the handler's arguments.
            invokeMethodIL.Emit(OpCodes.Ldarg_3);
            invokeMethodIL.Emit(OpCodes.Ldarg_2);
            invokeMethodIL.Emit(OpCodes.Ldloca, retVal);
            invokeMethodIL.Emit(OpCodes.Ldloca, bubbleEx);
            //  BeforeInvocationHandler.
            invokeMethodIL.Emit(OpCodes.Callvirt,
                                typeof (InvocationHandler).GetMethod(AFTER_INVOKE));
            //  Increment i.
            invokeMethodIL.Emit(OpCodes.Ldloc, i);
            invokeMethodIL.Emit(OpCodes.Ldc_I4_1);
            invokeMethodIL.Emit(OpCodes.Add);
            invokeMethodIL.Emit(OpCodes.Stloc, i);
            //  See if i < invokeHandlers.Length.
            invokeMethodIL.Emit(OpCodes.Ldloc, i);
            invokeMethodIL.Emit(OpCodes.Ldarg_0);
            invokeMethodIL.Emit(OpCodes.Ldfld, InvokeHandlers);
            invokeMethodIL.Emit(OpCodes.Ldlen);
            invokeMethodIL.Emit(OpCodes.Conv_I4);
            invokeMethodIL.Emit(OpCodes.Blt, doNextHandlerAfter);
            //  Exit loop.
            invokeMethodIL.BeginCatchBlock(typeof (Exception));
            //  If bubbleEx == true, rethrow exception.
            invokeMethodIL.Emit(OpCodes.Ldloc, bubbleEx);
            Label ignoreExAfter = invokeMethodIL.DefineLabel();
            invokeMethodIL.Emit(OpCodes.Brfalse, ignoreExAfter);
            invokeMethodIL.Emit(OpCodes.Rethrow);
            invokeMethodIL.MarkLabel(ignoreExAfter);
            invokeMethodIL.EndExceptionBlock();
            invokeMethodIL.MarkLabel(doNotCallAfters);

            #endregion

            #region CIL Return

            //  Finally, return the value.
            //  Note that we have to do this here, even if the
            //  target method doesn't have a return value.
            //  The caller in the proxy will decide what to do with it.
            invokeMethodIL.Emit(OpCodes.Ldloc, retVal);
            invokeMethodIL.Emit(OpCodes.Ret);
            return invokeMethod;

            #endregion
        }

        private static Hashtable GetTargetMethods(Object BaseObject)
        {
            Type baseType = BaseObject.GetType();
            Hashtable retVal = new Hashtable();

            foreach (Type itf in baseType.GetInterfaces())
            {
                InterfaceMapping imap = baseType.GetInterfaceMap(itf);

                for (int i = 0; i < imap.InterfaceMethods.Length; i++)
                {
                    MethodInfo trueTarget;

                    if (imap.TargetMethods[i].IsPublic)
                    {
                        //  We can invoke the true target
                        //  so the mapping will be directly to the target method.
                        //  Note that I don't care if it's final or not.
                        trueTarget = imap.TargetMethods[i];
                    }
                    else
                    {
                        trueTarget = imap.InterfaceMethods[i];
                    }

                    MethodMappings itfMM = (MethodMappings) retVal[trueTarget];

                    if (null == itfMM)
                    {
                        itfMM = new MethodMappings(trueTarget, true);
                        retVal.Add(trueTarget, itfMM);
                    }

                    itfMM.MappedMethods.Add(imap.InterfaceMethods[i]);
                }
            }

            foreach (MethodInfo mi in baseType.GetMethods())
            {
                if (mi.IsPublic && mi.IsVirtual && !mi.IsFinal)
                {
                    //  Let's see if we already have it
                    //  from the interface mapping.
                    MethodMappings baseMM = (MethodMappings) retVal[mi];

                    if (null == baseMM)
                    {
                        //  This method doesn't override
                        //  any itf. methods, so add it.
                        baseMM = new MethodMappings(mi, false);
                        retVal.Add(mi, baseMM);
                        baseMM.MappedMethods.Add(mi);
                    }
                }
            }

            return retVal;
        }

        private static void BuildParameterlessConstructor(TypeBuilder BaseProxy, Type BaseType)
        {
            ConstructorBuilder proxyCtor =
                BaseProxy.DefineConstructor(MethodAttributes.Private | MethodAttributes.SpecialName |
                                            MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
                                            CallingConventions.Standard, Type.EmptyTypes);

            ILGenerator proxyCtorIL = proxyCtor.GetILGenerator();
            proxyCtorIL.Emit(OpCodes.Ldarg_0);
            ConstructorInfo objectCtor = BaseType.GetConstructor(Type.EmptyTypes);
            proxyCtorIL.Emit(OpCodes.Call, objectCtor);
            proxyCtorIL.Emit(OpCodes.Ret);
        }

        private static void BuildConstructor(Type BaseObjectType, TypeBuilder BaseProxy,
                                             FieldBuilder WrappedType, FieldBuilder InvokeHandlers)
        {
            Type[] proxyCtorArgs = new Type[2];
            proxyCtorArgs[0] = BaseObjectType;
            proxyCtorArgs[1] = typeof (InvocationHandler[]);
            ConstructorBuilder proxyCtor =
                BaseProxy.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                            MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
                                            CallingConventions.Standard,
                                            proxyCtorArgs);

            ILGenerator proxyCtorIL = proxyCtor.GetILGenerator();
            //  Call the base constructor.
            proxyCtorIL.Emit(OpCodes.Ldarg_0);
            ConstructorInfo objectCtor = BaseObjectType.GetConstructor(Type.EmptyTypes);
            proxyCtorIL.Emit(OpCodes.Call, objectCtor);
            //  Store the target object.
            proxyCtorIL.Emit(OpCodes.Ldarg_0);
            proxyCtorIL.Emit(OpCodes.Ldarg_1);
            proxyCtorIL.Emit(OpCodes.Stfld, WrappedType);
            //  Store the handlers.
            proxyCtorIL.Emit(OpCodes.Ldarg_0);
            proxyCtorIL.Emit(OpCodes.Ldarg_2);
            proxyCtorIL.Emit(OpCodes.Stfld, InvokeHandlers);
            proxyCtorIL.Emit(OpCodes.Ret);
        }

        private static void AddInterfaces(Type BaseObjectType, TypeBuilder BaseProxy)
        {
            //  Force the proxy to implement BaseObject's interfaces.
            foreach (Type baseInterface in BaseObjectType.GetInterfaces())
            {
                if (baseInterface.IsPublic)
                {
                    BaseProxy.AddInterfaceImplementation(baseInterface);
                }
            }
        }

        private static void BuildTargetMethods(TypeBuilder BaseProxy, MethodInfo InvokeMethod,
                                               FieldBuilder WrappedObject,
                                               Hashtable TargetMethods)
        {
            #region Variable Declarations

            Type[] argTypes = null;
            LocalBuilder argValues = null;
            MethodAttributes methodAttribs;
            LocalBuilder methodCxtInfo = null;
            MethodInfo mi = null;
            Type paramRetType = null;
            String paramRetTypeName = null;
            Type paramType = null;
            String paramTypeName = null;
            ParameterInfo pi = null;
            MethodBuilder proxyMethod = null;
            ILGenerator proxyMthIL = null;
            LocalBuilder retVal = null;
            LocalBuilder targetMethod = null;
            LocalBuilder tempRetVal = null;
            LocalBuilder wrappedType = null;

            #endregion

            /*
            methodAttribs = MethodAttributes.HideBySig | 
            MethodAttributes.NewSlot | MethodAttributes.Virtual | 
            MethodAttributes.Private;
            */

            methodAttribs = MethodAttributes.HideBySig |
                            MethodAttributes.Virtual |
                            MethodAttributes.Private;

            foreach (DictionaryEntry de in TargetMethods)
            {
                mi = (MethodInfo) de.Key;

                argTypes = new Type[mi.GetParameters().Length];

                for (int i = 0; i < mi.GetParameters().Length; i++)
                {
                    argTypes[i] = mi.GetParameters()[i].ParameterType;
                }

                proxyMethod = BaseProxy.DefineMethod(mi.Name + mi.GetHashCode(),
                                                     methodAttribs, mi.ReturnType, argTypes);

                //  Determine if this method should override
                //  the mapped method (OverridesInterfaceMethods == false)
                //  or a number of itf. methods (OverridesInterfaceMethods == true)
                MethodMappings mm = (MethodMappings) de.Value;

                if (false == mm.OverridesInterfaceMethods)
                {
                    BaseProxy.DefineMethodOverride(proxyMethod, mi);
                }
                else
                {
                    for (int itfs = 0; itfs < mm.MappedMethods.Count; itfs++)
                    {
                        MethodInfo itfMth =
                            (MethodInfo) mm.MappedMethods[itfs];
                        BaseProxy.DefineMethodOverride(proxyMethod, itfMth);
                    }
                }

                proxyMthIL = proxyMethod.GetILGenerator();

                //  These are always there.
                argValues = proxyMthIL.DeclareLocal(typeof (Object[]));
                methodCxtInfo = proxyMthIL.DeclareLocal(typeof (MethodContextInfo));
                targetMethod = proxyMthIL.DeclareLocal(typeof (MethodInfo));
                wrappedType = proxyMthIL.DeclareLocal(typeof (Type));

                //  Check for a return value.
                if (typeof (void) != mi.ReturnType)
                {
                    tempRetVal = proxyMthIL.DeclareLocal(typeof (Object));
                    retVal = proxyMthIL.DeclareLocal(mi.ReturnType);
                }

                proxyMthIL.Emit(OpCodes.Ldc_I4, mi.GetParameters().Length);
                proxyMthIL.Emit(OpCodes.Newarr, typeof (Object));

                //  Set up the arg array
                if (0 == mi.GetParameters().Length)
                {
                    //  Store in argValues - there's no values
                    //  to put into the array.
                    proxyMthIL.Emit(OpCodes.Stloc, argValues);
                }
                else
                {
                    proxyMthIL.Emit(OpCodes.Stloc, argValues);
                    for (int argLoad = 0; argLoad < mi.GetParameters().Length; argLoad++)
                    {
                        proxyMthIL.Emit(OpCodes.Ldloc, argValues);
                        proxyMthIL.Emit(OpCodes.Ldc_I4, argLoad);
                        proxyMthIL.Emit(OpCodes.Ldarg, argLoad + 1);
                        pi = mi.GetParameters()[argLoad];

                        paramTypeName = pi.ParameterType.ToString();
                        paramTypeName = paramTypeName.Replace("&", "");
                        paramType = Type.GetType(paramTypeName);

                        if (pi.ParameterType.IsByRef)
                        {
                            proxyMthIL.Emit(OpCodes.Ldobj, paramType);
                        }

                        if (paramType.IsValueType)
                        {
                            proxyMthIL.Emit(OpCodes.Box, paramType);
                        }

                        proxyMthIL.Emit(OpCodes.Stelem_Ref);
                    }
                }

                //  Get the target method.
                proxyMthIL.Emit(OpCodes.Ldtoken, mi);
                proxyMthIL.Emit(OpCodes.Call,
                                typeof (MethodBase).GetMethod(GET_METHOD_FROM_HANDLE));
                proxyMthIL.Emit(OpCodes.Castclass,
                                typeof (MethodInfo));
                proxyMthIL.Emit(OpCodes.Stloc, targetMethod);

                //  Set up the method context object.
                proxyMthIL.Emit(OpCodes.Ldloca, methodCxtInfo);
                proxyMthIL.Emit(OpCodes.Ldarg_0);
                proxyMthIL.Emit(OpCodes.Ldfld, WrappedObject);
                proxyMthIL.Emit(OpCodes.Callvirt,
                                typeof (Object).GetMethod(GET_TYPE));
                proxyMthIL.Emit(OpCodes.Stloc, wrappedType);
                proxyMthIL.Emit(OpCodes.Ldloc, wrappedType);
                proxyMthIL.Emit(OpCodes.Callvirt,
                                typeof (Type).GetMethod(GET_ASSEMBLY_PROP));
                proxyMthIL.Emit(OpCodes.Callvirt,
                                typeof (Assembly).GetMethod(GET_NAME, Type.EmptyTypes));
                proxyMthIL.Emit(OpCodes.Ldloc, wrappedType);
                proxyMthIL.Emit(OpCodes.Callvirt,
                                typeof (Type).GetMethod(GET_FULL_NAME_PROP));
                proxyMthIL.Emit(OpCodes.Ldloc, targetMethod);
                proxyMthIL.Emit(OpCodes.Callvirt,
                                typeof (MemberInfo).GetMethod(GET_NAME_PROP));
                argTypes = new Type[3];
                argTypes[0] = typeof (AssemblyName);
                argTypes[1] = typeof (String);
                argTypes[2] = typeof (String);
                proxyMthIL.Emit(OpCodes.Call,
                                typeof (MethodContextInfo).GetConstructor(argTypes));
                //  "invokeMethod()".
                proxyMthIL.Emit(OpCodes.Ldarg_0);
                proxyMthIL.Emit(OpCodes.Ldloc, targetMethod);
                proxyMthIL.Emit(OpCodes.Ldloca, argValues);
                proxyMthIL.Emit(OpCodes.Ldloc, methodCxtInfo);
                proxyMthIL.Emit(OpCodes.Call, InvokeMethod);

                //  Set the method value (if any exists).
                if (typeof (void) != mi.ReturnType)
                {
                    //  Need to be careful here.  If the return type 
                    //  is null, then we leave retVal as-is.
                    Label retIsNull = proxyMthIL.DefineLabel();
                    proxyMthIL.Emit(OpCodes.Stloc, tempRetVal);
                    proxyMthIL.Emit(OpCodes.Ldloc, tempRetVal);
                    proxyMthIL.Emit(OpCodes.Brfalse, retIsNull);
                    proxyMthIL.Emit(OpCodes.Ldloc, tempRetVal);

                    if (mi.ReturnType.IsValueType)
                    {
                        //  Unbox whatever is on the stack.
                        proxyMthIL.Emit(OpCodes.Unbox, mi.ReturnType);
                        //  Note:  See pg. 72 of Partition III to see why
                        //  I chose the general ldobj over ldind.
                        proxyMthIL.Emit(OpCodes.Ldobj, mi.ReturnType);
                    }
                    else
                    {
                        proxyMthIL.Emit(OpCodes.Castclass, mi.ReturnType);
                    }

                    proxyMthIL.Emit(OpCodes.Stloc, retVal);
                    proxyMthIL.MarkLabel(retIsNull);
                }
                else
                {
                    proxyMthIL.Emit(OpCodes.Pop);
                }

                //  Move the ByRef or out arg values from the array
                //  to the arg values.
                foreach (ParameterInfo argValueByRef in mi.GetParameters())
                {
                    if (argValueByRef.ParameterType.IsByRef ||
                        argValueByRef.IsOut)
                    {
                        paramRetTypeName = argValueByRef.ParameterType.ToString();
                        paramRetTypeName = paramRetTypeName.Replace("&", "");
                        paramRetType = Type.GetType(paramRetTypeName);
                        proxyMthIL.Emit(OpCodes.Ldarg, argValueByRef.Position + 1);
                        proxyMthIL.Emit(OpCodes.Ldloc, argValues);
                        proxyMthIL.Emit(OpCodes.Ldc_I4, argValueByRef.Position);
                        proxyMthIL.Emit(OpCodes.Ldelem_Ref);
                        if (paramRetType.IsValueType)
                        {
                            proxyMthIL.Emit(OpCodes.Unbox, paramRetType);
                            proxyMthIL.Emit(OpCodes.Ldobj, paramRetType);
                            proxyMthIL.Emit(OpCodes.Stobj, paramRetType);
                        }
                        else
                        {
                            if (paramRetType != typeof (Object))
                            {
                                proxyMthIL.Emit(OpCodes.Castclass, paramRetType);
                            }
                            proxyMthIL.Emit(OpCodes.Stind_Ref);
                        }
                    }
                }

                //  Finally...return.
                if (typeof (void) != mi.ReturnType)
                {
                    proxyMthIL.Emit(OpCodes.Ldloc, retVal);
                }

                proxyMthIL.Emit(OpCodes.Ret);
            }
        }

        #endregion

        #region Public Static Methods

        public static Object Create(Object BaseObject, InvocationHandler[] Handlers)
        {
            #region Variable Declarations

            String assemblyKey = null;
            AssemblyName baseAssemblyName = null;
            Type baseType = null;
            FieldBuilder invokeHandlers = null;
            MethodInfo invokeMethod = null;
            Object newProxy = null;
            Type newProxyType = null;
            AssemblyName newAssemblyName = null;
            AssemblyBuilder proxyAssembly = null;
            Object[] proxyCtorArgs = null;
            ModuleBuilder proxyModule = null;
            TypeBuilder proxyType = null;
            Hashtable targets = null;
            String typeKey = null;
            FieldBuilder wrappedObject = null;

            #endregion

            #region Preconditions

            if (null == BaseObject)
            {
                throw new ArgumentNullException(BASE_OBJECT_ARG_NAME, ERROR_BASE_OBJECT_NULL);
            }

            if (null == Handlers)
            {
                throw new ArgumentNullException(HANDLERS_ARG_NAME, ERROR_HANDLERS_NULL);
            }

            if (0 == Handlers.Length)
            {
                throw new ArgumentException(ERROR_HANDLERS_NONE, HANDLERS_ARG_NAME);
            }

            if (BaseObject.GetType().IsAbstract)
            {
                throw new ArgumentException(ERROR_BASE_OBJECT_ABSTRACT, BASE_OBJECT_ARG_NAME);
            }

            if (BaseObject.GetType().IsSealed)
            {
                throw new ArgumentException(ERROR_BASE_OBJECT_SEALED, BASE_OBJECT_ARG_NAME);
            }

            targets = GetTargetMethods(BaseObject);

            if (0 == targets.Count)
            {
                throw new ArgumentException(ERROR_BASE_OBJECT_NO_METHODS, BASE_OBJECT_ARG_NAME);
            }

            #endregion

            //  Check to see if this proxy has already been created.
            baseType = BaseObject.GetType();

            typeKey = baseType.Assembly.FullName + "::" +
                      baseType.FullName;
            typeKey = typeKey.GetHashCode().ToString();

            proxyCtorArgs = new Object[2];
            proxyCtorArgs[0] = BaseObject;
            proxyCtorArgs[1] = Handlers;

            if (createdTypes.ContainsKey(typeKey))
            {
                newProxy = Activator.CreateInstance(
                    (Type) createdTypes[typeKey], proxyCtorArgs);
            }
            else
            {
                //  Check to see if the module already exists.
                assemblyKey = baseType.Assembly.FullName.GetHashCode().ToString();

                if (createdModules.ContainsKey(assemblyKey))
                {
                    proxyModule = (ModuleBuilder) createdModules[assemblyKey];
                }
                else
                {
                    //  Define this assembly's name.
                    newAssemblyName = new AssemblyName();
                    baseAssemblyName = baseType.Assembly.GetName();
                    newAssemblyName.Name = baseAssemblyName.Name + assemblyKey;
                    newAssemblyName.Version = baseAssemblyName.Version;

#if DEBUG
                    proxyAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                        newAssemblyName, AssemblyBuilderAccess.RunAndSave);

                    proxyModule = proxyAssembly.DefineDynamicModule(newAssemblyName.Name,
                                                                    newAssemblyName.Name + ".dll");
#else
					proxyAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
						newAssemblyName, AssemblyBuilderAccess.Run);

					proxyModule = proxyAssembly.DefineDynamicModule(newAssemblyName.Name);
#endif
                    createdModules.Add(assemblyKey, proxyModule);
                }

                //  Create the new type.
                proxyType = proxyModule.DefineType(
                    baseType.Namespace + "." + baseType.Name + typeKey,
                    TypeAttributes.Class | TypeAttributes.Sealed |
                    TypeAttributes.Public, baseType);

                //  Add the two private fields.
                invokeHandlers = proxyType.DefineField(INVOKE_HANDLER_FIELD,
                                                       Handlers.GetType(), FieldAttributes.Private);
                wrappedObject = proxyType.DefineField(WRAPPED_OBJECT_FIELD,
                                                      BaseObject.GetType(), FieldAttributes.Private);

                //  Build the type.
                BuildParameterlessConstructor(proxyType, baseType);
                BuildConstructor(baseType, proxyType, wrappedObject, invokeHandlers);
                AddInterfaces(baseType, proxyType);
                invokeMethod = BuildInvokeMethod(proxyType, invokeHandlers,
                                                 wrappedObject);

                BuildTargetMethods(proxyType, invokeMethod, wrappedObject, targets);
                //  Bake the type and create an instance.
                newProxyType = proxyType.CreateType();

#if DEBUG
                if (null == createdAssemblies[assemblyKey])
                {
                    createdAssemblies.Add(assemblyKey, proxyAssembly);
                }
#endif

                newProxy = Activator.CreateInstance(newProxyType, proxyCtorArgs);
                createdTypes.Add(typeKey, newProxyType);
            }
            return newProxy;
        }

        #endregion

#if DEBUG
        public static void Persist()
        {
            foreach (AssemblyBuilder ab in createdAssemblies.Values)
            {
                ab.Save(ab.GetName().Name + ".dll");
            }
        }
#endif
    }
}