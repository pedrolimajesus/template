namespace Shrike.Tenancy.DAL.Constants
{
    public static class TenantConstants
    {
        public const string DefaultTenantRouteName = "TestTenancy";

        public const string WarehouseDbFormatName = "warehouse{0}";

        public const string TenantRouteName = "tenant";

        public const string MapRouteDefaultName = "Default";

        public const string MapRouteTenantUrl = "{tenant}/{areaName}/{controller}/{action}/{id}";

        public const string MapRouteDefaultAccountController = "Account";

        public const string MapRouteDefaultAction = "Login";

        public const string IgnoreRouteInternal = "IgnoreRouteInternal";

        public const string RouteFormat = "raven://{0}/{1}";

        public const string TenantIdFormat = "Tenants/{0}";

        /// <summary>
        /// UserManagementUI
        /// </summary>
        public const string AreaName = "UserManagementUI";
    }
}
