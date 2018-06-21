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

namespace AppComponents
{
    internal class NamedObjectAssemblerRegistrationKey : IObjectAssemblerRegistrationKey
    {
        internal Type _instanceType;
        internal string _name;

        public NamedObjectAssemblerRegistrationKey(string name, Type type)
        {
            _name = name ?? String.Empty;
            _instanceType = type;
        }

        #region IObjectAssemblerRegistrationKey Members

        public Type GetInstanceType()
        {
            return _instanceType;
        }


        public override bool Equals(object obj)
        {
            var r = obj as NamedObjectAssemblerRegistrationKey;
            return (r != null) &&
                   ReferenceEquals(_instanceType, r._instanceType) &&
                   String.Compare(_name, r._name, true) == 0; // ignore case
        }

        public override int GetHashCode()
        {
            return Hash.GetCombinedHashCodeForHashes(_name.GetHashCode(), _instanceType.GetHashCode());
        }

        #endregion

        public override string ToString()
        {
            return string.Format("InstanceType: {0}, Name: {1}", _instanceType, _name);
        }
    }
}