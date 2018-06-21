using System.Linq;
using System.Web.Security;
using AppComponents.Web;


namespace Shrike.UserManagement.BusinessLogic.Business
{
    using DAL.Manager;

    using Lok.Unik.ModelCommon.Client;

    public class NavigationBusinessLogic
    {
        private NavigationManager _mgr;
        

        public NavigationBusinessLogic()
        {
            _mgr = new NavigationManager();
            
        }

        public Navigation GetNavigationByCurrentUser(ApplicationUser user)
        {
            if (user == null) return new Navigation();

            var role = GetRoleByUser(user);
            var navigationItems = _mgr.GetNavigation(role);
            return navigationItems;
        }

        private static string GetRoleByUser(ApplicationUser user)
        {
            var currentRole = string.Empty;

            if (user != null)
            {
                var roleId = user.AccountRoles.FirstOrDefault(ac => !string.IsNullOrEmpty(ac));
                var roleMgr = new RoleManager();
                currentRole = roleMgr.GetRoleNameById(roleId, Roles.ApplicationName);
            }

            return currentRole;
        }
    }
}
