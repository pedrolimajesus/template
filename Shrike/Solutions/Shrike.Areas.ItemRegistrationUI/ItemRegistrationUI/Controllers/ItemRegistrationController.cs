using System.Text.RegularExpressions;
using Shrike.Areas.UserManagementUI.UILogic;
using Shrike.Tenancy.Web;

namespace Shrike.Areas.ItemRegistrationUI.ItemRegistrationUI.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;

    using AppComponents;
    using AppComponents.Extensions.ExceptionEx;
    using AppComponents.Web;

    using ExceptionHandling;
    using ExceptionHandling.Logic;

    using Lok.Unik.ModelCommon.Client;
    using log4net;

    using TagsUI.TagsUI;
    using TagsUI.TagsUI.Models;
    using Lok.Unik.ModelCommon.Aware;

    [HandleError]
    [ContextAuthorize("ItemRegistration")]
    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class ItemRegistrationController : Controller
    {

        private readonly ItemRegistrationUILogic _itemRegistrationUILogic;

        private readonly ILog _log;
        private readonly IApplicationAlert _applicationAlert;
        private readonly IEnumerable<string> _command;

        public ItemRegistrationController()
        {
            _itemRegistrationUILogic = new ItemRegistrationUILogic();

            _log = ClassLogger.Create(GetType());
            _applicationAlert = Catalog.Factory.Resolve<IApplicationAlert>();
            _command = new List<string>
                {
                    "Add",
                    "Delete",
                    "Assign Tag",
                    "Clear Tag"
                };
        }

        //
        // GET: /ItemRegistration/
        // Here the model is the entity itself ItemRegistration
        public ActionResult Index(string criteria = null)
        {
            try
            {
                ViewBag.ItemsEnable = _command;

                ViewBag.id = Request["id"];

                var time = Request["kind"];
                var timeCriteria = criteria ?? time;

                ViewBag.criteria = timeCriteria;

                ViewBag.TagFilters = new TagUILogic().GetTagsFromEntities(UITaggableEntity.ItemRegistration);

                var categories = new TagCategoryUILogic().GetAllTagCategories();
                ViewBag.Categories = categories;

                var tags = _itemRegistrationUILogic.GetTagsFromItemRegistration();
                ViewBag.tags = tags;

                TimeCategories tCategory = TimeCategories.All;
                Enum.TryParse<TimeCategories>(time, out tCategory);

                var itemRegistration = _itemRegistrationUILogic.Get(criteria, tCategory);

                return View(itemRegistration);
            }

            catch (Exception exception)
            {
                if (!ExceptionHandler.Manage(exception, this, Layer.UILogic))
                    return RedirectToAction("Error", "Errors");
            }
            return View();
        }

        public ActionResult AddItemRegistration()
        {
            var categories = new TagCategoryUILogic().GetNotDefault();
            ViewBag.Categories = categories;

            var tags = _itemRegistrationUILogic.GetTagsFromItemRegistration();

            ViewBag.tags = tags;
            ViewBag.ItemsRegistrationsTypes =
                Lok.Unik.ModelCommon.ItemRegistration.ItemRegistrationType.GetProperties().ToList();

            if (Request.IsAjaxRequest())
                return PartialView("AddItemRegistration");

            return View("AddItemRegistration");
        }

        [HttpPost]
        public ActionResult AddItemRegistration(
            string name, string passCode,
            string newName, string newCategories,
            string tags, string itemRegistrationType, string facilityId)
        {

            var regEx = new Regex(@"\s+");
            name = regEx.Replace(name, @" ");
            passCode = regEx.Replace(passCode, @" ");

            if(!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(passCode))
            {

                var isUnique = _itemRegistrationUILogic.IsUnique(name, passCode);

                if (!isUnique)
                    return Json(Boolean.FalseString);
            }

            var user = User as ApplicationUser;

            var listTags = tags.Split(',').Where(element => !String.IsNullOrEmpty(element.Trim())).ToList();
            var names = newName.Split(',').Where(element => !String.IsNullOrEmpty(element.Trim())).ToList();
            var categories = newCategories.Split(',').Where(element => !String.IsNullOrEmpty(element.Trim())).ToList();

            var confirName = new List<string>();
            var confirCategorie = new List<string>();

            var newTags = new List<TagsUI.TagsUI.Models.Tag>();

            for (var i = 0; i < names.Count; i++)
            {
                if (names.ElementAt(i) == null) continue;
                if (!listTags.Contains(names.ElementAt(i))) continue;

                confirName.Add(names.ElementAt(i));
                confirCategorie.Add(categories.ElementAt(i));
                listTags.Remove(names.ElementAt(i));
            }

            if (confirName.Count != 0)
            {
                var tagCategory = new TagCategoryUILogic().GetAllTagCategories();
                for (int q = 0; q < confirCategorie.Count; q++)
                {
                    foreach (TagsUI.TagsUI.Models.TagCategory category in tagCategory)
                    {
                        if (category.Name.Equals(confirCategorie.ElementAt(q)))
                        {
                            newTags.Add(new TagsUI.TagsUI.Models.Tag
                                            {
                                                Id = Guid.NewGuid(),
                                                Name = confirName.ElementAt(q),
                                                Category = confirCategorie.ElementAt(q),
                                                Color = category.Color,
                                                Type = TagType.ItemRegistration.ToString(),
                                            });
                            break;
                        }
                    }
                }

                newTags = newTags.Distinct().ToList();
            }

            _itemRegistrationUILogic.AddItemRegistration(name, passCode, itemRegistrationType, newTags, listTags, user,
                                                         facilityId);

            return RedirectToAction("Index", "ItemRegistration");
        }

        public ActionResult Delete(Guid id)
        {
            var itemRegistration = _itemRegistrationUILogic.GetById(id);

            if (Request.IsAjaxRequest())
                return PartialView("Delete", itemRegistration);

            return View("Delete", itemRegistration);
        }

        [HttpPost]
        public ActionResult Delete(
            Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration itemRegistration)
        {
            var user = User as ApplicationUser;

            try
            {
                _itemRegistrationUILogic.Delete(itemRegistration.Id);
                return null;

            }

            catch (Exception ex)
            {
                _log.ErrorFormat("Current User: {0} - An exception occurred with the following message: {1}",
                                 user.UserName, ex.Message);
                _applicationAlert.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());
            }
            return Request.IsAjaxRequest()
                       ? PartialView("Delete", itemRegistration)
                       : (ActionResult)View("Delete", itemRegistration);
        }

    }
}
