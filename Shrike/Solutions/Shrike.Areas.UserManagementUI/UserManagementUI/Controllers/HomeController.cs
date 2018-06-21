using System.Web.Mvc;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Controllers
{
    //[Authorize(Roles = RoleFlags.AllowAll)]
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

    }
}
