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
using System.Security;
using System.Security.Authentication;
using AppComponents.ControlFlow;
using AppComponents.Raven;

namespace AppComponents.Web.ControlFlow
{
    public enum ContextualAuthorizationConfiguration
    {
        PrincipalsStore
    }


    public static class ContextualAuthorization
    {
        private static readonly ICachedData<string, ApplicationPrincipal> _cachedPrincipals;


        static ContextualAuthorization()
        {
            _cachedPrincipals = Catalog.Preconfigure()
                .Add(CachedDataLocalConfig.OptionalMaximumCacheSize, 5000)
                .Add(CachedDataLocalConfig.OptionalGroomExpiredData, true)
                .Add(CachedDataLocalConfig.OptionalDefaultExpirationTimeSeconds, 600)
                .Add(CachedDataLocalConfig.OptionalCacheHitRenewsExpiration, true)
                .ConfiguredCreate(() => new InMemoryCachedData<string, ApplicationPrincipal>());
        }

        public static void AuthorizeForRole(string role)
        {
            var pctx = ContextRegistry.ContextsOf("Principal").FirstOrDefault();
            if (null == pctx)
                throw new AuthenticationException("No authenticated identity");

            var id = pctx.Segments.First();

            ApplicationPrincipal pr;
            if (!_cachedPrincipals.MaybeGetItem(id, out pr))
            {
                using (
                    var dc =
                        DocumentStoreLocator.ResolveOrRoot(
                            ContextualAuthorizationConfiguration.PrincipalsStore))
                {
                    pr = dc.Load<ApplicationPrincipal>(id);
                    _cachedPrincipals.Add(id, pr);
                }
            }

            if (!pr.IsInRole(role))
                throw new SecurityException(string.Format("principal {0} is not in role {1}", id, role));
        }
    }
}