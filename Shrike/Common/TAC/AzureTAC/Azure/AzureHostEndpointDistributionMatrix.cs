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
using System.Linq;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace AppComponents.Azure
{
    /// <summary>
    ///   Creates a message distribution matrix, with each azure role being a recipient, by implementing the <see
    ///    cref="IDistributionMatrix" /> interface. Used with the <see cref="IBusBroadcaster" /> interface, this may be used to broadcast the same message to each azure role.
    /// </summary>
    public class AzureHostEndpointDistributionMatrix : IDistributionMatrix
    {
        private AzureHostEnvironment _he = new AzureHostEnvironment();

        #region IDistributionMatrix Members

        public IEnumerable<IPublisher> GetDistributionBusNames(string distributionScope)
        {
            var scopes = distributionScope.Split('/');
            var scope = scopes[0];
            var hostType = scopes[1];

            var ids = RoleEnvironment.Roles[hostType].Instances.Select(i => i.Id);
            foreach (var id in ids)
            {
                var hostId = _he.MakeIdentifier(scope, hostType, id);
                IPublisher p = Catalog.Preconfigure()
                    .Add(MessageBusLocalConfig.OptionalNamedMessageBus, scope)
                    .Add(HostEnvironmentLocalConfig.HostIdentifier, hostId)
                    .ConfiguredResolve<IPublisher>
                    (ScopeFactoryContexts.HostScope);
                yield return p;
            }
        }

        #endregion
    }
}