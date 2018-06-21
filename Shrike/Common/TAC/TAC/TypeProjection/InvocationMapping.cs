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
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AppComponents.Dynamic
{
    internal static partial class InvocationMapping
    {
        internal static readonly Type[] FuncPrototypes;
        internal static readonly Type[] ActionPrototypes;
        internal static readonly IDictionary<Type, int> FuncPrototypeArgCount;
        internal static readonly IDictionary<Type, int> ActionPrototypeArgCount;

        static InvocationMapping()
        {
            FuncPrototypes = new[]
                                 {
                                     typeof (Func<>), //0
                                     typeof (Func<,>), //1
                                     typeof (Func<,,>), //2
                                     typeof (Func<,,,>), //3
                                     typeof (Func<,,,,>), //4
                                     typeof (Func<,,,,,>), //5
                                     typeof (Func<,,,,,,>), //6
                                     typeof (Func<,,,,,,,>), //7
                                     typeof (Func<,,,,,,,,>), //8
                                     typeof (Func<,,,,,,,,,>), //9
                                     typeof (Func<,,,,,,,,,,>), //10
                                     typeof (Func<,,,,,,,,,,,>), //11
                                     typeof (Func<,,,,,,,,,,,,>), //12
                                     typeof (Func<,,,,,,,,,,,,,>), //13
                                     typeof (Func<,,,,,,,,,,,,,,>), //14
                                     typeof (Func<,,,,,,,,,,,,,,,>), //15
                                     typeof (Func<,,,,,,,,,,,,,,,,>), //16
                                 };

            ActionPrototypes = new[]
                                   {
                                       typeof (Action), //0
                                       typeof (Action<>), //1
                                       typeof (Action<,>), //2
                                       typeof (Action<,,>), //3
                                       typeof (Action<,,,>), //4
                                       typeof (Action<,,,,>), //5
                                       typeof (Action<,,,,,>), //6
                                       typeof (Action<,,,,,,>), //7
                                       typeof (Action<,,,,,,,>), //8
                                       typeof (Action<,,,,,,,,>), //9
                                       typeof (Action<,,,,,,,,,>), //10
                                       typeof (Action<,,,,,,,,,,>), //11
                                       typeof (Action<,,,,,,,,,,,>), //12
                                       typeof (Action<,,,,,,,,,,,,>), //13
                                       typeof (Action<,,,,,,,,,,,,,>), //14
                                       typeof (Action<,,,,,,,,,,,,,,>), //15
                                       typeof (Action<,,,,,,,,,,,,,,,>), //16
                                   };


            FuncPrototypeArgCount =
                FuncPrototypes.Zip(Enumerable.Range(0, FuncPrototypes.Length), (key, value) => new {key, value}).
                    ToDictionary(k => k.key, v => v.value);
            ActionPrototypeArgCount =
                ActionPrototypes.Zip(Enumerable.Range(0, ActionPrototypes.Length), (key, value) => new {key, value}).
                    ToDictionary(k => k.key, v => v.value);
        }

        #region

        internal static void MemberActionInvoke(ref CallSite callSite,
                                                Type binderType,
                                                LazyBinder binder,
                                                MemberInvocationMoniker name,
                                                bool staticContext,
                                                Type context,
                                                string[] argumentNames,
                                                object target,
                                                params object[] arguments)
        {
            var prototypeSwitch = arguments.Length;
            switch (prototypeSwitch)
            {
                case 0:
                    {
                        var tCallSite = (CallSite<Action<CallSite, object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite = CreateCallSite<Action<CallSite, object>>(binderType, binder, name, context,
                                                                                 argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target);
                        break;
                    }
                case 1:
                    {
                        var tCallSite = (CallSite<Action<CallSite, object, object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite = CreateCallSite<Action<CallSite, object, object>>(binderType, binder, name,
                                                                                         context, argumentNames,
                                                                                         staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0]);
                        break;
                    }
                case 2:
                    {
                        var tCallSite = (CallSite<Action<CallSite, object, object, object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite = CreateCallSite<Action<CallSite, object, object, object>>(binderType, binder,
                                                                                                 name, context,
                                                                                                 argumentNames,
                                                                                                 staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1]);
                        break;
                    }
                case 3:
                    {
                        var tCallSite = (CallSite<Action<CallSite, object, object, object, object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite = CreateCallSite<Action<CallSite, object, object, object, object>>(binderType,
                                                                                                         binder, name,
                                                                                                         context,
                                                                                                         argumentNames,
                                                                                                         staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2]);
                        break;
                    }
                case 4:
                    {
                        var tCallSite = (CallSite<Action<CallSite, object, object, object, object, object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite<Action<CallSite, object, object, object, object, object>>(binderType,
                                                                                                         binder, name,
                                                                                                         context,
                                                                                                         argumentNames,
                                                                                                         staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2], arguments[3]);
                        break;
                    }
                case 5:
                    {
                        var tCallSite =
                            (CallSite<Action<CallSite, object, object, object, object, object, object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite<Action<CallSite, object, object, object, object, object, object>>(
                                    binderType, binder, name, context, argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2], arguments[3],
                                         arguments[4]);
                        break;
                    }
                case 6:
                    {
                        var tCallSite =
                            (CallSite<Action<CallSite, object, object, object, object, object, object, object>>)
                            callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite<Action<CallSite, object, object, object, object, object, object, object>>
                                    (binderType, binder, name, context, argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2], arguments[3],
                                         arguments[4], arguments[5]);
                        break;
                    }
                case 7:
                    {
                        var tCallSite =
                            (CallSite<Action<CallSite, object, object, object, object, object, object, object, object>>)
                            callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <Action<CallSite, object, object, object, object, object, object, object, object>>(
                                        binderType, binder, name, context, argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2], arguments[3],
                                         arguments[4], arguments[5], arguments[6]);
                        break;
                    }
                case 8:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Action<CallSite, object, object, object, object, object, object, object, object, object>
                                >) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Action
                                            <CallSite, object, object, object, object, object, object, object, object,
                                                object>>(binderType, binder, name, context, argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2], arguments[3],
                                         arguments[4], arguments[5], arguments[6], arguments[7]);
                        break;
                    }
                case 9:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Action
                                <CallSite, object, object, object, object, object, object, object, object, object,
                                object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Action
                                            <CallSite, object, object, object, object, object, object, object, object,
                                                object, object>>(binderType, binder, name, context, argumentNames,
                                                                 staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2], arguments[3],
                                         arguments[4], arguments[5], arguments[6], arguments[7], arguments[8]);
                        break;
                    }
                case 10:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Action
                                <CallSite, object, object, object, object, object, object, object, object, object,
                                object, object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Action
                                            <CallSite, object, object, object, object, object, object, object, object,
                                                object, object, object>>(binderType, binder, name, context,
                                                                         argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2], arguments[3],
                                         arguments[4], arguments[5], arguments[6], arguments[7], arguments[8],
                                         arguments[9]);
                        break;
                    }
                case 11:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Action
                                <CallSite, object, object, object, object, object, object, object, object, object,
                                object, object, object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Action
                                            <CallSite, object, object, object, object, object, object, object, object,
                                                object, object, object, object>>(binderType, binder, name, context,
                                                                                 argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2], arguments[3],
                                         arguments[4], arguments[5], arguments[6], arguments[7], arguments[8],
                                         arguments[9], arguments[10]);
                        break;
                    }
                case 12:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Action
                                <CallSite, object, object, object, object, object, object, object, object, object,
                                object, object, object, object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Action
                                            <CallSite, object, object, object, object, object, object, object, object,
                                                object, object, object, object, object>>(binderType, binder, name,
                                                                                         context, argumentNames,
                                                                                         staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2], arguments[3],
                                         arguments[4], arguments[5], arguments[6], arguments[7], arguments[8],
                                         arguments[9], arguments[10], arguments[11]);
                        break;
                    }
                case 13:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Action
                                <CallSite, object, object, object, object, object, object, object, object, object,
                                object, object, object, object, object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Action
                                            <CallSite, object, object, object, object, object, object, object, object,
                                                object, object, object, object, object, object>>(binderType, binder,
                                                                                                 name, context,
                                                                                                 argumentNames,
                                                                                                 staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2], arguments[3],
                                         arguments[4], arguments[5], arguments[6], arguments[7], arguments[8],
                                         arguments[9], arguments[10], arguments[11], arguments[12]);
                        break;
                    }
                case 14:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Action
                                <CallSite, object, object, object, object, object, object, object, object, object,
                                object, object, object, object, object, object>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Action
                                            <CallSite, object, object, object, object, object, object, object, object,
                                                object, object, object, object, object, object, object>>(binderType,
                                                                                                         binder, name,
                                                                                                         context,
                                                                                                         argumentNames,
                                                                                                         staticContext);
                            callSite = tCallSite;
                        }
                        tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2], arguments[3],
                                         arguments[4], arguments[5], arguments[6], arguments[7], arguments[8],
                                         arguments[9], arguments[10], arguments[11], arguments[12], arguments[13]);
                        break;
                    }
                default:
                    var tArgTypes = Enumerable.Repeat(typeof (object), prototypeSwitch);
                    var tDelagateType = BuildProxy.GenerateCallSiteFuncType(tArgTypes, typeof (void));
                    InvocationBinding.InvokeCallSite(
                        CreateCallSite(tDelagateType, binderType, binder, name, context, argumentNames), target,
                        arguments);
                    break;
            }
        }

        #endregion

        #region

        internal static TReturn MemberTargetTypeInvoke<TTarget, TReturn>(
            ref CallSite callSite,
            Type binderType,
            LazyBinder binder,
            MemberInvocationMoniker name,
            bool staticContext,
            Type context,
            string[] argumentNames,
            TTarget target,
            params object[] arguments)
        {
            var tSwitch = arguments.Length;

            switch (tSwitch)
            {
                case 0:
                    {
                        var tCallSite = (CallSite<Func<CallSite, TTarget, TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite = CreateCallSite<Func<CallSite, TTarget, TReturn>>(binderType, binder, name,
                                                                                         context, argumentNames,
                                                                                         staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target);
                    }
                case 1:
                    {
                        var tCallSite = (CallSite<Func<CallSite, TTarget, object, TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite = CreateCallSite<Func<CallSite, TTarget, object, TReturn>>(binderType, binder,
                                                                                                 name, context,
                                                                                                 argumentNames,
                                                                                                 staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0]);
                    }
                case 2:
                    {
                        var tCallSite = (CallSite<Func<CallSite, TTarget, object, object, TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite = CreateCallSite<Func<CallSite, TTarget, object, object, TReturn>>(binderType,
                                                                                                         binder, name,
                                                                                                         context,
                                                                                                         argumentNames,
                                                                                                         staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1]);
                    }
                case 3:
                    {
                        var tCallSite = (CallSite<Func<CallSite, TTarget, object, object, object, TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite<Func<CallSite, TTarget, object, object, object, TReturn>>(binderType,
                                                                                                         binder, name,
                                                                                                         context,
                                                                                                         argumentNames,
                                                                                                         staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2]);
                    }
                case 4:
                    {
                        var tCallSite =
                            (CallSite<Func<CallSite, TTarget, object, object, object, object, TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite<Func<CallSite, TTarget, object, object, object, object, TReturn>>(
                                    binderType, binder, name, context, argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2],
                                                arguments[3]);
                    }
                case 5:
                    {
                        var tCallSite =
                            (CallSite<Func<CallSite, TTarget, object, object, object, object, object, TReturn>>)
                            callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite<Func<CallSite, TTarget, object, object, object, object, object, TReturn>>
                                    (binderType, binder, name, context, argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2],
                                                arguments[3], arguments[4]);
                    }
                case 6:
                    {
                        var tCallSite =
                            (CallSite<Func<CallSite, TTarget, object, object, object, object, object, object, TReturn>>)
                            callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <Func<CallSite, TTarget, object, object, object, object, object, object, TReturn>>(
                                        binderType, binder, name, context, argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2],
                                                arguments[3], arguments[4], arguments[5]);
                    }
                case 7:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Func<CallSite, TTarget, object, object, object, object, object, object, object, TReturn>
                                >) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Func
                                            <CallSite, TTarget, object, object, object, object, object, object, object,
                                                TReturn>>(binderType, binder, name, context, argumentNames,
                                                          staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2],
                                                arguments[3], arguments[4], arguments[5], arguments[6]);
                    }
                case 8:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Func
                                <CallSite, TTarget, object, object, object, object, object, object, object, object,
                                TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Func
                                            <CallSite, TTarget, object, object, object, object, object, object, object,
                                                object, TReturn>>(binderType, binder, name, context, argumentNames,
                                                                  staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2],
                                                arguments[3], arguments[4], arguments[5], arguments[6], arguments[7]);
                    }
                case 9:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Func
                                <CallSite, TTarget, object, object, object, object, object, object, object, object,
                                object, TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Func
                                            <CallSite, TTarget, object, object, object, object, object, object, object,
                                                object, object, TReturn>>(binderType, binder, name, context,
                                                                          argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2],
                                                arguments[3], arguments[4], arguments[5], arguments[6], arguments[7],
                                                arguments[8]);
                    }
                case 10:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Func
                                <CallSite, TTarget, object, object, object, object, object, object, object, object,
                                object, object, TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Func
                                            <CallSite, TTarget, object, object, object, object, object, object, object,
                                                object, object, object, TReturn>>(binderType, binder, name, context,
                                                                                  argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2],
                                                arguments[3], arguments[4], arguments[5], arguments[6], arguments[7],
                                                arguments[8], arguments[9]);
                    }
                case 11:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Func
                                <CallSite, TTarget, object, object, object, object, object, object, object, object,
                                object, object, object, TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Func
                                            <CallSite, TTarget, object, object, object, object, object, object, object,
                                                object, object, object, object, TReturn>>(binderType, binder, name,
                                                                                          context, argumentNames,
                                                                                          staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2],
                                                arguments[3], arguments[4], arguments[5], arguments[6], arguments[7],
                                                arguments[8], arguments[9], arguments[10]);
                    }
                case 12:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Func
                                <CallSite, TTarget, object, object, object, object, object, object, object, object,
                                object, object, object, object, TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Func
                                            <CallSite, TTarget, object, object, object, object, object, object, object,
                                                object, object, object, object, object, TReturn>>(binderType, binder,
                                                                                                  name, context,
                                                                                                  argumentNames,
                                                                                                  staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2],
                                                arguments[3], arguments[4], arguments[5], arguments[6], arguments[7],
                                                arguments[8], arguments[9], arguments[10], arguments[11]);
                    }
                case 13:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Func
                                <CallSite, TTarget, object, object, object, object, object, object, object, object,
                                object, object, object, object, object, TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Func
                                            <CallSite, TTarget, object, object, object, object, object, object, object,
                                                object, object, object, object, object, object, TReturn>>(binderType,
                                                                                                          binder, name,
                                                                                                          context,
                                                                                                          argumentNames,
                                                                                                          staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2],
                                                arguments[3], arguments[4], arguments[5], arguments[6], arguments[7],
                                                arguments[8], arguments[9], arguments[10], arguments[11], arguments[12]);
                    }
                case 14:
                    {
                        var tCallSite =
                            (
                            CallSite
                                <
                                Func
                                <CallSite, TTarget, object, object, object, object, object, object, object, object,
                                object, object, object, object, object, object, TReturn>>) callSite;
                        if (tCallSite == null)
                        {
                            tCallSite =
                                CreateCallSite
                                    <
                                        Func
                                            <CallSite, TTarget, object, object, object, object, object, object, object,
                                                object, object, object, object, object, object, object, TReturn>>(
                                                    binderType, binder, name, context, argumentNames, staticContext);
                            callSite = tCallSite;
                        }
                        return tCallSite.Target(tCallSite, target, arguments[0], arguments[1], arguments[2],
                                                arguments[3], arguments[4], arguments[5], arguments[6], arguments[7],
                                                arguments[8], arguments[9], arguments[10], arguments[11], arguments[12],
                                                arguments[13]);
                    }
                default:
                    var tArgTypes = Enumerable.Repeat(typeof (object), tSwitch);
                    var tDelagateType = BuildProxy.GenerateCallSiteFuncType(tArgTypes, typeof (TTarget));
                    return
                        InvocationBinding.InvokeCallSite(
                            CreateCallSite(tDelagateType, binderType, binder, name, context, argumentNames), target,
                            arguments);
            }
        }

        #endregion

