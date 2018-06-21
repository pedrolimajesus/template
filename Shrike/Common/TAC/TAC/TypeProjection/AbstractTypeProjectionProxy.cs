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
using System.Runtime.Serialization;

namespace AppComponents.Dynamic
{

    #region Interfaces

    public interface IDressAsProxyInitializer : ITypeProjectionProxy
    {
        void Initialize(dynamic original, IEnumerable<Type> interfaces = null,
                        IDictionary<string, Type> informalInterface = null);
    }

    #endregion Interfaces

    #region Classes

    [Serializable]
    public abstract class AbstractTypeProjectionProxy : IDressAsProxyInitializer, ISerializable
    {
        private bool _initialized;

        #region IDressAsProxyInitializer Members

        public dynamic OriginalTarget { get; private set; }

        public virtual void Initialize(dynamic original, IEnumerable<Type> interfaces = null,
                                       IDictionary<string, Type> informalInterface = null)
        {
            if (original == null)
                throw new ArgumentNullException("original", "Can't proxy a Null value");

            if (_initialized)
                throw new MethodAccessException("Cannot initialize again.");

            _initialized = true;
            OriginalTarget = original;
            var knownOriginal = OriginalTarget as IDynamicKnownInterfacesProjection;
            if (knownOriginal != null)
            {
                if (interfaces != null)
                    knownOriginal.KnownInterfaces = interfaces;
                if (informalInterface != null)
                    knownOriginal.KnownPropertySpec = informalInterface;
            }
        }

        #endregion

        #region ISerializable Members

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof (DressAsProxySerializer));

            var tCustomAttr =
                GetType().GetCustomAttributes(typeof (DressedAsAttribute), false).OfType<DressedAsAttribute>().
                    FirstOrDefault();

            info.AddValue("Context",
                          tCustomAttr == null
                              ? null
                              : tCustomAttr.Context, typeof (Type));

            if (TypeFactorization.IsMonoRuntimeEnvironment)
            {
                info.AddValue("MonoInterfaces",
                              tCustomAttr == null
                                  ? null
                                  : tCustomAttr.Interfaces.Select(it => it.AssemblyQualifiedName).ToArray(),
                              typeof (string[]));
            }
            else
            {
                info.AddValue("Interfaces",
                              tCustomAttr == null
                                  ? null
                                  : tCustomAttr.Interfaces, typeof (Type[]));
            }

            info.AddValue("Original", OriginalTarget);
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (ReferenceEquals(OriginalTarget, obj))
                return true;
            if (!(obj is AbstractTypeProjectionProxy))
                return OriginalTarget.Equals(obj);
            return Equals((AbstractTypeProjectionProxy) obj);
        }

        public bool Equals(AbstractTypeProjectionProxy other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(OriginalTarget, other.OriginalTarget))
                return true;
            return Equals(other.OriginalTarget, OriginalTarget);
        }

        public override int GetHashCode()
        {
            return OriginalTarget.GetHashCode();
        }

        public override string ToString()
        {
            return OriginalTarget.ToString();
        }
    }

    #endregion Classes
}