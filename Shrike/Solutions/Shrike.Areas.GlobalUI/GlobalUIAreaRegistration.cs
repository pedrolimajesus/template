﻿using System.Web.Mvc;
using MvcContrib.PortableAreas;

namespace Shrike.Areas.GlobalUI.GlobalUI
{
    public class GlobalUIAreaRegistration : PortableAreaRegistration {
        
        /// <summary>
        /// GlobalUI
        /// </summary>
        public override string AreaName {
            get {
                return "GlobalUI";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context, IApplicationBus bus)
        {
            context.MapRoute(
                "GlobalUI_default",
                "{tenant}/"+AreaName+"/{controller}/{action}/{id}",
                new { tenant = "SystemOwner", controller = "Account", action = "Login", id = UrlParameter.Optional }
            );
            
            this.RegisterAreaEmbeddedResources();
        }

    }
}
