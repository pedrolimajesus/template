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
using System.Web;

using AppComponents.Extensions.EnumerableEx;

namespace AppComponents.ControlFlow
{
    using AppComponents.Web;

    public class WebPrincipalContextProvider : IContextProvider
    {
        private readonly PrincipalContextProvider principalContextProvider = new PrincipalContextProvider();

        #region IContextProvider Members

        public IEnumerable<Uri> ProvideContexts()
        {
            var ctx = HttpContext.Current;
            if (null != ctx)
            {
                var user = (ApplicationUser)HttpContext.Current.Items["ApplicationUser"];
                if (null != user)
                {
                    return EnumerableEx.OfOne(new Uri(string.Format("context://Principal/{0}", user.PrincipalId)));
                }
            }

            return this.principalContextProvider.ProvideContexts();
        }

        #endregion
    }
}