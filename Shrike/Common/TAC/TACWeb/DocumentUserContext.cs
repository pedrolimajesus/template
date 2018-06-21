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
using System.Linq;
using System.Web.Security;
using AppComponents.Raven;

namespace AppComponents.Web
{
    public class UserNotFoundException : ApplicationException
    {
        public UserNotFoundException()
        {
        }

        public UserNotFoundException(string msg)
            : base(msg)
        {
        }
    }

    public class DocumentUserContext : IUserContext
    {
        #region IUserContext Members

        public ApplicationUser GetCurrentUser()
        {
            MembershipUser mu = Membership.GetUser();
            if (null == mu)
            {
                throw new UserNotFoundException("No current user logged in");
            }

            ApplicationUser au = FetchPrincipal((string)mu.ProviderUserKey);

            if (null == au)
            {
                var explanation = string.Format("user {0} is not registered in the application membership rolls",
                                                mu.UserName);
                throw new UnauthorizedAccessException(explanation);
            }

            return au;
        }

        public ApplicationUser LocateUser(string principal)
        {
            ApplicationUser foundUser = null;
            var au = FetchPrincipal(principal);

            if (null == au)
            {
                foundUser = new ApplicationUser
                                {
                                    PrincipalId = principal,
                                    AccountRoles = new List<string>(),
                                    Enabled = true
                                };
            }

            return foundUser;
        }

        public IEnumerable<string> AccountTypesFor(string principal)
        {
            var au = LocateUser(principal);
            return au.Enabled ? au.AccountRoles : Enumerable.Empty<string>();
        }

        #endregion

        private static ApplicationUser FetchPrincipal(string principal)
        {
            ApplicationUser au = null;
            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                au = ds.Load<ApplicationUser>(principal);
            }
            return au;
        }
    }
}