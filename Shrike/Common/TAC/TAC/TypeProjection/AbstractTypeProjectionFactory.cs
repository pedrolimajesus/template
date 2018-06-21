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

namespace AppComponents.Dynamic
{

    #region Classes

    [Serializable]
    public class AbstractTypeProjectionFactory : ShapeableObject
    {
        public static T Create<T>() where T : class
        {
            return new AbstractTypeProjectionFactory().DressedAs<T>();
        }

        protected virtual object CreateType(Type type, params object[] args)
        {
            return InvocationBinding.CreateInstance(type, args);
        }

        protected virtual object GetInstanceForDynamicMember(string memberName, params object[] args)
        {
            Type type;
            return GetTypeForPropertyNameFromInterface(memberName, out type) ? CreateType(type, args) : null;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = GetInstanceForDynamicMember(binder.Name);
            return result != null;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = GetInstanceForDynamicMember(binder.Name, args);
            return result != null;
        }
    }

    public class AbstractTypeProjectionSingleInstancesFactory : AbstractTypeProjectionFactory
    {
        protected readonly Dictionary<string, dynamic> _hashFactoryTypes = new Dictionary<string, dynamic>();
        protected readonly object _lockTable = new object();


        public new static T Create<T>() where T : class
        {
            return new AbstractTypeProjectionSingleInstancesFactory().DressedAs<T>();
        }

        protected override object GetInstanceForDynamicMember(string memberName, params object[] args)
        {
            lock (_lockTable)
            {
                if (!_hashFactoryTypes.ContainsKey(memberName))
                {
                    Type type;
                    if (GetTypeForPropertyNameFromInterface(memberName, out type))
                    {
                        _hashFactoryTypes.Add(memberName, CreateType(type, args));
                    }
                    else
                    {
                        return null;
                    }
                }

                return _hashFactoryTypes[memberName];
            }
        }
    }

    #endregion Classes
}