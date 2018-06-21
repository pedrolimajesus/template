using System.Web.Mvc;
using MvcContrib.PortableAreas;

namespace Shrike.Areas.ErrorManagementUI.ErrorManagementUI
{
    public class ErrorManagementUIAreaRegistration : PortableAreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "ErrorManagementUI";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context, IApplicationBus bus)
        {
            context.MapRoute(
                "ErrorManagementUI_default",
                "{tenant}/" + AreaName + "/{controller}/{action}/{id}",
                new { tenant = "SuperAdmin", controller = "Errors", Error = "Error", id = UrlParameter.Optional }
            );

            this.RegisterAreaEmbeddedResources();
        }
    }
}


