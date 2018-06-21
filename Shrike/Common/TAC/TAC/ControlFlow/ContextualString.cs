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
using System.Resources;
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents.ControlFlow
{
    public static class ContextualString
    {
        public static ResourceManager Resources
        {
            get
            {
                var appContext = ContextRegistry.ContextsOf("ApplicationType").First();
                //var cultureContext = ContextRegistry.ContextsOf("Culture").First();

                var firstSegment = appContext.Segments.Second().TrimEnd('/');
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !a.FullName.StartsWith("System.") && !a.FullName.StartsWith("Microsoft.") && !a.FullName.StartsWith("DotNet"));

                var appTypeResourceAssembly = assemblies.FirstOrDefault(
                    ass => ass.GetManifestResourceNames().Contains(firstSegment)
                    );
                if (appTypeResourceAssembly==null) return null;
                var rm = new ResourceManager(appContext.Segments.Third(), appTypeResourceAssembly);
                return rm;
            }
        }

        public static string Get(string id)
        {
            var rm = Resources;
            return rm == null ? id : rm.GetString(id);
        }

        public static string Merge(string id, params object[] args)
        {
            return string.Format(Get(id), args);
        }
    }
}