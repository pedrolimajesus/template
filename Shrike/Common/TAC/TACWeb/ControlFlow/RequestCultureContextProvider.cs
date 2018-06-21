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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents.ControlFlow
{
    public class RequestCultureContextProvider : IContextProvider
    {
        private CultureContextProvider _cultureContext = new CultureContextProvider();

        #region IContextProvider Members

        public IEnumerable<Uri> ProvideContexts()
        {
            return Enumerable.Empty<Uri>();

            var ctx = HttpContext.Current;

            if (null != ctx && ctx.Request.UserLanguages.EmptyIfNull().Any())
            {
                var culture = ctx.Request.UserLanguages.First();
                var ci = CultureInfo.CreateSpecificCulture(culture);
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
            }

            return _cultureContext.ProvideContexts();
        }

        #endregion
    }
}