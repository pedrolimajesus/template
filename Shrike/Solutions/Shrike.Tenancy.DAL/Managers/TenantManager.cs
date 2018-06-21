namespace Shrike.Tenancy.DAL.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using AppComponents.ControlFlow;
    using AppComponents.Raven;

    using Lok.Unik.ModelCommon.Client;

    using AppComponents;

    using Shrike.ExceptionHandling;
    using Shrike.ExceptionHandling.Logic;
    using Shrike.Tenancy.DAL.Constants;

    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class TenantManager
    {
        public static string CurrentTenancy
        {
            get
            {
                try
                {
                    string tenancy = GetTenancy();

                    if (string.IsNullOrEmpty(tenancy))
                    {
                        tenancy = TenantConstants.DefaultTenantRouteName;
                    }

                    return tenancy;
                }
                catch (Exception ex)
                {
                    ExceptionHandler.Manage(ex, new TenantManager(), Layer.DataAccess);
                    return string.Empty;
                }
            }
        }

        private static string GetTenancy()
        {
            string tenancy = null;

            var tenancyUris = ContextRegistry.ContextsOf("Tenancy");
            if (tenancyUris.Any())
            {
                var tenancyUri = tenancyUris.First();
                tenancy = tenancyUri.Segments.Count() > 1 ? tenancyUri.Segments[1] : string.Empty;
            }
            else
            {
                var context = HttpContext.Current;
                if (context != null
                    && context.Request.RequestContext.RouteData.Values.ContainsKey(TenantConstants.TenantRouteName))
                {
                    tenancy = context.Request.RequestContext.RouteData.GetRequiredString(
                        TenantConstants.TenantRouteName);
                }
            }

            return tenancy;
        }

        public IEnumerable<Tenant> GetAllTenants()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q = session.Query<Tenant>();
                return q.ToArray();
            }
        }

        public Tenant GetOrCreate(string tenancy, string site, string id)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var tenant = session.Load<Tenant>(id);

                //if tenant exist do not create new tenant
                if (tenant == null)
                {
                    tenant = new Tenant
                        { Id = id, Name = tenancy, Site = site.ToLowerInvariant(), Timestamp = DateTime.UtcNow };
                    session.Store(tenant);
                    session.SaveChanges();
                }

                return tenant;
            }
        }

        public static IDictionary<string, string> GetAll()
        {
            var tenantList = new Dictionary<string, string>();

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var query = session.Query<Tenant>().ToList();

                foreach (var tenant in query) tenantList.Add(tenant.Id, tenant.Name);
            }

            return tenantList;
        }
    }
}