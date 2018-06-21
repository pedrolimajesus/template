using System;
using System.Linq;
using System.Web.Mvc;
using log4net;

namespace Shrike.Areas.TagsUI.TagsUI.Controllers
{
    using AppComponents;
    using AppComponents.Web;
    using ExceptionHandling;
    using ExceptionHandling.Logic;
    using Models;

    public class GroupLinkController : Controller
    {
        private readonly GroupLinkUILogic _groupLinkUILogic;
        private readonly ILog _log;

        public GroupLinkController()
        {
            _groupLinkUILogic = new GroupLinkUILogic();
            _log = ClassLogger.Create(this.GetType());
        }

        public ActionResult Index()
        {
            return Json(_groupLinkUILogic.GetAllGroupLinks(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult AssignGroup(string type, string leftText, string rightText)
        {
            if (string.IsNullOrEmpty(type)) return Json(false, JsonRequestBehavior.AllowGet);

            var group = _groupLinkUILogic.GetAvailableGroup(type, leftText, rightText);
            if (group == null || !_groupLinkUILogic.ExistGroups(group))
                return Json(false, JsonRequestBehavior.AllowGet);
            
            return PartialView(group);
        }

        [HttpPost]
        public ActionResult AssignGroup(GroupLink groupLink, string groupOne, string groupTwo, string type)
        {
            var user = User as ApplicationUser;
            try
            {
                _groupLinkUILogic
                    .AddToLinkEntities(groupLink, groupOne, groupTwo, user.PrincipalId, type);
            }

            catch (Exception exception)
            {
                _log.ErrorFormat("Current User: {0} - An exception occurred with the following message: {1}",
             user.UserName, exception.Message);

                if (!ExceptionHandler.Manage(exception, this, Layer.UILogic))
                    return RedirectToAction("Error", "Errors");
            }

            return PartialView(groupLink);
        }

        [HttpGet]
        public ActionResult RemoveGroup(string type)
        {
            var allGroups = _groupLinkUILogic.GetAllGroupLinks(type);

            if (string.IsNullOrEmpty(type)
                || !allGroups.Any())
                return Json(false, JsonRequestBehavior.AllowGet);

            return PartialView(allGroups);
        }

        [HttpPost, ActionName("RemoveGroup")]
        public ActionResult RemoveGroupPost(string groupId)
        {
            try
            {
                _groupLinkUILogic.RemoveGroupLink(groupId);
            }

            catch (Exception exception)
            {
                var user = User as ApplicationUser;

                _log.ErrorFormat("Current User: {0} - An exception occurred with the following message: {1}",
               user.UserName, exception.Message);

                if (!ExceptionHandler.Manage(exception, this, Layer.UILogic))
                    return RedirectToAction("Error", "Errors");
            }

            return PartialView();
        }
    }
}
