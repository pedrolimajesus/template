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

namespace AppComponents.InstanceFactories
{
    public class ThreadLocalStorageInstanceFactory : IInstanceCreationStrategy
    {
        [ThreadStatic] private static Dictionary<string, object> _localStorage;

        #region IInstanceCreationStrategy Members

        public object ActivateInstance(IObjectAssemblySpecification creator)
        {
            object instance = null;


            if (_localStorage == null)
                _localStorage = new Dictionary<string, object>();

            if (!_localStorage.TryGetValue(creator.Key, out instance))
            {
                instance = creator.CreateInstance();
                _localStorage[creator.Key] = instance;
            }

            return instance;
        }


        public void FlushCache(IObjectAssemblySpecification registration)
        {
            if (_localStorage == null)
                return;

            _localStorage.Remove(registration.Key);
        }

        #endregion
    }
}