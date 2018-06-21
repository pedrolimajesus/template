using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using Shrike.ExceptionHandling;
using Shrike.ExceptionHandling.Logic;
using FormCollection = System.Web.Mvc.FormCollection;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Controllers
{
    using AppComponents;
    using AppComponents.Web;

    using UILogic;
    using Models;
    using UserManagement.BusinessLogic.Business;

    using log4net;

    using Tenancy.DAL.Managers;

    using AppComponents.Extensions.ExceptionEx;
    using ExceptionHandling.Exceptions;

    using DAL.Manager;
    using Tenancy.Web;
    using Lok.Unik.ModelCommon.Aware;


    [HandleError]
    [ContextAuthorize("InviteUser")]
    public class UserController : Controller
    {
        private readonly ILog _log;

        private readonly IApplicationAlert _applicationAlert;
        private readonly UserBusinessLogic _userBusinessLogic;
        private readonly UserUILogic _userUILogic;
        private readonly IEnumerable<string> _command;

        public UserController()
        {
            _userBusinessLogic = new UserBusinessLogic();
            _userUILogic = new UserUILogic();
            _log = ClassLogger.Create(GetType());

            _applicationAlert = Catalog.Factory.Resolve<IApplicationAlert>();

            _command = new List<string>
                           {
                               "Invite New User",
                               "Enable",
                               "Disable",
                               "Change User Role",
                               "Edit User",
                               "Details User",
                               "Delete User",
                               "Assign Tags",
                               "Clear Tags",
                               "ReSent Email",
                               "Assign Group",
                               "Remove Group"
                           };
        }

        public ActionResult Index(bool reload = false)
        {
            ViewBag.ItemsEnable = _command;
            ViewBag.IsReloadInvited = reload;

            var time = Request["kind"];
            var criteriaTime = time ?? string.Empty;
            TimeCategories timeCategorie;

            if (!Enum.TryParse<TimeCategories>(criteriaTime, out timeCategorie))
            {
                timeCategorie = TimeCategories.All;
            }

            var allUsers = _userUILogic.GetAllValidUsers(timeCategorie);

            ViewBag.BasePath = Server.MapPath("~");

            return View(allUsers);
        }

        public ActionResult InviteUser()
        {
            var current = TempData["CurrentInvite"] as User;
            var messageError = TempData["MessageError"];
            ViewBag.ItemsEnable = _command;

            var tenants = TenantManager.GetAll();

            var externalContextRoles = TempData["ExternalContextRole"] as Dictionary<string, string>;
            var roles = externalContextRoles ?? RoleManager.TenantRoleDescriptions as Dictionary<string, string>;

            ViewBag.Roles = roles;
            ViewBag.Tenants = tenants;

            return current == null && messageError == null ? PartialView() : PartialView(current);
        }

        [HttpPost]
        public ActionResult InviteUser(User newUser, FormCollection collection)
        {
            var userLogin = User as ApplicationUser;
            try
            {
                var band = _userUILogic.NotFoundUserWithEmail(newUser.Email);

                if (band)
                {
                    var currentTenancy = TenantManager.CurrentTenancy;
                    if (currentTenancy.Equals(Tenants.SuperAdmin, StringComparison.OrdinalIgnoreCase))
                    {
                        //It should create a tenant
                        var tenantToInvite = collection.GetValue("selectedTenant").AttemptedValue.Split('/').Last();
                        _userUILogic.SendInvitation(newUser, tenantToInvite, currentTenancy, Request.Url, userLogin);
                    }

                    else
                    {
                        _userUILogic.SendInvitation(newUser, currentTenancy, Request.Url, userLogin);
                    }

                    var toUrl = Request.UrlReferrer;
                    if (toUrl != null) return Redirect(toUrl.AbsoluteUri);
                }

                TempData.Add("CurrentInvite", newUser);
                TempData.Add("MessageError", "Email already invited, try with another email");

                ModelState.AddModelError("Email", "Email already invited, try with another email");
                return RedirectToAction("Index", new { reload = true });
            }

            catch (Exception ex)
            {
                ExceptionHandler.Manage(ex, this, Layer.UILogic);

                if (ex.InnerException != null)
                {
                    _log.ErrorFormat("{0} \n InnerException: {1}", ex, ex.InnerException);
                }
                else if (userLogin != null)
                {
                    _log.ErrorFormat(
                        "Current User: {0} - An exception occurred with the following message: {1}",
                        userLogin.UserName,
                        ex.Message);
                }

                const string smtpExceptionMessage = "Failure to send the email, please check the SMTP configuration";
                TempData.Remove("MessageError");
                TempData.Add("MessageError", smtpExceptionMessage);

                TempData.Remove("CurrentInvite");
                TempData.Add("CurrentInvite", newUser);

                return RedirectToAction("Index", new { reload = true });
            }
        }

        public ActionResult Details(Guid id)
        {
            Shrike.Areas.ErrorManagementUI.ErrorManagementUI.Helpers.CustomErrors.SetLayoutForErrorPage(
                Request.IsAjaxRequest());

            ViewBag.ItemsEnable = _command;
            var user = _userUILogic.GetUserById(id);

            return Request.IsAjaxRequest() ? PartialView("Details", user) : PartialView(user);
        }

        public ActionResult ResetPassword(string id)
        {
            try
            {
                Shrike.Areas.ErrorManagementUI.ErrorManagementUI.Helpers.CustomErrors.SetLayoutForErrorPage(
                   Request.IsAjaxRequest());
                return PartialView("ResetPassword", new PassInfo() { UserId = id });
            }
            catch (Exception ex)
            {
                string message = string.Concat(
                    ex.ToString(), ex.InnerException != null ? ex.InnerException.ToString() : string.Empty);
                if (ex.InnerException != null)
                {
                    ViewBag.Error = ex.ToString() + "\n InnerException: " + ex.InnerException;
                    throw new Exception(ex.ToString(), ex.InnerException);
                }
                else
                {
                    ViewBag.Error = ex.ToString();
                    throw new Exception(ex.ToString());
                }
            }


        }

        [HttpPost]
        public ActionResult ResetPassword(PassInfo info)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (info == null || string.IsNullOrEmpty(info.NewPassword))
                    {
                        ViewBag.Status = "Error";
                        ViewBag.Error = "Error collecting information";
                    }
                    else
                    {
                        if (ChangePasswordInfo(info))
                        {
                            ViewBag.Status = "Passed";
                            //info = new PassInfo(); //clean view model
                        }
                        else
                        {
                            ViewBag.Status = "Error";
                            ViewBag.Error = "";
                        }
                    }
                }
                return PartialView("ResetPassword", info);
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Error reset Password [{0}], Trace [{1}]", e.Message, e.StackTrace);
                ViewBag.Status = "Error";
                //ViewBag.Error = "Error: [" + HttpUtility.HtmlEncode(e.Message) + "]";
                return PartialView("ResetPassword", info);
            }

        }

        private bool ChangePasswordInfo(PassInfo model)
        {
            bool response = false;
            try
            {
                var accountUser = _userBusinessLogic.GetById(Guid.Parse(model.UserId));
                MembershipUser membershipUser = Membership.GetUser(accountUser.AppUser.UserName);
                if (membershipUser != null)
                {
                    response = membershipUser.ChangePassword(accountUser.AppUser.PasswordHash, model.NewPassword);
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return response;
        }

        //
        // GET: /User/Edit/5

        public ActionResult Edit(string id)
        {
            var guid = Guid.Empty;
            var result = PartialView("Edit", new User());
            try
            {
                Shrike.Areas.ErrorManagementUI.ErrorManagementUI.Helpers.CustomErrors.SetLayoutForErrorPage(
                    Request.IsAjaxRequest());

                if (Guid.TryParse(id, out guid))
                {
                    ViewBag.RolesString = _userBusinessLogic.GetTenantRoles(); // RoleManager.TenantRoles;
                    var user = _userUILogic.GetUserById(guid);
                    result = PartialView("Edit", user);
                }
            }
            catch (Exception ex)
            {
                string message = string.Concat(
                    ex.ToString(), ex.InnerException != null ? ex.InnerException.ToString() : string.Empty);
                if (ex.InnerException != null)
                {
                    ViewBag.Error = ex.ToString() + "\n InnerException: " + ex.InnerException;
                    throw new Exception(ex.ToString(), ex.InnerException);
                }
                else
                {
                    ViewBag.Error = ex.ToString();
                    throw new Exception(ex.ToString());
                }
            }
            return result;
        }

        //
        // POST: /User/Edit/5

        [HttpPost]
        public ActionResult Edit(User user)
        {
            var id = new Guid(Request.Form["Id"]);
            var user1 = User as ApplicationUser;
            try
            {
                _userUILogic.UpdateUser(id, user);

                return RedirectToAction("Index", "User", new { allbody = "both" });
            }
            catch (Exception exception)
            {
                if (user1 != null)
                {
                    this._log.ErrorFormat(
                        "Current User: {0} - An exception occurred with the following message: {1}",
                        user1.UserName,
                        exception.Message);
                }
                _applicationAlert.RaiseAlert(ApplicationAlertKind.System, exception.TraceInformation());
                if (Request.IsAjaxRequest())
                {
                    return PartialView("Edit", user);
                }
                return View("Edit", user);
            }
        }

        [Authorize(Roles = RoleFlags.OwnerAndTenantOwners)]
        public ActionResult Delete(Guid id)
        {
            Shrike.Areas.ErrorManagementUI.ErrorManagementUI.Helpers.CustomErrors.SetLayoutForErrorPage(
                Request.IsAjaxRequest());

            var user = _userUILogic.GetUserById(id);
            if (Request.IsAjaxRequest())
            {
                return PartialView("Delete", user);
            }
            ViewBag.ItemsEnable = _command;
            return PartialView("Delete", user);
        }

        //
        // POST: /User/Delete/5
        [Authorize(Roles = RoleFlags.OwnerAndTenantOwners)]
        [HttpPost]
        public ActionResult Delete()
        {
            var id = Request.Form["idUserDelete"];
            var user1 = User as ApplicationUser;
            try
            {
                var currentUser = (ApplicationUser)System.Web.HttpContext.Current.User;
                _userUILogic.Remove(new Guid(id), currentUser);

                return RedirectToAction("Index", "User");
            }
            catch (Exception exception)
            {
                if (user1 != null)
                {
                    this._log.ErrorFormat(
                        "Current User: {0} - An exception occurred with the following message: {1}",
                        user1.UserName,
                        exception.Message);
                }
                _applicationAlert.RaiseAlert(ApplicationAlertKind.System, exception.TraceInformation());

                return RedirectToAction("Index", "User");
            }
        }

        public ActionResult EnableDisable(Guid id, string status)
        {
            _userUILogic.EnableDisable(id, status);
            return RedirectToAction("Index", "User");
        }

        public ActionResult ChangeRole(Guid id)
        {
            var userToChange = _userUILogic.GetUserById(id);

            if (null != userToChange) return Request.IsAjaxRequest() ? PartialView("ChangeRole", userToChange) : PartialView(userToChange);

            return RedirectToAction("Index", "User");
        }

        [HttpPost]
        public ActionResult ChangeRole(User user, FormCollection collection)
        {
            var currentUser = User as ApplicationUser;

            var currentRole = Request.Form["CurrentRole"];//the id
            var roleToChange = collection.GetValue("Roles").AttemptedValue; //new role id

            if (currentRole.Equals(roleToChange, StringComparison.InvariantCultureIgnoreCase))
            {
                return RedirectToAction("Index", "User");
            }

            _userUILogic.ChangeRoleUser(user.Id, roleToChange, user.Email, currentUser);
            return RedirectToAction("Index", "User");
        }

        public ActionResult ExternalContextRoles(string key, string value)
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                var externalRoles = new Dictionary<string, string> { { key, value } };
                TempData.Add("ExternalContextRole", externalRoles);
                return Json(Boolean.TrueString, JsonRequestBehavior.AllowGet);
            }

            return Json(Boolean.FalseString, JsonRequestBehavior.AllowGet);

        }
    }
}