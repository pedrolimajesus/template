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

using System.Linq;
using AppComponents;
using AppComponents.Raven;
using Raven.Client.Indexes;

namespace ApplicationComponents.Raven
{
    public class CoreEntitiesTag : AbstractIndexCreationGroupAssemblyTag
    {
    };

    public class AuthorizationCode_ByPrincipal : AbstractIndexCreationTask<AuthorizationCode>
    {
        public AuthorizationCode_ByPrincipal()
        {
            Map = acr => from ac in acr select new {ac.Principal};
        }
    }

    public class AuthorizationCode_ByEmailedTo : AbstractIndexCreationTask<AuthorizationCode>
    {
        public AuthorizationCode_ByEmailedTo()
        {
            Map = acr => from ac in acr select new {ac.EmailedTo};
        }
    }

    public class ApplicationPrincipal_ByTenancy : AbstractIndexCreationTask<ApplicationPrincipal>
    {
        public ApplicationPrincipal_ByTenancy()
        {
            Map = principals => from p in principals select new {p.Tenancy};
        }
    }
}