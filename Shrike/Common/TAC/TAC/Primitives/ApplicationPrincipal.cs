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

using System.Collections.Generic;
using System.Security.Principal;

namespace AppComponents
{
    using System;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    public class ApplicationIdentity : IIdentity
    {
        public ApplicationIdentity(string principalId)
        {
            Name = principalId;
            AuthenticationType = "Default";
            //IsAuthenticated = true;
        }

        #region Implementation of IIdentity

        public string Name { get; private set; }

        public string AuthenticationType { get; private set; }

        public bool IsAuthenticated
        {
            get
            {
                return !string.IsNullOrEmpty(this.Name);
            }
        }

        #endregion
    }

    public class ApplicationPrincipal : IPrincipal
    {
        [JsonIgnore]
        [XmlIgnore]
        [NonSerialized]
        private IIdentity _identity;

        public ApplicationPrincipal()
        {
            AccountRoles = new List<string>();
        }

        public string Id
        {
            get
            {
                return PrincipalId;
            }
            set
            {
                PrincipalId = value;
            }
        }

        [DocumentIdentifier]
        public string PrincipalId { get; set; }

        public List<string> AccountRoles { get; set; }
        public bool Enabled { get; set; }

        public string Tenancy { get; set; }

        #region IPrincipal Members

        [JsonIgnore]
        [XmlIgnore]
        public IIdentity Identity
        {
            get { return _identity ?? (_identity = new ApplicationIdentity(PrincipalId)); }
        }

        public bool IsInRole(string roleId)
        {
            var retVal = AccountRoles.Contains(roleId) || AccountRoles.Contains(roleId.ToLowerInvariant());
            return retVal;
        }

        #endregion
    }
}