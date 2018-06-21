using System;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using AppComponents.Raven;
using AppComponents.Web;
using Shrike.App_Start;
using System.Web.Optimization;
using Shrike.Areas.UserManagementUI.UILogic;
using Shrike.DAL.Manager;
using Shrike.UserManagement.BusinessLogic.Business;

using log4net.Config;
using AppComponents;

namespace Shrike
{
    using System.Text.RegularExpressions;

    using ModelCommon.RavenDB;
    using Shrike.Tenancy.Web;
    using log4net;

    using MvcContrib.PortableAreas;

    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        private readonly ILog log = ClassLogger.Create(typeof(MvcApplication));

        private const string RegexStaticResourceString = @"(js|css|ico|jpe?g|gif|png|bmp|html?)$";

        private static readonly Regex RegexStaticResource = new Regex(RegexStaticResourceString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        protected void Application_Start()
        {
            //Configure logging
            XmlConfigurator.Configure();
            ClassLogger.Configure();

            AreaRegistration.RegisterAllAreas();

            PortableAreaRegistration.RegisterEmbeddedViewEngine();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            new TagCategoryManager().CreateDefaultCategories();
            RoleBusinessLogic.CreateRolesAndNavigationInfo();

            TenantHelper.TenantCreated += TenantHelperOnTenantCreated;
            ContextRoleRegister.RegisteredRoles.Add("ItemRegistration", RoleFlags.TenantOwner);
            ContextRoleRegister.RegisteredRoles.Add("InviteUser", RoleFlags.OwnerAndTenantOwners);

        }

        //catch an event that any caller can catch when a tenant is created
        //then the caller can create indexes or any other actions
        private void TenantHelperOnTenantCreated(object sender, TenantCreatedArgs tenantCreatedArgs)
        {
            //Put your code when a tenant is created on the DB
            using (var tenantSession = DocumentStoreLocator.Resolve(tenantCreatedArgs.TenantDocumentDBSite))
            {
                IndexesManager.CreateIndexes(tenantSession.Advanced.DocumentStore, tenantCreatedArgs.TenantName, typeof(SchedulePlanByTagFullPath));
            }
        }

        /// <summary>
        /// Retrieves the IPrincipal from ApplicationUser documents by using its principalId which should be unique
        /// </summary>
        /// <param name="principalId">unique id for a user for the hole system</param>
        /// <returns>The IPrincipal instance from ApplicationUser</returns>
        private static IPrincipal GetUserFromPrincipalId(string principalId)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var user = session.Load<ApplicationUser>(principalId);
                return user;
            }
        }

        //validate that a user when logged will not try to use another tenant
        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            //Since this method is called on every request
            //we want to fail as early as possible
            if (!Request.IsAuthenticated) return;//Check issues when not authenticated and doing logoff on old page

            //ignore static resources
            var matchesStaticResource = RegexStaticResource.IsMatch(Context.Request.FilePath);
            if (matchesStaticResource) return;

            //Forms auth ticket
            var formsId = Context.User.Identity as FormsIdentity;
            if (formsId == null) return;

            //redirect by authorization exception to login page
            if (Request.Url.AbsolutePath.Contains(FormsAuthentication.LoginUrl))
            {
                return;
            }

            var currentTenant = TenantHelper.GetCurrentTenantFormUrl(this.Context);

            if (TenantHelper.ValidateTenant(currentTenant, formsId))
            {
                return;
            }

            //The user is attempting to access a different tenant
            //than the one they logged into so sign them out
            //an and redirect to the home page of the new tenant
            //where they can sign back in (if they are authorized!)

            FormsAuthentication.SignOut();
            HttpContext.Current.User = new GenericPrincipal(new GenericIdentity(string.Empty), new string[] { });
            this.Response.Redirect("/" + currentTenant + "/" + Areas.UserManagementUI.AreaPortableName.AreaName);
        }

        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var id = Context.User.Identity as FormsIdentity;
            if (id == null)
            {
                return;
            }

            var principal = GetUserFromPrincipalId(id.Name);
            if (principal != null)
            {
                Thread.CurrentPrincipal = HttpContext.Current.User = principal;
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            try
            {
                bool? pageNotFound = null;

                var exception = Server.GetLastError();

                var httpException = exception as HttpException;

                

                /*
                //Text Code Lines for to find the library or file that is causing reflection problems in the project.
                var reflectionException = exception as System.Reflection.ReflectionTypeLoadException;
                if (reflectionException != null)
                {
                    var sb = new StringBuilder();
                    foreach (var exSub in reflectionException.LoaderExceptions)
                    {
                        sb.AppendLine(exSub.Message);
                        var exFileNotFound = exSub as FileNotFoundException;
                        if (exFileNotFound != null)
                        {
                            if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                            {
                                sb.AppendLine("Fusion Log:");
                                sb.AppendLine(exFileNotFound.FusionLog);
                            }
                        }

                        sb.AppendLine();
                    }

                    // The message indicating the library or file is write in the project log.
                    log.Error("Reflection Error: " + sb);
                }
                */
                //Response.Clear();

                Server.ClearError();

                
                if (httpException != null)
                {
                    var exceptionCode = httpException.GetHttpCode();

                    switch (exceptionCode)
                    {
                        case 400:
                            pageNotFound = false;
                            break;
                        case 404:
                            pageNotFound = true;
                            break;
                    }
                }

                var err = exception.ToString();

                HttpContext.Current.Items["error"] = exception;

                if (exception.InnerException != null)
                {
                    err += "\nInnerException: " + exception.InnerException;
                }

                this.log.Error("Url Controller: " + HttpContext.Current.Request.Url);
                this.log.Error(err);

                var routeData = new RouteData();
                routeData.Values.Add("controller", "Errors");
                routeData.DataTokens["area"] = "ErrorManagementUI";

                if (pageNotFound.HasValue)
                {
                    routeData.Values.Add("action", pageNotFound.Value ? "Http404" : "Http400");
                }
                else routeData.Values.Add("action", "Error");

                IController errorController = new Shrike.Areas.ErrorManagementUI.ErrorManagementUI.Controllers.ErrorsController();
                errorController.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));

            }
            catch (Exception ex)
            {
                this.log.Error("Unexpected error", ex);
            }
        }
    }
}