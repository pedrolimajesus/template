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

using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Web;
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents.Web
{
    public static class WebPermitRoles
    {
        public static void Check(params string[] roles)
        {
            Debug.Assert(roles.EmptyIfNull().Any());

            var log = ClassLogger.Create(typeof (WebPermitRoles));
            var dblog = DebugOnlyLogger.Create(log);

            var context = HttpContext.Current;
            var user = context.User;

            foreach (string role in roles)
            {
                if (user.IsInRole(role))
                {
                    dblog.InfoFormat("{0} granted access through role {1}", user.Identity.Name, role);
                    return;
                }
            }

            log.ErrorFormat("{0} is not in any role {1}, security exception", user.Identity.Name,
                            string.Join(",", roles));
            throw new SecurityException(string.Format("user {0} does not have role required for action.",
                                                      user.Identity.Name));
        }
    }
}