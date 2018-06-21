using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using AppComponents;
using AppComponents.Extensions.ExceptionEx;
using AppComponents.Web;
using log4net;
using Shrike.Areas.UserManagementUI.UILogic;
using Shrike.ExceptionHandling;
using Shrike.ExceptionHandling.Logic;
using Shrike.Tenancy.DAL.Managers;
using Shrike.UserManagement.BusinessLogic.Models;
using Lok.Unik.ModelCommon.Aware;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Controllers
{
    [NamedContext("context://ContextResourceKind/ApplicationRoot")]
    [Authorize]
    public class OwnerInvitationController : Controller
    {
        private readonly InvitationUILogic _invitationUILogic;

        private static string _systemOwnerId;

        private readonly ILog _log;
        private readonly IApplicationAlert _applicationAlert;
        private readonly IEnumerable<string> _command;
        private readonly IEnumerable<string> _command1;

        public OwnerInvitationController()
        {
            _invitationUILogic = new InvitationUILogic();
            _systemOwnerId = _invitationUILogic.GetSuperAdminId();

            _log = ClassLogger.Create(this.GetType());
            _applicationAlert = Catalog.Factory.Resolve<IApplicationAlert>();
            _command = new List<string> {"Create", "Edit", "Details", "Delete", "Sent Email"};
            _command1 = new List<string> {"Edit", "Details", "Delete", "Sent Email"};
        }

        private bool RedirectToCorrectTenancy(OwnerInvitationModel invitationModel, out ActionResult redirectToRoute)
        {
            redirectToRoute = null;

            var newTenancy = invitationModel.Tenancy;

            if (!TenantManager.CurrentTenancy.Equals(newTenancy, StringComparison.InvariantCultureIgnoreCase))
            {
                redirectToRoute = this.RedirectToRoute(
                    "Default",
                    new
                    {
                        tenant = newTenancy,
                        controller = this.ControllerContext.RouteData.Values["controller"].ToString(),
                        action = this.ControllerContext.RouteData.Values["action"].ToString(),
                        id = Server.UrlEncode(invitationModel.AuthorizationCode)
                    });
                return true;
            }
            return false;
        }

        //
        // GET: /Invitation/AcceptInvitation/Code
        [AllowAnonymous]
        public ActionResult AcceptInvitation(string id)
        {
            ViewBag.ItemsEnable = _command;
            if (string.IsNullOrEmpty(id))
            {
                return new HttpNotFoundResult();
            }

            var invitationModel = _invitationUILogic.GetInvitationModelByModelId(id);

            if (invitationModel == null)
            {
                return new HttpNotFoundResult();
            }
            else if (invitationModel.Status == InvitationStatus.Rejected ||
                     invitationModel.Status == InvitationStatus.Active)
            {
                return RedirectToAction("Login", "Account");
            }

            ActionResult redirectToRoute;
            return this.RedirectToCorrectTenancy(invitationModel, out redirectToRoute)
                ? redirectToRoute
                : this.View(invitationModel);
        }

        [HttpPost]
        //
        // POST: /Invitation/AcceptInvitation/Code
        [AllowAnonymous]
        public ActionResult AcceptInvitation(string id, bool accepted)
        {
            var appUser = User as ApplicationUser;
            var isAuthenticated = appUser != null && appUser.Identity.IsAuthenticated;
            return this.AcceptInvitationUILogic(id, appUser, isAuthenticated, accepted, this.Session);
        }

        public ActionResult AcceptInvitationUILogic(
            string id, ApplicationUser appUser, bool isAuthenticated, bool accepted,
            HttpSessionStateBase session)
        {
            ValidateId(id);

            var redirectToHome = this.RedirectToAction("Login", "Account");
            var redirectToRegister = this.RedirectToAction("Register", "Account");

            //retrieve authentication code from invitation and its invitation
            var invitationModel = this._invitationUILogic.GetInvitationModelByModelId(id);

            if (invitationModel == null)
            {
                return new HttpNotFoundResult();
            }

            //user rejected invitation
            if (!accepted)
            {
                this._invitationUILogic.InvitationRejected(invitationModel);
                return redirectToHome;
            }

            if (appUser != null && appUser.Status == UserStatus.RejectInvitation)
            {
                return redirectToHome;
            }

            ActionResult redirectToRoute;
            if (this.RedirectToCorrectTenancy(invitationModel, out redirectToRoute))
            {
                return redirectToRoute;
            }

            //add/update user
            if (isAuthenticated)
            {
                //update current user if using same email
                if (appUser.ContactEmail == invitationModel.SentTo)
                {
                    if (!this._invitationUILogic.UpdateInvitation(invitationModel, appUser.PrincipalId))
                    {
                        return redirectToHome;
                    }
                }
                else
                {
                    this.LogoutUser(invitationModel);
                    return redirectToRegister;
                }
            }
            else
            {
                // add user, redirect to registration form
                session["Invitation"] = invitationModel;
                return redirectToRegister;
            }
            return redirectToHome;
        }

        private void LogoutUser(OwnerInvitationModel invitationModel)
        {
            //logout current user
            FormsAuthentication.SignOut();
            this.HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), new string[] {});

            // add user, redirect to registration form
            this.Session["Invitation"] = invitationModel;
        }

        //
        // GET: /Invitation/
        [Authorize(Roles = RoleFlags.OwnerAndTenantOwners)]
        public ActionResult Index(string sort = "SentTo", string sortDir = "ASC", bool reload = false)
        {
            ViewBag.IsReloadInvited = reload;
            var time = Request["kind"];
            ViewBag.criteria = time;
            TimeCategories timeCategory;
            Enum.TryParse<TimeCategories>(time, out timeCategory);

            IEnumerable<OwnerInvitationModel> invitations;
            var user = User as ApplicationUser;

            if (user != null && user.Tenancy != Tenants.SuperAdmin)
            {
                invitations = string.IsNullOrEmpty(time)
                    ? _invitationUILogic.GetInvitationsFrom(user.Id)
                    : _invitationUILogic.GetInvitationsFrom(user.Id, timeCategory);
            }
            else
            {
                invitations = string.IsNullOrEmpty(time)
                    ? _invitationUILogic.GetInvitations()
                    : _invitationUILogic.GetInvitations(timeCategory);
            }

            var orderInvitation = GetInvitationDirection(sort, sortDir, invitations);
            ViewBag.LastSortedColumn = Request["sort"];
            ViewBag.ItemsEnable = _command;
            return View(orderInvitation);
        }

        private IEnumerable<OwnerInvitationModel> GetInvitationDirection(string sort, string sortDir,
            IEnumerable<OwnerInvitationModel> invitations)
        {
            IEnumerable<OwnerInvitationModel> invitationDireccion = new List<OwnerInvitationModel>();
            if (invitations.Any())
            {
                if (sort.ToLower() == "status")
                {
                    if (sortDir == "ASC")
                    {
                        invitationDireccion = from invitation in invitations
                            orderby invitation.Status.ToString() ascending
                            select invitation;
                    }
                    else
                    {
                        invitationDireccion = from invitation in invitations
                            orderby invitation.Status.ToString() descending
                            select invitation;
                    }
                }
            }
            if (!invitationDireccion.Any())
            {
                invitationDireccion = invitations;
            }
            return invitationDireccion;
        }

        //
        // GET: /Invitation/Details/5

        public ActionResult Details(string id)
        {
            ErrorManagementUI.ErrorManagementUI.Helpers.CustomErrors.SetLayoutForErrorPage(Request.IsAjaxRequest(), true);

            ViewBag.ItemsEnable = _command1;
            ValidateId(id);
            var invitationModel = _invitationUILogic.GetInvitationsModelById(new Guid(id), this.Request.Url);
            if (Request["criteria"] != null)
                ViewBag.criteria = Request["criteria"];
            return View(invitationModel);
        }

        //
        // GET: /Invitation/Create
        [Authorize(Roles = RoleFlags.OnlySuperAdmin)]
        public ActionResult Create()
        {
            var current = TempData["CurrentInvitation"] as OwnerInvitationModel;
            var messageError = TempData["MessageError"];
            ViewBag.MessageError = messageError;
            return current == null && messageError == null ? PartialView() : PartialView(current);
        }

        //
        // POST: /Invitation/Create
        [Authorize(Roles = RoleFlags.OnlySuperAdmin)]
        [HttpPost]
        public ActionResult Create(OwnerInvitationModel newModel)
        {
            var user = User as ApplicationUser;

            try
            {
                if (!ModelState.IsValid) return RedirectToAction("Index", new {reload = true});

                var emailNotExist = _invitationUILogic.NotFoundUserWithEmail(newModel.SentTo);
                if (emailNotExist)
                {
                    _invitationUILogic.CreateInvitation(newModel, Request.Url, user);

                    Thread.Sleep(100);
                    //an issue when adding new invitation it do not appear on the index after adding
                    return RedirectToAction("Index");
                }

                TempData.Add("CurrentInvitation", newModel);

                TempData.Remove("MessageError");
                TempData.Add("MessageError", "Email already invited, try with another email");

                ModelState.AddModelError("SentTo", "Email already invited, try with another email");
                return RedirectToAction("Index", new {reload = true});
            }
            catch (Exception ex)
            {
                var errorMessage = "Failure to send the email, please check the SMTP configuration";
                if (ExceptionHandler.Manage(ex, this, Layer.UILogic))
                {
                    errorMessage = ex.Message;
                }

                if (user != null)
                {
                    _log.ErrorFormat("Current User: {0} - An exception occurred with the following message: {1}",
                        user.UserName, ex.Message);
                }

                _applicationAlert.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());
                this.ModelState.AddModelError("Tenancy", errorMessage);

                TempData.Remove("MessageError");
                TempData.Add("MessageError", errorMessage);

                TempData.Remove("CurrentInvitation");
                TempData.Add("CurrentInvitation", newModel);

                return RedirectToAction("Index", new {reload = true});
            }
        }

        //
        // GET: /Invitation/Edit/email
        public ActionResult Edit(string id = "")
        {
            Guid guid;
            var result = PartialView("Edit", new OwnerInvitationModel());
            if (!Guid.TryParse(id, out guid)) return result;

            ViewBag.ItemsEnable = _command1;
            ValidateId(id);

            var ownerInvitation = _invitationUILogic.GetInvitationsModelById(guid);

            ViewBag.InvitationRol = ownerInvitation.Role;
            result = PartialView("Edit", ownerInvitation);

            return result;
        }

        //
        // POST: /Invitation/Edit/5

        [HttpPost]
        public ActionResult Edit(OwnerInvitationModel model)
        {
            var user = User as ApplicationUser;
            try
            {
                if (ModelState.IsValid)
                {
                    _invitationUILogic.UpdateInvitation(model);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                var errorMessage = "Failure to update an invitation, please check the SMTP configuration";
                if (ExceptionHandler.Manage(ex, this, Layer.UILogic))
                {
                    errorMessage = ex.Message;
                }

                if (user != null)
                {
                    _log.ErrorFormat("Current User: {0} - An exception occurred with the following message: {1}",
                        user.UserName, ex.Message);
                }

                _applicationAlert.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());
                this.ModelState.AddModelError("Tenancy", errorMessage);

                TempData.Remove("MessageError");
                TempData.Add("MessageError", errorMessage);

                TempData.Remove("CurrentInvitation");
                TempData.Add("CurrentInvitation", model);

                return RedirectToAction("Index", new {reload = true});
            }
        }

        //
        // GET: /Invitation/Delete/5

        public ActionResult Delete(string id)
        {
            ViewBag.ItemsEnable = _command1;
            ValidateId(id);

            var im = _invitationUILogic.GetInvitationsModelById(new Guid(id));

            return Request.IsAjaxRequest() ? PartialView("Delete", im) : this.PartialView(im);
        }

        //
        // POST: /Invitation/Delete/5

        [HttpPost]
        public ActionResult Delete(OwnerInvitationModel ownerInvitationModel)
        {
            var id = ownerInvitationModel.Id.ToString();
            Guid invitationId;
            if (!Guid.TryParse(id, out invitationId)) return this.View();

            var user = User as ApplicationUser;
            try
            {
                var success = _invitationUILogic.Remove(invitationId);
                if (success)
                {
                    return this.RedirectToAction("Index");
                }
            }
            catch (Exception exception)
            {
                _log.ErrorFormat(
                    "Current User: {0} - An exception occurred with the following message: {1}",
                    user.UserName,
                    exception.Message);
                _applicationAlert.RaiseAlert(ApplicationAlertKind.System, exception.TraceInformation());

                var im = this._invitationUILogic.GetInvitationsModelById(invitationId);
                return this.View(im);
            }

            return this.RedirectToAction("Index");
        }

        public ActionResult SendEmail(string id)
        {
            ViewBag.ItemsEnable = _command1;
            ValidateId(id);
            return this.PartialView(_invitationUILogic.GetInvitationsModelById(new Guid(id)));
        }

        [HttpPost]
        public ActionResult SendEmail()
        {
            var id = Request.Form["SelectedRow"];
            Guid invitationId;
            if (!Guid.TryParse(id, out invitationId)) return this.View();

            try
            {
                var user = User as ApplicationUser;

                if (user == null)
                {
                    throw new ApplicationException("You should log into the system before sending emails.");
                }

                _invitationUILogic.SendInvitationId(invitationId, Request.Url, user);
                return this.RedirectToAction("Index");

            }

            catch (Exception exception)
            {
                _log.ErrorFormat(exception.Message);
                _log.ErrorFormat("An exception occurred with the following message: {0}", exception.Message);
                _applicationAlert.RaiseAlert(ApplicationAlertKind.System, exception.TraceInformation());

                return this.View("SendEmail", _invitationUILogic.GetInvitationsModelById(invitationId));
            }
        }

        private void ValidateId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new HttpException(404, "HTTP/1.1 404 Not Found");
            }
        }
    }
}