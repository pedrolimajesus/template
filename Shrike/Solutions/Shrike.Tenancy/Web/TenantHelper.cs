using System.ComponentModel;
using Shrike.Tenancy.DAL;

namespace Shrike.Tenancy.Web
{
    using System;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using System.Web.Security;

    using AppComponents;
    using AppComponents.Extensions.ExceptionEx;
    using AppComponents.Raven;

    using Lok.Unik.ModelCommon.Client;

    using ModelCommon.RavenDB;

    using Shrike.DAL.Manager;
    using Shrike.Tenancy.DAL.Constants;
    using Shrike.Tenancy.DAL.Managers;

    using log4net;

    /// <summary>
    /// Event arguments for tenancy creation
    /// </summary>
    public class TenantCreatedArgs : EventArgs
    {
        public TenantCreatedArgs(string site, string tenantName)
        {
            TenantName = tenantName;
            TenantDocumentDBSite = site;
        }

        /// <summary>
        /// Name of the tenant created.
        /// </summary>
        public string TenantName { get; private set; }


        /// <summary>
        /// name of the database allocated for tenant data
        /// </summary>
        public string TenantDocumentDBSite { get; private set; }
    }

    /// <summary>
    /// Class for creating and registering new tenant databases
    /// For multi-tenant application support.
    /// </summary>
    public static class TenantHelper
    {
        private static readonly ILog Log;

        private static readonly IApplicationAlert ApplicationAlert;

        public static event EventHandler<TenantCreatedArgs> TenantCreated;

        private static readonly object TenantCreatedObject = new object();

        private static void OnTenantCreated(string site, string tenantName)
        {
            if (TenantCreatedObject != null && TenantCreated != null)
            {
                TenantCreated(TenantCreatedObject, new TenantCreatedArgs(site, tenantName));
            }
        }

        static TenantHelper()
        {
            Log = ClassLogger.Create(typeof(TenantHelper));
            ApplicationAlert = Catalog.Factory.Resolve<IApplicationAlert>();
        }

        public static void RegisterTenancyRoute(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{*favicon}", new {favicon = @"(.*/)?favicon.ico(/.*)?"});
            routes.IgnoreRoute("Content/{*pathInfo}");
            routes.IgnoreRoute("bundles/{*pathInfo}");
            routes.IgnoreRoute("Bundles/{*pathInfo}");

            routes.MapRoute(
                TenantConstants.MapRouteDefaultName,
                TenantConstants.MapRouteTenantUrl,
                new
                {
                    tenant = DefaultRoles.SuperAdmin,
                    areaName = TenantConstants.AreaName,
                    controller = TenantConstants.MapRouteDefaultAccountController,
                    action = TenantConstants.MapRouteDefaultAction,
                    id = UrlParameter.Optional
                }).DataTokens["area"] = "UserManagementUI";
        }

        public static bool ValidateTenant(string currentTenant, FormsIdentity formsId)
        {
            //if null means that validation should not be done
            if (string.IsNullOrEmpty(currentTenant)) return true;

            //Get the tenant that the user is logged into
            //from the Forms Authentication Ticket
            var userTenant = formsId.Ticket.UserData;
            return userTenant.Trim().ToLower() == currentTenant.Trim().ToLower();
        }

        public static string GetCurrentTenantFormUrl(HttpContext context)
        {
            //Get the tenant that the user is logged into
            //from the Forms Authentication Ticket
            var route = RouteTable.Routes.GetRouteData(new HttpContextWrapper(context));
            if (route == null || route.Route.GetType().Name == TenantConstants.IgnoreRouteInternal)
            {
                return null;
            }

            //Get the current tenant specified in URL 
            if (!route.Values.ContainsKey(TenantConstants.TenantRouteName))
            {
                return null;
            }

            var currentTenant = route.GetRequiredString(TenantConstants.TenantRouteName);
            return currentTenant;
        }

        public static Tenant GetOrCreate(string tenancy)
        {
            Tenant tenant;
            try
            {
                var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Safe);
                var defaultDb = config[CommonConfiguration.DefaultDataDatabase];

                if (Catalog.Factory.CanResolve<ITenantCreationLicense>())
                {
                    var licenseChecker = Catalog.Factory.Resolve<ITenantCreationLicense>();
                    licenseChecker.CheckTenantCreationLicensed();
                }

                if (defaultDb.Equals(tenancy))
                {
                    throw new ApplicationException("A tenancy cannot use the same db name as the core db");
                }

                var ravenServer = config[CommonConfiguration.DefaultDataConnection];
                var dbName = tenancy;
                var user = config[CommonConfiguration.DefaultDataUser];
                var pass = config[CommonConfiguration.DefaultDataPassword];

                var site = String.Format(TenantConstants.RouteFormat, UnikContextTypes.UnikTenantContextResourceKind,
                                         tenancy);
                var id = String.Format(TenantConstants.TenantIdFormat, tenancy);

                tenant = new TenantManager().GetOrCreate(tenancy, site, id);
                if (tenant == null) return null;

                DocumentStoreLocator.AddRoute(new Uri(site), ravenServer, dbName, user, pass, tenancy);

                var warehouseRoute = String.Format(TenantConstants.RouteFormat,
                                                   UnikContextTypes.UnikWarehouseContextResourceKind, tenancy);

                DocumentStoreLocator.AddRoute(
                    new Uri(warehouseRoute), ravenServer, String.Format(TenantConstants.WarehouseDbFormatName, dbName),
                    user, pass, tenancy);

                //raise an event on tenant created
                //then the caller can create indexes or any other actions
                OnTenantCreated(site, tenancy);
            }
            catch (LicenseException lex)
            {
                Log.ErrorFormat("An exception occurred with the following message: {0}", lex.Message);
                ApplicationAlert.RaiseAlert(ApplicationAlertKind.System, lex.TraceInformation());

                // Pass along info that tenant creation failed due to license exception.

                throw;
            }
            catch (Exception exception)
            {
                Log.ErrorFormat("An exception occurred with the following message: {0}", exception.Message);
                ApplicationAlert.RaiseAlert(ApplicationAlertKind.System, exception.TraceInformation());
                return null;
            }

            return tenant;
        }
    }
}