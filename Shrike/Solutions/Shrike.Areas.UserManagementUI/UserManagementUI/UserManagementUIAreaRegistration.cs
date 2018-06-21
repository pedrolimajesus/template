using System.Web.Mvc;
using MvcContrib.PortableAreas;

namespace Shrike.Areas.UserManagementUI.UserManagementUI
{
    public class UserManagementUIAreaRegistration : PortableAreaRegistration
    {
        /// <summary>
        /// UserManagementUI
        /// </summary>
        public override string AreaName
        {
            get
            {
                return "UserManagementUI";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context, IApplicationBus bus)
        {   
            context.MapRoute(
                "UserManagementUI_default",
                "{tenant}/"+AreaName+"/{controller}/{action}/{id}",
                new { tenant = "SuperAdmin", controller = "Account", action = "Login", id = UrlParameter.Optional }
            );
            
            this.RegisterAreaEmbeddedResources();
        }
    }
}
