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
using System.Linq;
using System.Runtime.Serialization;

namespace AppComponents.Dynamic
{

    #region Classes

    [Serializable]
    public class DressAsProxySerializer : IObjectReference
    {
        public Type Context;
        public Type[] Interfaces;
        public string[] MonoInterfaces;
        public object Original;

        #region IObjectReference Members

        public object GetRealObject(StreamingContext context)
        {
            var interfaces = Interfaces ?? MonoInterfaces.Select(it => Type.GetType(it)).ToArray();
            var type = BuildProxy.BuildType(Context, interfaces.First(), interfaces.Skip(1).ToArray());
            return InvocationBinding.InitializeProxy(type, Original, interfaces);
        }

        #endregion
    }

    #endregion Classes
}