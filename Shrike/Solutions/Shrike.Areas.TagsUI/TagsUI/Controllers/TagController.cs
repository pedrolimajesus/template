namespace Shrike.Areas.TagsUI.TagsUI.Controllers
{
    using System;
    using System.Web.Mvc;

    using AppComponents;
    using AppComponents.Web;
    using ExceptionHandling;
    using ExceptionHandling.Logic;
    using log4net;
    using Models;

    [HandleError]
    [Authorize]
    [NamedContext("context://ContextResourceKind/ApplicationRoot")]
    public class TagController : Controller
    {
        private readonly TagUILogic _tagUiLogic;
        private readonly ILog _log;

        public TagController()
        {
            _tagUiLogic = new TagUILogic();
            _log = ClassLogger.Create(this.GetType());
        }

        public ActionResult Delete(string id, string entity)
        {
            try
            {
                _tagUiLogic.DeleteTag(id, entity);
                return null;
            }
            catch (Exception exception)
            {
                var user = User as ApplicationUser;

                if (user != null)
                {
                    this._log.ErrorFormat("Current User: {0} - An exception occurred with the following message: {1}",
                                          user.UserName, exception.Message);
                }

                if (!ExceptionHandler.Manage(exception, this, Layer.UILogic))
                    return RedirectToAction("Error", "Errors");
            }

            return RedirectToAction("Error", "Errors");
        }

        public ActionResult AssignTag(string id, string entity, string validateQuantityTags, string validateCategoryTags)
        {
            var tagUi = new DataTagUi
                            {
                                Id = id,
                                Entity = entity
                            };
            try
            {

                ViewBag.QuantityTagsToValidate = validateQuantityTags;
                ViewBag.CategoryTagsToValidate = validateCategoryTags;

                var tagUiResponse = _tagUiLogic.NewSelectedTag(tagUi);
                return PartialView("AssignTag", tagUiResponse);
            }

            catch (Exception exception)
            {
                var user = User as ApplicationUser;

                if (user != null)
                {
                    _log.ErrorFormat("Current User: {0} - An exception occurred with the following message: {1}",
                                     user.UserName, exception.Message);
                }

                if (!ExceptionHandler.Manage(exception, this, Layer.UILogic))
                    return RedirectToAction("Error", "Errors");
            }
            var currentController = HttpContext.Request.RequestContext.RouteData.Values["controller"].ToString();
            return RedirectToAction("Index", currentController);

        }

        [HttpPost]
        public ActionResult SaveAssignTag(string id, string list, string name, string category, string type,
                                          string quantityTagsToValidate, string categoryTagsToValidate)
        {
            var user = User as ApplicationUser;

            try
            {
                var tagUi = new DataTagUi
                                {
                                    Id = id,
                                    Entity = type
                                };
                _tagUiLogic.SaveNewSelectedTag(tagUi, list, name, category, type, user);
            }
            catch (Exception exception)
            {

                if (user != null)
                {
                    _log.ErrorFormat("Current User: {0} - An exception occurred with the following message: {1}",
                                     user.UserName, exception.Message);
                }

                if (!ExceptionHandler.Manage(exception, this, Layer.UILogic))
                    return RedirectToAction("Error", "Errors");
            }

            return null;
        }

        [HttpPost]
        public ActionResult CreateTagCategory(string categoryName, string categoryColor)
        {
            if(!string.IsNullOrEmpty(categoryName) && !string.IsNullOrEmpty(categoryColor))
            {

            }

            return Json("An Error ocurred, please check the log file for more details.");
        }

    }
}

