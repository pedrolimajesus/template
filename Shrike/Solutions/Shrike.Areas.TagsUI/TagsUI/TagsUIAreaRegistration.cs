using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcContrib.PortableAreas;

namespace Shrike.Areas.TagsUI.TagsUI
{
    public class TagsUIAreaRegistration : PortableAreaRegistration
    {
        /// <summary>
        /// TagsUI
        /// </summary>
        public override string AreaName
        {
            get
            {
                return "TagsUI";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context, IApplicationBus bus)
        {
            context.MapRoute(
                "TagsUI_default",
                "{tenant}/"+AreaName+"/{controller}/{action}/{id}",
                new { tenant = "SuperAdmin", controller = "Account", action = "Login", id = UrlParameter.Optional }
            );

            this.RegisterAreaEmbeddedResources();
        }
    }
}