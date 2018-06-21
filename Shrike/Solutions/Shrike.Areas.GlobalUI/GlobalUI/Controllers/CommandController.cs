using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using AppComponents.Web;
using Lok.Unik.ModelCommon.Client;
using Shrike.UserManagement.BusinessLogic.Business;

namespace Shrike.Areas.GlobalUI.GlobalUI.Controllers
{
    [Authorize]
    public class CommandController : Controller
    {
        //
        // GET: /MenuRight/
        public ActionResult Index(string controller, string showoptions = "true")
        {
            var user = User as ApplicationUser;
            if(user != null)
            {
                ViewBag.RoleUser = Roles.GetRolesForUser(user.UserName).First();
                ViewBag.ListCommandData = GetViewCommands(user, controller);
            }
            ViewBag.ShowOptions = showoptions;
            return PartialView();
        }

        private static List<ViewCommand> GetViewCommands(ApplicationUser user, string controller)
        {
            var actions = new List<ViewCommand>();
            var actionsSelect = new List<ViewCommand>();
            var navigationData = new NavigationBusinessLogic().GetNavigationByCurrentUser(user);
            var dataSettingUI = navigationData.NavigationItems;
            foreach (var dataUi in dataSettingUI.Where(setting => setting.ViewItems.Any(x => x.ControllerName.Equals(controller))))
            {
                var viewItem = dataUi.ViewItems.First(view => view.ControllerName.Equals(controller));

                actions = viewItem.ViewCommands;
                break;
            }
            actionsSelect.AddRange(actions);

            return actionsSelect;
        }
    }
}
