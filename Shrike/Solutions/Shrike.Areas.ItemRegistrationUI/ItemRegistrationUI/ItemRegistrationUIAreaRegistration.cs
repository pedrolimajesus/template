using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcContrib.PortableAreas;

namespace Shrike.Areas.ItemRegistrationUI.ItemRegistrationUI
{
    public class ItemRegistrationUIAreaRegistration : PortableAreaRegistration
    {
        /// <summary>
        /// ItemRegistrationUI
        /// </summary>
        public override string AreaName
        {
            get
            {
                return "ItemRegistrationUI";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context, IApplicationBus bus)
        {
            context.MapRoute(
                "ItemRegistrationUI_default",
                "{tenant}/"+AreaName+"/{controller}/{action}/{id}",
                new { tenant = "SuperAdmin", controller = "Account", action = "Login", id = UrlParameter.Optional }
            );

            this.RegisterAreaEmbeddedResources();
        }
    }
}