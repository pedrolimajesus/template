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
using System.Dynamic;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;

namespace AppComponents.Dynamic
{

    #region Classes

    public class InvokeSetters : DynamicObject
    {
        internal InvokeSetters()
        {
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            IEnumerable<KeyValuePair<string, object>> kvps = null;
            object target = null;
            result = null;

            //Setup Properties as dictionary
            if (binder.CallInfo.ArgumentNames.Any())
            {
                if (binder.CallInfo.ArgumentNames.Count + 1 == binder.CallInfo.ArgumentCount)
                {
                    target = args.First();
                    kvps = binder.CallInfo
                        .ArgumentNames
                        .Zip(args.Skip(1), (key, value) => new {key, value})
                        .ToDictionary(k => k.key, v => v.value);
                }
                else
                {
                    throw new RuntimeBinderException(
                        "InvokeSetAll requires first parameter to be target unamed, and all other parameters to be named.");
                }
            }
            else if (args.Length == 2)
            {
                target = args[0];
                if (args[1] is IEnumerable<KeyValuePair<string, object>>)
                {
                    kvps = (IEnumerable<KeyValuePair<string, object>>) args[1];
                }
                else if (args[1] is IEnumerable &&
                         args[1].GetType().IsGenericType
                    )
                {
                    var enArgs = (IEnumerable) args[1];

                    var tInterface = enArgs.GetType().GetInterface("IEnumerable`1", false);
                    if (tInterface != null)
                    {
                        var tParamTypes = tInterface.GetGenericArguments();
                        if (tParamTypes.Length == 1 &&
                            tParamTypes[0].GetGenericTypeDefinition() == typeof (Tuple<,>))
                        {
                            kvps = enArgs.Cast<dynamic>().ToDictionary(k => (string) k.Item1, v => v.Item2);
                        }
                    }
                }
                else if (TypeFactorization.IsTypeAnonymous(args[1]))
                {
                    var keyDict = new Dictionary<string, object>();
                    foreach (var tProp in args[1].GetType().GetProperties())
                    {
                        keyDict[tProp.Name] = InvocationBinding.InvokeGet(args[1], tProp.Name);
                    }
                    kvps = keyDict;
                }
            }
            //Invoke all properties
            if (target != null && kvps != null)
            {
                foreach (var pair in kvps)
                {
                    InvocationBinding.InvokeSetChain(target, pair.Key, pair.Value);
                }
                result = target;
                return true;
            }
            return false;
        }
    }

    #endregion Classes
}