#if !__MonoCS__
        internal static Delegate WrapFunctionToDelegate<TReturn>(dynamic functor, int length)
        {
            switch (length)
            {
                case 0:
                    return new Func<TReturn>(() => functor());
                case 1:
                    return new Func<object, TReturn>((a1) => functor(a1));
                case 2:
                    return new Func<object, object, TReturn>((a1, a2) => functor(a1, a2));
                case 3:
                    return new Func<object, object, object, TReturn>((a1, a2, a3) => functor(a1, a2, a3));
                case 4:
                    return new Func<object, object, object, object, TReturn>((a1, a2, a3, a4) => functor(a1, a2, a3, a4));
                case 5:
                    return
                        new Func<object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5) => functor(a1, a2, a3, a4, a5));
                case 6:
                    return
                        new Func<object, object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6) => functor(a1, a2, a3, a4, a5, a6));
                case 7:
                    return
                        new Func<object, object, object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7) => functor(a1, a2, a3, a4, a5, a6, a7));
                case 8:
                    return
                        new Func<object, object, object, object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8) => functor(a1, a2, a3, a4, a5, a6, a7, a8));
                case 9:
                    return
                        new Func<object, object, object, object, object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9) => functor(a1, a2, a3, a4, a5, a6, a7, a8, a9));
                case 10:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10));
                case 11:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11));
                case 12:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12));
                case 13:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13));
                case 14:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14));
                case 15:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15));
                case 16:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16));

                default:
                    return new DynamicFunc<TReturn>(args => (TReturn) InvocationBinding.Invoke((object) functor, args));
            }
        }
