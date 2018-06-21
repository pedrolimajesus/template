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

using AppComponents.Extensions.EnumerableEx;
using AppComponents.Raven;
using log4net;

namespace AppComponents.ControlFlow
{
    using System.Web;

    public enum PrincipalTenancyContextProviderConfiguration
    {
        PrincipalContextFactoryKey,

        TenancyContextPrincipalsStore
    }

    public class PrincipalTenancyContextProvider<TPrincipalType> : IContextProvider
        where TPrincipalType : ApplicationPrincipal
    {
        private readonly ILog _log;

        private readonly ICachedData<string, string> cachedTenancies;

        private readonly IContextProvider principalContextProvider;

        public PrincipalTenancyContextProvider()
        {
            try
            {
                _log = ClassLogger.Create(this.GetType());
                this.principalContextProvider =
                    (Catalog.Factory.Resolve<IContextProvider>(
                        PrincipalTenancyContextProviderConfiguration.PrincipalContextFactoryKey)
                     ?? Catalog.Factory.Resolve<IContextProvider>("TenancyContext"))
                    ?? Catalog.Factory.Resolve<IContextProvider>();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    _log.Error(ex.ToString() + " \n InnerException: " + ex.InnerException.ToString());
                else
                    _log.Error(ex.ToString());
                Console.WriteLine(ex.ToString());
            }

            this.cachedTenancies =
                Catalog.Preconfigure().Add(CachedDataLocalConfig.OptionalMaximumCacheSize, 5000).Add(
                    CachedDataLocalConfig.OptionalGroomExpiredData, true).Add(
                        CachedDataLocalConfig.OptionalDefaultExpirationTimeSeconds, 600).Add(
                            CachedDataLocalConfig.OptionalCacheHitRenewsExpiration, true).ConfiguredCreate(
                                () => new InMemoryCachedData<string, string>());
        }

        #region IContextProvider Members

        public IEnumerable<Uri> ProvideContexts()
        {
            var principalContext = this.principalContextProvider.ProvideContexts().SingleOrDefault();
            if (null != principalContext)
            {
                var principalName = principalContext.Segments.Count() > 2
                                        ? principalContext.Segments.Second() + principalContext.Segments.Third()
                                        : principalContext.Segments.Any() ? principalContext.Segments.First() : "/";

                string tenancy;

                if (!this.cachedTenancies.MaybeGetItem(principalName, out tenancy))
                {
                    using (
                        var dc =
                            DocumentStoreLocator.ResolveOrRoot(
                                PrincipalTenancyContextProviderConfiguration.TenancyContextPrincipalsStore))
                    {
                        var p = dc.Load<TPrincipalType>(principalName);
                        if (null != p && !string.IsNullOrEmpty(p.Tenancy))
                        {
                            tenancy = p.Tenancy;
                            this.cachedTenancies.Add(principalName, tenancy);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(tenancy))
                {
                    return EnumerableEx.OfOne(new Uri(string.Format("context://Tenancy/{0}", tenancy)));
                }
            }

            return Enumerable.Empty<Uri>();
        }

        #endregion
    }
}