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
using System.Runtime.Serialization;

namespace AppComponents.Dynamic
{
    [Serializable]
    public abstract class ShapeableObject :
        DynamicObject,
        IDynamicKnownInterfacesProjection,
        IDressedAs,
        ISerializable
    {
        protected static readonly IDictionary<TypeHasher, IDictionary<string, Type>> _returnTypeHashes =
            new Dictionary<TypeHasher, IDictionary<string, Type>>();

        private static readonly object _typeHashLock = new object();

        protected TypeHasher _hasher;

        protected IDictionary<string, Type> _propertySpecification;

        public ShapeableObject()
        {
        }

        protected ShapeableObject(SerializationInfo info, StreamingContext context)
        {
        }

        protected virtual IEnumerable<Type> KnownInterfaces
        {
            get
            {
                if (_propertySpecification != null)
                    return new Type[] {};

                return _hasher.Types.Cast<Type>();
            }

            set
            {
                lock (_typeHashLock)
                {
                    _propertySpecification = null;

                    _hasher = TypeHasher.Create(value);
                    if (_returnTypeHashes.ContainsKey(_hasher))
                        return;

                    var propertyReturnTypes = value.SelectMany(@interface => @interface.GetProperties())
                        .Where(property => property.GetGetMethod() != null)
                        .Select(property => new {property.Name, property.GetGetMethod().ReturnType});

                    var methodReturnTypes = value.SelectMany(@interface => @interface.GetMethods())
                        .Where(method => !method.IsSpecialName)
                        .GroupBy(method => method.Name)
                        .Where(group => group.Select(method => method.ReturnType).Distinct().Count() == 1)
                        .Select(group => new
                                             {
                                                 Name = group.Key,
                                                 ReturnType =
                                             group.Select(method => method.ReturnType).Distinct().Single()
                                             });

                    var nameToTypeMapping = propertyReturnTypes.Concat(methodReturnTypes)
                        .ToDictionary(info => info.Name, info => info.ReturnType);

                    _returnTypeHashes.Add(_hasher, nameToTypeMapping);
                }
            }
        }

        protected virtual IDictionary<string, Type> KnownPropertySpec
        {
            get { return _propertySpecification; }
            set { _propertySpecification = value; }
        }

        #region IDressedAs Members

        public virtual TInterface DressedAs<TInterface>(params Type[] otherInterfaces) where TInterface : class
        {
            return InvocationBinding.DressedAs<TInterface>(this, otherInterfaces);
        }

        #endregion

        #region IDynamicKnownInterfacesProjection Members

        IEnumerable<Type> IDynamicKnownInterfacesProjection.KnownInterfaces
        {
            set { KnownInterfaces = value; }
        }

        IDictionary<string, Type> IDynamicKnownInterfacesProjection.KnownPropertySpec
        {
            set { KnownPropertySpec = value; }
        }

        #endregion

        #region ISerializable Members

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }

        #endregion

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            var theHasher = HashForThisType();
            return theHasher == null
                       ? new String[] {}
                       : theHasher.Select(it => it.Key);
        }

        private IDictionary<string, Type> HashForThisType()
        {
            if (_propertySpecification != null)
                return _propertySpecification;
            IDictionary<string, Type> tOut;
            if (_hasher == null || !_returnTypeHashes.TryGetValue(_hasher, out tOut))
                return null;

            return tOut;
        }

        public virtual bool GetTypeForPropertyNameFromInterface(string name, out Type returnType)
        {
            var theHasher = HashForThisType();
            if (theHasher == null || !theHasher.TryGetValue(name, out returnType))
            {
                returnType = null;
                return false;
            }
            return true;
        }
    }
}