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

namespace AppComponents.Dynamic
{

    #region Interfaces

    public interface IBuilder
    {
        dynamic Object { get; }

        dynamic Setup { get; }


        dynamic Array(params dynamic[] contents);

        dynamic ArraySetup<TList>();

        dynamic ArraySetup(params dynamic[] constructorArgs);

        dynamic ArraySetup(Func<object[]> constructorArgsFactory);

        dynamic List(params dynamic[] contents);

        dynamic ListSetup(params dynamic[] constructorArgs);

        dynamic ListSetup<TList>();

        dynamic ListSetup(Func<object[]> constructorArgsFactory);

        dynamic ObjectSetup(params dynamic[] constructorArgs);

        dynamic ObjectSetup(Func<object[]> constructorArgsFactory);
    }

    #endregion Interfaces

    #region Classes

    public class Builder<TObjectProtoType> : ShapeableObject, IBuilder
    {
        protected IDictionary<string, Activate> _buildType;

        public Builder()
        {
            _buildType = new Dictionary<string, Activate>();
            Setup = new SetupTrampoline(this);
            Object = new BuilderTrampoline(this);
        }

        #region IBuilder Members

        public dynamic Object { get; private set; }

        public dynamic Setup { get; private set; }


        public dynamic Array(params dynamic[] contents)
        {
            return List(contents);
        }

        public dynamic ArraySetup<TList>()
        {
            return ListSetup(new Activate<TList>());
        }

        public dynamic ArraySetup(params dynamic[] constructorArgs)
        {
            return ListSetup(constructorArgs);
        }

        public dynamic ArraySetup(Func<object[]> constructorArgsFactory)
        {
            return ListSetup((object) constructorArgsFactory);
        }

        public dynamic List(params dynamic[] contents)
        {
            Activate buildType;
            if (!_buildType.TryGetValue("List", out buildType))
                buildType = null;

            if (buildType != null)
            {
                dynamic builtContents = buildType.Create();

                if (contents != null)
                {
                    foreach (var item in contents)
                    {
                        builtContents.Add(item);
                    }
                }
                return builtContents;
            }

            return new ShapeableExpandoList(contents);
        }

        public dynamic ListSetup(params dynamic[] constructorArgs)
        {
            var activation = constructorArgs.OfType<Activate>().SingleOrDefault();

            if (null == activation)
            {
                if (!_buildType.TryGetValue("Object", out activation))
                    activation = null;
                if (activation != null)
                {
                    activation = new Activate(activation.Type, constructorArgs);
                }
                if (null == activation)
                    activation = new Activate<ShapeableExpandoList>(constructorArgs);
            }

            _buildType["List"] = activation;
            _buildType["Array"] = activation;
            return this;
        }

        public dynamic ListSetup<TList>()
        {
            return ListSetup(new Activate<TList>());
        }

        public dynamic ListSetup(Func<object[]> constructorArgsFactory)
        {
            return ListSetup((object) constructorArgsFactory);
        }

        public dynamic ObjectSetup(params dynamic[] constructorArgs)
        {
            _buildType["Object"] = new Activate<TObjectProtoType>(constructorArgs);
            return this;
        }

        public dynamic ObjectSetup(Func<object[]> constructorArgsFactory)
        {
            return ObjectSetup((object) constructorArgsFactory);
        }

        #endregion

        private static object InvokeHelper(CallInfo callinfo, IList<object> args, Activate buildType = null)
        {
            bool setWithName = true;
            object theArgument = null;
            if (callinfo.ArgumentNames.Count == 0 && callinfo.ArgumentCount == 1)
            {
                theArgument = args[0];

                if (TypeFactorization.IsTypeAnonymous(theArgument) ||
                    theArgument is IEnumerable<KeyValuePair<string, object>>)
                {
                    setWithName = false;
                }
            }

            if (setWithName && callinfo.ArgumentNames.Count != callinfo.ArgumentCount)
                throw new ArgumentException("Requires argument names for every argument");

            object result;
            if (buildType != null)
            {
                result = buildType.Create();
            }
            else
            {
                try
                {
                    result = Activator.CreateInstance<TObjectProtoType>();
                }
                catch (MissingMethodException)
                {
                    result = InvocationBinding.CreateInstance(typeof (TObjectProtoType));
                }
            }
            if (setWithName)
            {
                theArgument = callinfo.ArgumentNames.Zip(args, (n, a) => new KeyValuePair<string, object>(n, a));
            }

            return InvocationBinding.InvokeSetAll(result, theArgument);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            Type type;

            Activate buildType;
            if (!_buildType.TryGetValue(binder.Name, out buildType))
                buildType = null;

            if (buildType == null && !_buildType.TryGetValue("Object", out buildType))
                buildType = null;

            result = InvokeHelper(binder.CallInfo, args, buildType);
            if (GetTypeForPropertyNameFromInterface(binder.Name, out type))
            {
                if (type.IsInterface && result != null && !type.IsAssignableFrom(result.GetType()))
                {
                    result = InvocationBinding.DynamicDressedAs(result, type);
                }
            }
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, dynamic value)
        {
            if (value != null)
            {
                if (value is Type)
                {
                    _buildType[binder.Name] = new Activate(value);
                    return true;
                }

                if (value is Activate)
                {
                    _buildType[binder.Name] = value;
                    return true;
                }
            }
            else
            {
                _buildType[binder.Name] = null;
                return true;
            }
            return false;
        }

        #region Nested type: BuilderTrampoline

        public class BuilderTrampoline : DynamicObject
        {
            private Builder<TObjectProtoType> _builder;


            public BuilderTrampoline(Builder<TObjectProtoType> builder)
            {
                _builder = builder;
            }

            public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
            {
                Activate buildType;
                if (!_builder._buildType.TryGetValue("Object", out buildType))
                    buildType = null;

                result = InvokeHelper(binder.CallInfo, args, buildType);
                return true;
            }
        }

        #endregion

        #region Nested type: SetupTrampoline

        public class SetupTrampoline : DynamicObject
        {
            private Builder<TObjectProtoType> _builder;


            public SetupTrampoline(Builder<TObjectProtoType> builder)
            {
                _builder = builder;
            }

            public override bool TryInvoke(InvokeBinder binder, dynamic[] args, out object result)
            {
                if (binder.CallInfo.ArgumentNames.Count != binder.CallInfo.ArgumentCount)
                    throw new ArgumentException("Requires argument names for every argument");

                var theArguments = args.Select(it => it is Type ? new Activate(it) : (Activate) it);
                foreach (
                    var kvp in
                        binder.CallInfo.ArgumentNames.Zip(theArguments,
                                                          (n, a) => new KeyValuePair<string, Activate>(n, a)))
                {
                    _builder._buildType[kvp.Key] = kvp.Value;
                }
                result = _builder;
                return true;
            }
        }

        #endregion
    }

    #endregion Classes
}