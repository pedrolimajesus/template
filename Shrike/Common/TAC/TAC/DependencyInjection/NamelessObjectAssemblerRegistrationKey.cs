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
    internal class NamelessObjectAssemblerRegistrationKey : IObjectAssemblerRegistrationKey
    {
        internal Type InstanceType;

        public NamelessObjectAssemblerRegistrationKey(Type type)
        {
            InstanceType = type;
        }

        #region IObjectAssemblerRegistrationKey Members

        public Type GetInstanceType()
        {
            return InstanceType;
        }

        public override bool Equals(object obj)
        {
            var r = obj as NamelessObjectAssemblerRegistrationKey;
            return (r != null) && ReferenceEquals(InstanceType, r.InstanceType);
        }

        public override int GetHashCode()
        {
            return InstanceType.GetHashCode();
        }

        #endregion
    }
}