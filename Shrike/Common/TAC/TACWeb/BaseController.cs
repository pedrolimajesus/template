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

using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using AppComponents.ControlFlow;

namespace AppComponents.Web
{
    public enum WebLocalization
    {
        DefaultCulture
    }

    public class BaseController : Controller
    {
        protected override void ExecuteCore()
        {
            ExtractCurrentContext(Request);
            base.ExecuteCore();
        }

        private static void ExtractCurrentContext(HttpRequestBase request)
        {
            string cultureName = null;
            var cf = Catalog.Factory.Resolve<IConfig>();

            // Attempt to read the culture cookie from Request
            var cultureCookie = request.Cookies["_culture"];
            if (cultureCookie != null)
                cultureName = cultureCookie.Value;
            else if (request.UserLanguages != null)
                cultureName = request.UserLanguages[0]; // obtain it from HTTP header AcceptLanguages
            else cultureName = cf.Get(WebLocalization.DefaultCulture, "en-US");

            // Validate culture name
            var isSupported = false;
            var rm = ContextualString.Resources;
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            var culture = cultures.FirstOrDefault(c => c.Name == cultureName);

            if (null != culture)
            {
                var rs = rm.GetResourceSet(culture, true, false);
                isSupported = rs != null;


                if (!isSupported)
                {
                    var ci = new CultureInfo(culture.TwoLetterISOLanguageName);
                    rs = rm.GetResourceSet(ci, true, false);
                    isSupported = rs != null;
                    if (isSupported)
                        cultureName = ci.Name;
                }
            }


            if (!isSupported)
            {
                cultureName = cf.Get(WebLocalization.DefaultCulture, "en-US");
            }


            // Modify current thread's culture            
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureName);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(cultureName);
        }
    }
}