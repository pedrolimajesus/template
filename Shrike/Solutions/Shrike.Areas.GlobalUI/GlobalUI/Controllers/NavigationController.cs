using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Shrike.UserManagement.BusinessLogic.Business;
using AppComponents.Web;

namespace Shrike.Areas.GlobalUI.GlobalUI.Controllers
{
    [Authorize]
    public class NavigationController : Controller
    {
        //
        // GET: /MenuFooter/

        public ActionResult Index(string controller)
        {
            var user = User as ApplicationUser;
            var navigationItem = new NavigationBusinessLogic().GetNavigationByCurrentUser(user);

            if (navigationItem != null)
            {
                ViewBag.RoleItem = navigationItem;
                var enumerable = navigationItem.NavigationItems;
                ViewBag.DataSettingUI = enumerable.Count();
                ViewBag.CurrentController = controller;
                return PartialView();
            }

            return new ContentResult();
        }
    }
}