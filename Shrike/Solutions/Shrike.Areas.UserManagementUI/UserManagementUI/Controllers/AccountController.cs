using AppComponents;
using AppComponents.ControlFlow;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.Extensions.ExceptionEx;
using AppComponents.Web;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.RelyingParty;
using log4net;
using Shrike.Areas.UserManagementUI.UILogic;
using Shrike.DAL.Manager;
using Shrike.ExceptionHandling;
using Shrike.ExceptionHandling.Exceptions;
using Shrike.ExceptionHandling.Logic;
using Shrike.Tenancy.DAL.Managers;
using Shrike.UserManagement.BusinessLogic.Business;
using Shrike.UserManagement.BusinessLogic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Controllers
{
    [Authorize]
    [NamedContext("context://ContextResourceKind/ApplicationRoot")]
    public class AccountController : Controller
    {
        private readonly ILog _log;

        private readonly IApplicationAlert _applicationAlert;

        private readonly AccountBusinessLogic _accountBusinessLogic;

        private readonly NavigationBusinessLogic _navigationBusinessLogic;

        private static readonly OpenIdRelyingParty Openid = new OpenIdRelyingParty();

        private const string DefaultAvatarLocation = "~/Application_Upload/uploads/Photos/";

        public AccountController()
        {
            _accountBusinessLogic = new AccountBusinessLogic();
            _navigationBusinessLogic = new NavigationBusinessLogic();
            _log = ClassLogger.Create(this.GetType());
            _applicationAlert = Catalog.Factory.Resolve<IApplicationAlert>();
        }

        [AllowAnonymous]
        [ValidateInput(false)]
        public ActionResult LoginOpenId(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            var response = Openid.GetResponse();

            ActionResult loginOpenId;

            if (UserSubmittingIdentifier(response, out loginOpenId))
            {
                return loginOpenId;
            }

            // Stage 3: OpenID Provider sending assertion response
            switch (response.Status)
            {
                case AuthenticationStatus.Authenticated:
                    var username = response.FriendlyIdentifierForDisplay;
                    Session["FriendlyIdentifier"] = username;

                    //Check OpenID user exists on DB
                    if (_accountBusinessLogic.ValidateOpendIdUser(username, TenantManager.CurrentTenancy))
                    {
                        //login it with selected user
                        this.SignIn(username, false, TenantManager.CurrentTenancy);

                        ActionResult redirectToRoute;
                        if (this.LoginPostValidations(out redirectToRoute))
                        {
                            return redirectToRoute;
                        }
                    }
                    else //register user with more data about him first time he login with opendid
                    {
                        ActionResult registerOpenIdAction;
                        bool isSuperAdminTenancy;
                        return this.CheckSuperAdminTenancy(out registerOpenIdAction, out isSuperAdminTenancy)
                            ? registerOpenIdAction
                            : this.RedirectToAction("RegisterOpenId", "Account");
                    }

                    return this.RedirectAfterLogin(returnUrl);

                case AuthenticationStatus.Canceled:
                    this.ViewData["Message"] = "Canceled at provider";
                    Session["FriendlyIdentifier"] = null;
                    Session["Invitation"] = null;
                    return this.View("Login");
                case AuthenticationStatus.Failed:
                    Session["FriendlyIdentifier"] = null;
                    Session["Invitation"] = null;
                    this.ViewData["Message"] = response.Exception.Message;
                    return this.View("Login");
            }

            return new EmptyResult();
        }

        //
        // GET: /Account/TakeOwnership
        //
        [AllowAnonymous]
        public ActionResult TakeOwnership()
        {
            return View();
        }

        //
        // POST: /Account/TakeOwnership
        //
        [AllowAnonymous]
        [HttpPost]
        public ActionResult TakeOwnership(TakeOwnerShipModel model)
        {
            try
            {
                if (!Request.IsAuthenticated)
                    RedirectToAction("Login");

                if (!ModelState.IsValid)
                {
                    // If we got this far, something failed, redisplay form
                    return View(model);
                }

                _accountBusinessLogic.ValidateOwner(model);
                var validPasscode = _accountBusinessLogic.ValidatePasscode(model);

                if (!validPasscode) throw new ApplicationException("Pass Code is not correct");

                if (TempData.ContainsKey("passcode"))
                {
                    TempData["passcode"] = model.PassCode;
                }
                else
                {
                    TempData.Add("passcode", model.PassCode);
                }
                return RedirectToAction("Register");
            }
            catch (Exception ex)
            {
                _log.ErrorFormat(
                    "Current User: {0} - An exception occurred with TakeOwnership Passcode: {1}",
                    User.Identity.Name,
                    ex.Message);
                _applicationAlert.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());

                ModelState.AddModelError("Error", ex.Message);
                TempData.Remove("ownershipError");
                TempData["ownershipError"] = ex.Message;
                return View(model);
            }
        }

        //
        // GET: /Account/Login
        //
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // GET: /Account/AuthenticationInviteCode
        //
        public ActionResult AuthenticationInviteCode()
        {
            return View();
        }

        //
        // POST: /Account/AuthenticationInviteCode
        //
        [HttpPost]
        public ActionResult AuthenticationInviteCode(AuthenticationInviteCodeModel model)
        {
            if (!ModelState.IsValid) return this.View(model);

            if (string.IsNullOrEmpty(model.AuthenticationCode))
            {
                ModelState.AddModelError("AuthenticationCode", "The code is required");
            }
            else
            {
                var invitationLogic = new InvitationUILogic();
                var invitation = invitationLogic.GetInvitationModelByModelId(model.AuthenticationCode);

                if (invitation == null)
                {
                    ModelState.AddModelError("AuthenticationCode", "Code is invalid");
                }
                else
                {
                    if (invitation.Tenancy.Equals(TenantManager.CurrentTenancy, StringComparison.OrdinalIgnoreCase) ||
                        !User.Identity.IsAuthenticated)
                        return this.RedirectToRoute(
                            "Default",
                            new
                            {
                                tenant = invitation.Tenancy,
                                controller = "OwnerInvitation",
                                areaName = Areas.UserManagementUI.AreaPortableName.AreaName,
                                action = "AcceptInvitation",
                                id = invitation.AuthorizationCode //.UrlEncodedCode
                            });

                    FormsAuthentication.SignOut();
                    this.SetAuthCookie(User.Identity.Name, false, invitation.Tenancy);

                    return this.RedirectToRoute(
                        "Default",
                        new
                        {
                            tenant = invitation.Tenancy,
                            controller = "OwnerInvitation",
                            areaName = Areas.UserManagementUI.AreaPortableName.AreaName,
                            action = "AcceptInvitation",
                            id = invitation.AuthorizationCode //.UrlEncodedCode
                        });
                }
            }
            return this.View(model);
        }

        //
        // POST: /Account/Login
        //
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            try
            {
                ViewBag.ReturnUrl = Request["returnUrl"];
                if (ModelState.IsValid)
                {
                    //validates against current tenancy
                    if (Membership.ValidateUser(model.UserName, model.Password))
                    {
                        this.SignIn(model.UserName, model.RememberMe, TenantManager.CurrentTenancy);

                        ActionResult redirectToRoute;

                        return this.LoginPostValidations(out redirectToRoute)
                            ? redirectToRoute
                            : this.RedirectAfterLogin(returnUrl);
                    }

                    if (!this._accountBusinessLogic.SuperAdminExist())
                    {
                        var route = this.RedirectToRoute(
                            "Default",
                            new
                            {
                                tenancy = Tenants.SuperAdmin,
                                controller = "Account",
                                action = "TakeOwnership",
                                areaName = Areas.UserManagementUI.AreaPortableName.AreaName
                            });
                        return route;
                    }

                    var message = System.Web.HttpContext.Current.Session["ErrorMessage"] != null
                        ? System.Web.HttpContext.Current.Session["ErrorMessage"].ToString()
                        : "The user name or password provided is incorrect.";

                    this.ModelState.AddModelError(string.Empty, message);

                    TempData.Remove("UsernameError");
                    TempData.Add("UsernameError", message);
                }
            }
            catch (Exception exception)
            {
                var message = ExceptionHandler.Manage(exception, this, Layer.UILogic)
                    ? exception.Message
                    : "An error has ocurred, it has been logged correctly. Try again later.";
                ModelState.AddModelError(string.Empty, message);

                TempData.Remove("UsernameError");
                TempData.Add("UsernameError", message);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        private ActionResult RedirectAfterLogin(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl))
            {
                var rUrl = HttpUtility.UrlDecode(returnUrl);
                return this.Redirect(rUrl);
            }

            _log.Debug("before user");

            var user = this.User as ApplicationUser;

            if (user == null) throw new BusinessLogicException("User should be logged after login");
            _log.Debug("before navigationData");

            var navigationData = _navigationBusinessLogic.GetNavigationByCurrentUser(user);

            if (navigationData == null) throw new BusinessLogicException("User should have permissions after login");
            _log.Debug("before default view");

            var defaultViewItemDontExist = navigationData.NavigationItems == null
                                           || !navigationData.NavigationItems.Any()
                                           || navigationData.NavigationItems.First().ViewItems == null
                                           || !navigationData.NavigationItems.First().ViewItems.Any();
            if (defaultViewItemDontExist)
                throw new BusinessLogicException("User should have a default view after login");

            var defaultViewItem = navigationData.NavigationItems.First().ViewItems.First();
            _log.Debug("before redirect to route: " + TenantManager.CurrentTenancy + " isAuth " +
                       user.Identity.IsAuthenticated + " user: " + user.Identity.Name);
            return this.RedirectToRoute(
                "Default",
                new
                {
                    tenant = TenantManager.CurrentTenancy,
                    areaName = defaultViewItem.AreaName,
                    controller = defaultViewItem.ControllerName,
                    action = defaultViewItem.ActionName
                });
        }

        //
        // GET: /Account/LogOff
        //
        [AllowAnonymous]
        public ActionResult LogOff()
        {
            this.Logout();
            return RedirectToAction("Login", "Account");
        }

        private void Logout()
        {
            FormsAuthentication.SignOut();
            this.HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), new string[] {});
            this.Session["DefaultMenu"] = null;
        }

        //
        // GET: /Account/Register
        //
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        //
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //begin
                    var invitationUILogic = new InvitationUILogic();
                    var invitationModel = invitationUILogic.GetInvitationModelByModelId(model.AuthenticationCode);

                    if (invitationModel == null
                        &&
                        !TenantManager.CurrentTenancy.Equals(
                            Tenants.SuperAdmin, StringComparison.InvariantCultureIgnoreCase))
                    {
                        ModelState.AddModelError("AuthenticationCode", "Code is invalid");
                        ViewBag.ErrorMessage = "Code is invalid";
                        return View(model);
                    }

                    if (invitationModel != null)
                    {
                        ActionResult redirectToRoute;
                        if (this.RedirectToCorrectTenancy(invitationModel, out redirectToRoute))
                        {
                            return redirectToRoute;
                        }

                        if (!invitationModel.SentTo.Equals(model.Email, StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.ModelState.AddModelError("Email", "Email do not equals invitation's email");
                            ViewBag.ErrorMessage = "Email do not equals invitation's email";
                            return View(model);
                        }
                    }

                    //end
                    var invitation = this.Session["Invitation"] as OwnerInvitationModel ?? invitationModel;

                    Server.MapPath(DefaultAvatarLocation);

                    MembershipCreateStatus createStatus;

                    //membership created and log automatically
                    if (CreateMembershipUser(model, invitationUILogic.ModelToEntity(invitation), out createStatus))
                    {
                        Session["Invitation"] = null;
                        object passcode;

                        string code = null;
                        if (TempData.TryGetValue("passcode", out passcode))
                        {
                            code = passcode.ToString();
                        }

                        //if (code != null)
                        if (!string.IsNullOrEmpty(code))
                        {
                            TempData.Remove("passcode");

                            var passCodeModel = new TakeOwnerShipModel {PassCode = code};
                            if (_accountBusinessLogic.AddRoleToUser(passCodeModel, User.Identity.Name))
                            {
                                var user = User as ApplicationUser;

                                if (user != null &&
                                    user.Tenancy.Equals(Tenants.SuperAdmin, StringComparison.InvariantCultureIgnoreCase))
                                    return RedirectToAction("Index", "OwnerInvitation");

                                ActionResult redirectToRoute;
                                return this.LoginPostValidations(out redirectToRoute)
                                    ? redirectToRoute
                                    : RedirectAfterLogin(null);

                            }
                        }
                    }

                    this.ModelState.AddModelError(string.Empty, ErrorCodeToString(createStatus));
                }
            }
            catch (Exception ex)
            {
                if (ExceptionHandler.Manage(ex, this, Layer.UILogic))
                {
                    this.ModelState.AddModelError(string.Empty, ex.Message);
                    ViewBag.ErrorMessage = ex.Message;
                }
                else
                {
                    if (ex.InnerException != null)
                        _log.ErrorFormat("{0} \n Inner Exception: {1}", ex, ex.InnerException);
                    else _log.ErrorFormat("An exception occurred with the following message: {0}", ex.Message);

                    _applicationAlert.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());

                    const string errorMessage =
                        "An error occurred while processing your request. Please refresh the page. The error have been logged.";
                    this.ModelState.AddModelError(string.Empty, errorMessage);
                    ViewBag.ErrorMessage = errorMessage;
                }

                return View(model);
            }

            // If we got this far, something failed, redisplay form
            //return View(model);S
            ActionResult routeToRedirect;
            return this.LoginPostValidations(out routeToRedirect)
                ? routeToRedirect
                : RedirectAfterLogin(null);
            //return RedirectToAction("Index", "OwnerInvitation");
        }

        [AllowAnonymous]
        public ActionResult RedirectLogin()
        {
            //logout if logged
            FormsAuthentication.SignOut();
            System.Web.HttpContext.Current.User = new GenericPrincipal(new GenericIdentity(string.Empty),
                new string[] {});

            var rUrl = Request["returnUrl"];
            if (string.IsNullOrEmpty(rUrl))
            {
                //redirect to login if not return url found
                return RedirectToAction(
                    "Login", "Account", new {area = Shrike.Areas.UserManagementUI.AreaPortableName.AreaName});
            }

            var newUrl = Server.UrlDecode(rUrl);
            var newTenancy = newUrl.Split('/').Second();

            if (string.IsNullOrEmpty(newTenancy))
            {
                return RedirectToAction(
                    "Login", "Account", new {area = Shrike.Areas.UserManagementUI.AreaPortableName.AreaName});
            }

            var redirectToRoute = this.RedirectToRoute(
                "Default",
                new
                {
                    tenant = newTenancy,
                    areaName = Shrike.Areas.UserManagementUI.AreaPortableName.AreaName,
                    controller = "Account",
                    action = "Login",
                    returnUrl = Server.UrlEncode(newUrl)
                });

            return redirectToRoute;
        }

        //
        // GET: /Account/ChangePassword
        //
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword
        //
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = User as ApplicationUser;
                if (user == null) return new ContentResult();

                //MembershipUser currentUser = Membership.GetUser(User.Identity.Name, userIsOnline: true);
                var currentUser = Membership.GetUser(user.UserName, userIsOnline: true);

                // ChangePassword will throw an exception rather
                // than return false in certain failure scenarios.
                var changePasswordSucceeded = ChangePasswordSucceeded(model, currentUser);

                if (changePasswordSucceeded)
                {
                    return RedirectToAction("ChangePasswordSuccess");
                }

                ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePasswordSuccess
        //
        [AllowAnonymous]
        public ActionResult ChangePasswordSuccess()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult RegisterOpenId(string returnUrl)
        {
            ////retrieve the last validated user by principalId
            //var list = ContextRegistry.ContextsOf("Principal");
            //var principalId = list.Single().LocalPath.TrimStart('/');
            var isSuperAdminTenancy = TenantManager.CurrentTenancy.Equals(
                Tenants.SuperAdmin, StringComparison.InvariantCultureIgnoreCase);

            //if we are on system owner tenancy and there already exist a system owner then do not proceed.
            if (isSuperAdminTenancy && _accountBusinessLogic.SuperAdminExist())
            {
                Session["FriendlyIdentifier"] = null;
                Session["Invitation"] = null;
                return RedirectToAction("Http404", "Errors", new {area = "ErrorManagementUI"});
            }

            var usernamePassOpenId = Session["FriendlyIdentifier"] as string;
            var model = new RegisterModel
            {UserName = usernamePassOpenId, Password = usernamePassOpenId, ConfirmPassword = usernamePassOpenId};

            return this.View("Register", model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult RegisterOpenId(RegisterOpenIdModel model, string returnUrl)
        {
            ActionResult registerOpenIdAction;
            bool isSuperAdminTenancy;
            if (this.CheckSuperAdminTenancy(out registerOpenIdAction, out isSuperAdminTenancy))
                return registerOpenIdAction;

            if (ModelState.IsValid)
            {
                if (isSuperAdminTenancy)
                {
                    // Attempt to register the user
                    var createStatus = this.MembershipCreateUser(model);

                    switch (createStatus)
                    {
                        case MembershipCreateStatus.Success:
                        {
                            //FormsAuthentication.SetAuthCookie(model.UserName, createPersistentCookie: false);
                            this.SignIn(model.UserName, false, TenantManager.CurrentTenancy);

                            this.Session["Invitation"] = null;
                            this.Session["FriendlyIdentifier"] = null;

                            ActionResult redirectToRoute;
                            return this.LoginPostValidations(out redirectToRoute)
                                ? redirectToRoute
                                : this.RedirectToAction("Index", "User");
                        }
                        default:
                            this.ModelState.AddModelError(string.Empty, ErrorCodeToString(createStatus));
                            break;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(model.AuthenticationCode))
                    {
                        this.ModelState.AddModelError("Not invited", "User should be invited.");
                    }
                    else
                    {
                        var invitationController = new OwnerInvitationController();
                        var appUser = User as ApplicationUser;
                        var isAuthenticated = User.Identity.IsAuthenticated;
                        return invitationController.AcceptInvitationUILogic(
                            model.AuthenticationCode, appUser, isAuthenticated, true, Session);
                    }
                }
            }
            else
            {
                this.ModelState.AddModelError("Error", "Invalid Values");
            }
            return this.View(model);
        }

        [AllowAnonymous]
        public ActionResult VerifyAutentication()
        {
            return Json(Request.IsAuthenticated, JsonRequestBehavior.AllowGet);
        }

        private bool UserSubmittingIdentifier(IAuthenticationResponse response, out ActionResult loginOpenId)
        {
            if (response == null)
            {
                // Stage 2: user submitting Identifier
                Identifier id;
                if (Identifier.TryParse(Request.Form["OpenId"], out id))
                {
                    try
                    {
                        loginOpenId = Openid.CreateRequest(id).RedirectingResponse.AsActionResult();
                        return true;
                    }
                    catch (ProtocolException ex)
                    {
                        _log.ErrorFormat("An exception occurred with the following message: {0}", ex.Message);
                        _applicationAlert.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());
                        ViewData["Message"] = ex.Message;
                        {
                            loginOpenId = View("Login");
                            return true;
                        }
                    }
                }

                ViewData["Message"] = "Invalid identifier";
                {
                    loginOpenId = View("Login");
                    return true;
                }
            }

            loginOpenId = null;

            return false;
        }

        private bool LoginPostValidations(out ActionResult redirectToRoute)
        {
            //if no owner is present show takeownership page
            if (!_accountBusinessLogic.SuperAdminExist())
            {
                redirectToRoute = this.RedirectToRoute(
                    "Default", new {tenancy = Tenants.SuperAdmin, controller = "Account", action = "TakeOwnership"});
                return true;
            }

            //if user on default tenancy and do not have tenancy show the authentication code page
            var user = this.User as ApplicationUser;
            if (user == null)
            {
                redirectToRoute = this.RedirectToAction("Index", "User");
                return true;
            }

            if (string.IsNullOrEmpty(user.Tenancy)
                ||
                (user.Tenancy.Equals(Tenants.SuperAdmin, StringComparison.InvariantCultureIgnoreCase)
                 &&
                 !user.IsInRole(_accountBusinessLogic.GetRoleIdByRoleName(Tenants.SuperAdmin, Roles.ApplicationName))))
            {
                redirectToRoute = this.RedirectToAction("AuthenticationInviteCode");
                return true;
            }

            //if user on current tenancy but do not have TenantOperationsAdmin role and
            //its email has an invitation show the authentication code page
            if (user.Tenancy.Equals(TenantManager.CurrentTenancy, StringComparison.OrdinalIgnoreCase))
            {
                var invitationLogic = new OwnerInvitationBusinessLogic();
                string code;
                var hasInvitation = invitationLogic.UserHasInvitation(user.ContactEmail, user.Tenancy, out code);

                if (hasInvitation)
                {
                    redirectToRoute = this.RedirectToRoute(
                        "Default",
                        new
                        {
                            tenancy = user.Tenancy,
                            controller = "OwnerInvitation",
                            action = "AcceptInvitation",
                            id = code
                        });
                    return true;
                }
            }

            redirectToRoute = null;
            return false;
        }

        private void SignIn(string userName, bool createPersistentCookie, string tenantName)
        {
            //retrieve the last validated user by principalId
            var list = ContextRegistry.ContextsOf("Principal");
            var principalId = list.Single().LocalPath.TrimStart('/');

            //create forms authentication ticket taking into account the tenant name
            this.SetAuthCookie(principalId, createPersistentCookie, tenantName);

            //reset menu for current session
            Session["DefaultMenu"] = null;

            //registering last validated or created user as IPrincipal
            Thread.CurrentPrincipal =
                HttpContext.User =
                    System.Web.HttpContext.Current.User = _accountBusinessLogic.GetUserFromPrincipalId(principalId);
        }

        private void SetAuthCookie(string userName, bool createPersistentCookie, string tenantName)
        {
            var utcNow = DateTime.UtcNow;
            var ticket = new FormsAuthenticationTicket(
                1,
                userName,
                utcNow,
                utcNow.AddMinutes(this.Session.Timeout),
                createPersistentCookie,
                tenantName);

            _log.DebugFormat(
                "cookie user:'{0}', timeout:'{1}', persistent:'{2}, tenant:'{3}'",
                userName,
                this.Session.Timeout,
                createPersistentCookie,
                tenantName);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket));
            this.Response.AppendCookie(cookie);
        }

        public bool CreateMembershipUser(
            RegisterModel model, Invitation invitation, out MembershipCreateStatus createStatus)
        {
            // Attempt to register the user            

            Membership.CreateUser(
                model.UserName,
                model.Password,
                model.Email,
                passwordQuestion: null,
                passwordAnswer: null,
                isApproved: true,
                providerUserKey: invitation,
                status: out createStatus);

            if (createStatus == MembershipCreateStatus.Success)
            {
                _accountBusinessLogic.UpdateUser(model, invitation);
                this.SignIn(model.UserName, false, TenantManager.CurrentTenancy);

                return true;
            }
            return false;
        }

        private static bool ChangePasswordSucceeded(ChangePasswordModel model, MembershipUser currentUser)
        {
            bool changePasswordSucceeded = false;

            try
            {
                if (currentUser != null)
                {
                    changePasswordSucceeded = currentUser.ChangePassword(model.OldPassword, model.NewPassword);
                }
            }
            catch (Exception)
            {
                changePasswordSucceeded = false;
            }

            return changePasswordSucceeded;
        }


        private IEnumerable<string> GetErrorsFromModelState()
        {
            return ModelState.SelectMany(x => x.Value.Errors.Select(error => error.ErrorMessage));
        }

        #region Status Codes

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return
                        "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return
                        "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return
                        "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return
                        "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }

        #endregion

        private bool CheckSuperAdminTenancy(out ActionResult registerOpenIdAction, out bool isSuperAdminTenancy)
        {
            isSuperAdminTenancy = TenantManager.CurrentTenancy.Equals(
                Tenants.SuperAdmin, StringComparison.InvariantCultureIgnoreCase);

            registerOpenIdAction = null;
            //if we are on system owner tenancy and there already exist a system owner then do not proceed.
            if (isSuperAdminTenancy && this._accountBusinessLogic.SuperAdminExist())
            {
                this.Session["FriendlyIdentifier"] = null;
                this.Session["Invitation"] = null;
                registerOpenIdAction =
                    new HttpNotFoundResult("SuperAdmin tenancy cannot have more than one SuperAdmin user");
                //registerOpenIdAction = new ResourceErrorActionResult(new HttpException(string.Empty, new ApplicationException("SuperAdmin tenancy cannot have more than one SuperAdmin user")), new System.Net.Mime.ContentType("plain/text"));
                //new HttpNotFoundResult();//todo redirect to error page
            }

            return registerOpenIdAction != null;
        }

        private MembershipCreateStatus MembershipCreateUser(RegisterOpenIdModel model)
        {
            MembershipCreateStatus createStatus;
            Membership.CreateUser(
                model.UserName,
                model.UserName,
                model.Email,
                passwordQuestion: null,
                passwordAnswer: null,
                isApproved: true,
                providerUserKey: Session["Invitation"],
                status: out createStatus);

            return createStatus;
        }

        private bool RedirectToCorrectTenancy(OwnerInvitationModel invitation, out ActionResult redirectToRoute)
        {
            redirectToRoute = null;

            var newTenancy = invitation.Tenancy;

            if (!TenantManager.CurrentTenancy.Equals(newTenancy, StringComparison.InvariantCultureIgnoreCase))
            {
                redirectToRoute = this.RedirectToRoute(
                    "Default",
                    new
                    {
                        tenant = newTenancy,
                        controller = this.ControllerContext.RouteData.Values["controller"].ToString(),
                        action = this.ControllerContext.RouteData.Values["action"].ToString(),
                        id = invitation.AuthorizationCode //.UrlEncodedCode
                    });
                return true;
            }
            return false;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult ForgotEmailSendConfirmation()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ForgotPassword(ForgotPasswordUser forgotPasswordUser)
        {
            var appUserContactEmail = forgotPasswordUser.ContactEmail;
            var _userBusinessLogic = new UserBusinessLogic();
            var invitationManager = new InvitationManager();

            if (!string.IsNullOrEmpty(appUserContactEmail))
            {
                try
                {

                    var valid = Regex.IsMatch(appUserContactEmail, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase);
                    if (!valid)
                    {
                        TempData.Remove("EmailAddressError");
                        TempData.Add("EmailAddressError", "Please use a valid email address.");
                        return View("ForgotPassword");
                    }
                
                    var currentUrl = Request.Url;
                    var user = _userBusinessLogic.GetByEmail(appUserContactEmail);
                    bool sent = false;

                    if (user != null)
                    {
                        //Session["appUser"] = user.AppUser;
                        var applicationUser = user.AppUser;
                        sent = _userBusinessLogic.SendResetPasswordEmail(
                            applicationUser.ContactEmail, applicationUser.PrincipalId, currentUrl, applicationUser.Tenancy);
                        return RedirectToAction(sent ? "ForgotEmailSendConfirmation" : "ForgotPassword");
                    }

                    var systemOwner = _userBusinessLogic.GetSuperAdmin(appUserContactEmail);
                    if (systemOwner != null)
                    {
                        //Session["appUser"] = systemOwner;
                        sent = _userBusinessLogic.SendResetPasswordEmail(
                            systemOwner.ContactEmail, systemOwner.PrincipalId, currentUrl, systemOwner.Tenancy);
                        return RedirectToAction(sent ? "ForgotEmailSendConfirmation" : "ForgotPassword");
                    }
                }
                catch (Exception ex)
                {
                    TempData.Add("EmailAddressError", "The email was not sent, please contact the system administrator.");
                    var message = ExceptionHandler.Manage(ex, this, Layer.UILogic) ? ex.Message : "An unexpected error has ocurred.";
                    ModelState.AddModelError("ContactEmail", message);
                    return View("ForgotPassword");
                }
                TempData.Remove("EmailAddressError");
                TempData.Add("EmailAddressError", "The email address does not exist!.");
                ModelState.AddModelError("ContactEmail", "The email address does not exist!");
                return View("ForgotPassword");
            }
            TempData.Remove("EmailAddressError");
            TempData.Add("EmailAddressError", "Please the email address is required.");
            ModelState.AddModelError("ContactEmail", "Please the email address is required");
            return View("ForgotPassword");
        }

        [AllowAnonymous]
        public ActionResult ResetPassword(string id)
        {
            TempData["ResetPasswordCode"] = id;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ResetPassword(ChangePasswordModel model, string id)
        {
            //var code = TempData["ResetPasswordCode"] as string;

            var passwordReset = this._accountBusinessLogic.GetResetPasswordByCode(id);

            if (DateTime.UtcNow > passwordReset.ResetDateTime.AddDays(3))
                throw new HttpException(404, "Reset password not found");

            var applicationUser =
                (ApplicationUser) _accountBusinessLogic.GetUserFromPrincipalId(passwordReset.PrincipalId);

            if (applicationUser != null)
            {
                var oldPassword = applicationUser.PasswordHash;

                model.OldPassword = oldPassword;

                if (model.NewPassword == model.ConfirmPassword)
                {
                    MembershipUser membershipUser = Membership.GetUser(applicationUser.UserName);
                    var changePasswordSucceeded = ChangePasswordSucceeded(model, membershipUser);
                    if (changePasswordSucceeded)
                    {
                        return RedirectToAction("ChangePasswordSuccess");
                    }
                }

                ModelState.AddModelError("NewPassword", "the passwords are not equal");
                ModelState.AddModelError("ConfirmPassword", "the passwords are not equal");
            }

            return View();
        }

    }
}
