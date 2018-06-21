namespace Shrike.UserManagement.BusinessLogic.Business
{
    using Shrike.DAL.Manager;

    public static class RoleBusinessLogic
    {
        public static void CreateRolesAndNavigationInfo()
        {
            RoleManager.CreateRolesAndNavigationInfo();
        }
    }
}
