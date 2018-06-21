using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using AppComponents.Web;

namespace Shrike
{
    using Shrike.Tenancy.Web;

    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            TenantHelper.RegisterTenancyRoute(routes);
        }
    }
}