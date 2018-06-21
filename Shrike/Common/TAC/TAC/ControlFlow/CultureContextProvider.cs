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
using System.Threading;
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents.ControlFlow
{
    public class CultureContextProvider : IContextProvider
    {
        #region IContextProvider Members

        public IEnumerable<Uri> ProvideContexts()
        {
            return EnumerableEx.OfOne(
                new Uri(string.Format("context://Culture/{0}/{1}", Thread.CurrentThread.CurrentCulture,
                                      Thread.CurrentThread.CurrentUICulture)));
        }

        #endregion
    }
}