#endif

        internal static class ConvertCallSiteForMono<T>
        {
            internal static CallSite CallSite;
        }

        internal static Delegate WrapFunctionToDelegateMono<TReturn>(dynamic functor, int length)
        {
            switch (length)
            {
                case 0:
                    return new Func<TReturn>(() =>
                                                 {
                                                     object tResult = functor();
                                                     return
                                                         (TReturn)
                                                         InvokeConvertCallSite(tResult, true, typeof (TReturn),
                                                                               typeof (object),
                                                                               ref
                                                                                   ConvertCallSiteForMono<TReturn>.
                                                                                   CallSite);
                                                 });
                case 1:
                    return new Func<object, TReturn>((a1) =>
                                                         {
                                                             object tResult = functor(a1);
                                                             return
                                                                 (TReturn)
                                                                 InvokeConvertCallSite(tResult, true, typeof (TReturn),
                                                                                       typeof (object),
                                                                                       ref
                                                                                           ConvertCallSiteForMono
                                                                                           <TReturn>.CallSite);
                                                         });
                case 2:
                    return new Func<object, object, TReturn>((a1, a2) =>
                                                                 {
                                                                     object tResult = functor(a1, a2);
                                                                     return
                                                                         (TReturn)
                                                                         InvokeConvertCallSite(tResult, true,
                                                                                               typeof (TReturn),
                                                                                               typeof (object),
                                                                                               ref
                                                                                                   ConvertCallSiteForMono
                                                                                                   <TReturn>.CallSite);
                                                                 });
                case 3:
                    return new Func<object, object, object, TReturn>((a1, a2, a3) =>
                                                                         {
                                                                             object tResult = functor(a1, a2, a3);
                                                                             return
                                                                                 (TReturn)
                                                                                 InvokeConvertCallSite(tResult, true,
                                                                                                       typeof (TReturn),
                                                                                                       typeof (object),
                                                                                                       ref
                                                                                                           ConvertCallSiteForMono
                                                                                                           <TReturn>.
                                                                                                           CallSite);
                                                                         });
                case 4:
                    return new Func<object, object, object, object, TReturn>((a1, a2, a3, a4) =>
                                                                                 {
                                                                                     object tResult = functor(a1, a2, a3,
                                                                                                              a4);
                                                                                     return
                                                                                         (TReturn)
                                                                                         InvokeConvertCallSite(tResult,
                                                                                                               true,
                                                                                                               typeof (
                                                                                                                   TReturn
                                                                                                                   ),
                                                                                                               typeof (
                                                                                                                   object
                                                                                                                   ),
                                                                                                               ref
                                                                                                                   ConvertCallSiteForMono
                                                                                                                   <
                                                                                                                   TReturn
                                                                                                                   >.
                                                                                                                   CallSite);
                                                                                 });
                case 5:
                    return new Func<object, object, object, object, object, TReturn>((a1, a2, a3, a4, a5) =>
                                                                                         {
                                                                                             object tResult = functor(
                                                                                                 a1, a2, a3, a4, a5);
                                                                                             return
                                                                                                 (TReturn)
                                                                                                 InvokeConvertCallSite(
                                                                                                     tResult, true,
                                                                                                     typeof (TReturn),
                                                                                                     typeof (object),
                                                                                                     ref
                                                                                                         ConvertCallSiteForMono
                                                                                                         <TReturn>.
                                                                                                         CallSite);
                                                                                         });
                case 6:
                    return new Func<object, object, object, object, object, object, TReturn>((a1, a2, a3, a4, a5, a6) =>
                                                                                                 {
                                                                                                     object tResult =
                                                                                                         functor(a1, a2,
                                                                                                                 a3, a4,
                                                                                                                 a5, a6);
                                                                                                     return
                                                                                                         (TReturn)
                                                                                                         InvokeConvertCallSite
                                                                                                             (tResult,
                                                                                                              true,
                                                                                                              typeof (
                                                                                                                  TReturn
                                                                                                                  ),
                                                                                                              typeof (
                                                                                                                  object
                                                                                                                  ),
                                                                                                              ref
                                                                                                                  ConvertCallSiteForMono
                                                                                                                  <
                                                                                                                  TReturn
                                                                                                                  >.
                                                                                                                  CallSite);
                                                                                                 });
                case 7:
                    return
                        new Func<object, object, object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7) =>
                                {
                                    object tResult = functor(a1, a2, a3, a4, a5, a6, a7);
                                    return
                                        (TReturn)
                                        InvokeConvertCallSite(tResult, true, typeof (TReturn), typeof (object),
                                                              ref ConvertCallSiteForMono<TReturn>.CallSite);
                                });
                case 8:
                    return
                        new Func<object, object, object, object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8) =>
                                {
                                    object tResult = functor(a1, a2, a3, a4, a5, a6, a7, a8);
                                    return
                                        (TReturn)
                                        InvokeConvertCallSite(tResult, true, typeof (TReturn), typeof (object),
                                                              ref ConvertCallSiteForMono<TReturn>.CallSite);
                                });
                case 9:
                    return
                        new Func<object, object, object, object, object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9) =>
                                {
                                    object tResult = functor(a1, a2, a3, a4, a5, a6, a7, a8, a9);
                                    return
                                        (TReturn)
                                        InvokeConvertCallSite(tResult, true, typeof (TReturn), typeof (object),
                                                              ref ConvertCallSiteForMono<TReturn>.CallSite);
                                });
                case 10:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) =>
                                {
                                    object tResult = functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10);
                                    return
                                        (TReturn)
                                        InvokeConvertCallSite(tResult, true, typeof (TReturn), typeof (object),
                                                              ref ConvertCallSiteForMono<TReturn>.CallSite);
                                });
                case 11:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                TReturn>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) =>
                                             {
                                                 object tResult = functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11);
                                                 return
                                                     (TReturn)
                                                     InvokeConvertCallSite(tResult, true, typeof (TReturn),
                                                                           typeof (object),
                                                                           ref ConvertCallSiteForMono<TReturn>.CallSite);
                                             });
                case 12:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, TReturn>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) =>
                                                     {
                                                         object tResult = functor(a1, a2, a3, a4, a5, a6, a7, a8, a9,
                                                                                  a10, a11, a12);
                                                         return
                                                             (TReturn)
                                                             InvokeConvertCallSite(tResult, true, typeof (TReturn),
                                                                                   typeof (object),
                                                                                   ref
                                                                                       ConvertCallSiteForMono<TReturn>.
                                                                                       CallSite);
                                                     });
                case 13:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object, TReturn>((a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) =>
                                                             {
                                                                 object tResult = functor(a1, a2, a3, a4, a5, a6, a7, a8,
                                                                                          a9, a10, a11, a12, a13);
                                                                 return
                                                                     (TReturn)
                                                                     InvokeConvertCallSite(tResult, true,
                                                                                           typeof (TReturn),
                                                                                           typeof (object),
                                                                                           ref
                                                                                               ConvertCallSiteForMono
                                                                                               <TReturn>.CallSite);
                                                             });
                case 14:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) =>
                                {
                                    object tResult = functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14);
                                    return
                                        (TReturn)
                                        InvokeConvertCallSite(tResult, true, typeof (TReturn), typeof (object),
                                                              ref ConvertCallSiteForMono<TReturn>.CallSite);
                                });
                case 15:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15) =>
                                {
                                    object tResult = functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14,
                                                             a15);
                                    return
                                        (TReturn)
                                        InvokeConvertCallSite(tResult, true, typeof (TReturn), typeof (object),
                                                              ref ConvertCallSiteForMono<TReturn>.CallSite);
                                });
                case 16:
                    return
                        new Func
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object, object, object, object, TReturn>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16) =>
                                {
                                    object tResult = functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14,
                                                             a15, a16);
                                    return
                                        (TReturn)
                                        InvokeConvertCallSite(tResult, true, typeof (TReturn), typeof (object),
                                                              ref ConvertCallSiteForMono<TReturn>.CallSite);
                                });

                default:
                    return new DynamicFunc<TReturn>(args =>
                                                        {
                                                            object tResult = InvocationBinding.Invoke((object) functor,
                                                                                                      args);
                                                            return
                                                                (TReturn)
                                                                InvokeConvertCallSite(tResult, true, typeof (TReturn),
                                                                                      typeof (object),
                                                                                      ref
                                                                                          ConvertCallSiteForMono
                                                                                          <TReturn>.CallSite);
                                                        });
            }
        }


        internal static Delegate WrapAction(dynamic functor, int length)
        {
            switch (length)
            {
                case 0:
                    return new Action(() => functor());
                case 1:
                    return new Action<object>((a1) => functor(a1));
                case 2:
                    return new Action<object, object>((a1, a2) => functor(a1, a2));
                case 3:
                    return new Action<object, object, object>((a1, a2, a3) => functor(a1, a2, a3));
                case 4:
                    return new Action<object, object, object, object>((a1, a2, a3, a4) => functor(a1, a2, a3, a4));
                case 5:
                    return
                        new Action<object, object, object, object, object>(
                            (a1, a2, a3, a4, a5) => functor(a1, a2, a3, a4, a5));
                case 6:
                    return
                        new Action<object, object, object, object, object, object>(
                            (a1, a2, a3, a4, a5, a6) => functor(a1, a2, a3, a4, a5, a6));
                case 7:
                    return
                        new Action<object, object, object, object, object, object, object>(
                            (a1, a2, a3, a4, a5, a6, a7) => functor(a1, a2, a3, a4, a5, a6, a7));
                case 8:
                    return
                        new Action<object, object, object, object, object, object, object, object>(
                            (a1, a2, a3, a4, a5, a6, a7, a8) => functor(a1, a2, a3, a4, a5, a6, a7, a8));
                case 9:
                    return
                        new Action<object, object, object, object, object, object, object, object, object>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9) => functor(a1, a2, a3, a4, a5, a6, a7, a8, a9));
                case 10:
                    return
                        new Action<object, object, object, object, object, object, object, object, object, object>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10));
                case 11:
                    return
                        new Action
                            <object, object, object, object, object, object, object, object, object, object, object>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11));
                case 12:
                    return
                        new Action
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12));
                case 13:
                    return
                        new Action
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13));
                case 14:
                    return
                        new Action
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object, object>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14));
                case 15:
                    return
                        new Action
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object, object, object>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15));
                case 16:
                    return
                        new Action
                            <object, object, object, object, object, object, object, object, object, object, object,
                                object, object, object, object, object>(
                            (a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16) =>
                            functor(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16));

                default:
                    return new DynamicAction(args => InvocationBinding.InvokeAction((object) functor, args));
            }
        }


        internal static object DynamicInvokeReturn(Delegate theDelegate, dynamic[] arguments)
        {
            dynamic tDel = theDelegate;
            switch (arguments.Length)
            {
                default:
                    try
                    {
                        return theDelegate.DynamicInvoke(arguments);
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw ex.InnerException;
                    }

                    #region Optimization

                case 1:
                    return tDel(arguments[0]);
                case 2:
                    return tDel(arguments[0], arguments[1]);
                case 3:
                    return tDel(arguments[0], arguments[1], arguments[2]);
                case 4:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3]);
                case 5:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4]);
                case 6:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5]);
                case 7:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                                arguments[6]);
                case 8:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                                arguments[6], arguments[7]);
                case 9:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                                arguments[6], arguments[7], arguments[8]);
                case 10:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                                arguments[6], arguments[7], arguments[8], arguments[9]);
                case 11:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                                arguments[6], arguments[7], arguments[8], arguments[9], arguments[10]);
                case 12:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                                arguments[6], arguments[7], arguments[8], arguments[9], arguments[10], arguments[11]);
                case 13:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                                arguments[6], arguments[7], arguments[8], arguments[9], arguments[10], arguments[11],
                                arguments[12]);
                case 14:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                                arguments[6], arguments[7], arguments[8], arguments[9], arguments[10], arguments[11],
                                arguments[12], arguments[13]);
                case 15:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                                arguments[6], arguments[7], arguments[8], arguments[9], arguments[10], arguments[11],
                                arguments[12], arguments[13], arguments[14]);
                case 16:
                    return tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                                arguments[6], arguments[7], arguments[8], arguments[9], arguments[10], arguments[11],
                                arguments[12], arguments[13], arguments[14], arguments[15]);

                    #endregion
            }
        }

        internal static void DynamicInvokeAction(Delegate theDelegate, params dynamic[] arguments)
        {
            dynamic tDel = theDelegate;
            switch (arguments.Length)
            {
                default:
                    try
                    {
                        theDelegate.DynamicInvoke(arguments);
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw ex.InnerException;
                    }
                    return;

                    #region Optimization

                case 1:
                    tDel(arguments[0]);
                    return;
                case 2:
                    tDel(arguments[0], arguments[1]);
                    return;
                case 3:
                    tDel(arguments[0], arguments[1], arguments[2]);
                    return;
                case 4:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3]);
                    return;
                case 5:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4]);
                    return;
                case 6:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5]);
                    return;
                case 7:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                         arguments[6]);
                    return;
                case 8:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                         arguments[6], arguments[7]);
                    return;
                case 9:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                         arguments[6], arguments[7], arguments[8]);
                    return;
                case 10:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                         arguments[6], arguments[7], arguments[8], arguments[9]);
                    return;
                case 11:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                         arguments[6], arguments[7], arguments[8], arguments[9], arguments[10]);
                    return;
                case 12:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                         arguments[6], arguments[7], arguments[8], arguments[9], arguments[10], arguments[11]);
                    return;
                case 13:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                         arguments[6], arguments[7], arguments[8], arguments[9], arguments[10], arguments[11],
                         arguments[12]);
                    return;
                case 14:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                         arguments[6], arguments[7], arguments[8], arguments[9], arguments[10], arguments[11],
                         arguments[12], arguments[13]);
                    return;
                case 15:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                         arguments[6], arguments[7], arguments[8], arguments[9], arguments[10], arguments[11],
                         arguments[12], arguments[13], arguments[14]);
                    return;
                case 16:
                    tDel(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5],
                         arguments[6], arguments[7], arguments[8], arguments[9], arguments[10], arguments[11],
                         arguments[12], arguments[13], arguments[14], arguments[15]);
                    return;

                    #endregion
            }
        }
    }
}