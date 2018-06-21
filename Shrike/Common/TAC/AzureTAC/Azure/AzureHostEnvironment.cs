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

using Microsoft.WindowsAzure.ServiceRuntime;

namespace AppComponents.Azure
{
    /// <summary>
    ///   Completes the <see cref="HostEnvironment" /> abstract base class to provide information about the current host environment, for the case that the host environment is an azure role.
    /// </summary>
    public class AzureHostEnvironment : HostEnvironment
    {
        public override string GetCurrentHostIdentifier(string scope)
        {
            return MakeIdentifier(scope, RoleEnvironment.CurrentRoleInstance.Role.Name,
                RoleEnvironment.CurrentRoleInstance.Id);
        }
    }
}