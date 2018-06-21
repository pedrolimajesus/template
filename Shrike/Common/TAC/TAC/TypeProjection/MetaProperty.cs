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
using System.ComponentModel;
using Microsoft.CSharp.RuntimeBinder;

namespace AppComponents.Dynamic
{

    #region Classes

    public class MetaProperty : PropertyDescriptor
    {
        private readonly InvocationCacheCompatible _invokeGet;
        private readonly InvocationCacheCompatible _invokeSet;

        public MetaProperty(string name)
            : base(name, null)
        {
            _invokeGet = new InvocationCacheCompatible(InvocationKind.Get, name);
            _invokeSet = new InvocationCacheCompatible(InvocationKind.Set, name);
        }


        public override Type ComponentType
        {
            get { return typeof (object); }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get { return typeof (object); }
        }


        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override object GetValue(object component)
        {
            try
            {
                return _invokeGet.Invoke(component);
            }
            catch (RuntimeBinderException)
            {
                return null;
            }
        }

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object component, object value)
        {
            try
            {
                _invokeSet.Invoke(component, value);
            }
            catch (RuntimeBinderException)
            {
            }
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }
    }

    #endregion Classes
}