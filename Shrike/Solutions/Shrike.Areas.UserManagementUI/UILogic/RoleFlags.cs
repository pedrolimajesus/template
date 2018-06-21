namespace Shrike.Areas.UserManagementUI.UILogic
{
    public static class RoleFlags
    {
        public const string OnlySuperAdmin = "authorization/roles/SuperAdmin";

        public const string OwnerAndTenantOwners = "authorization/roles/SuperAdmin, authorization/roles/TenantOwner";

        public const string TenantOwner = "authorization/roles/TenantOwner";

        /// <summary>
        /// Role from MultiContext Shrike or another.
        /// </summary>
        public const string MultiContext = "authorization/roles/{0}";

    }
}