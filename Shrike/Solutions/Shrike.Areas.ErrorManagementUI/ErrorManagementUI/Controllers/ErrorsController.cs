using Shrike.Areas.ErrorManagementUI.ErrorManagementUI.Helpers;

namespace Shrike.Areas.ErrorManagementUI.ErrorManagementUI.Controllers
{
    using System;
    using System.Web.Mvc;

    //[Authorize]
    public class ErrorsController : Controller
    {
        //
        // GET: /Errors/
        [AllowAnonymous]
        public ActionResult Unknown(Exception exception)
        {
            return this.Content(CustomErrors.UnknownError, ContentType.TextPlain);
        }

        [AllowAnonymous]
        public ActionResult Error()
        {
            //this.Response.StatusCode = (int)StatusCode.Http400;
            Response.TrySkipIisCustomErrors = true;
            //var exception = this.Server.GetLastError() as Exception;

            var exception = HttpContext.Items["error"] as Exception;
            ViewBag.Layout = HttpContext.Items["Layout"];
            if (exception == null) return View();

            var handleErrorInfo = new HandleErrorInfo(exception, "Errors", "Error");
            return this.View(handleErrorInfo);
        }

        [AllowAnonymous]
        public ActionResult Http400()
        {
            this.Response.StatusCode = (int)StatusCode.Http400;
            this.Response.TrySkipIisCustomErrors = true;

            return View();
        }

        [AllowAnonymous]
        public ActionResult Http404()
        {
            this.Response.StatusCode = (int)StatusCode.Http404;
            this.Response.TrySkipIisCustomErrors = true;
            return View();
        }

        [AllowAnonymous]
        public ActionResult Http403()
        {
            return this.Content(CustomErrors.Error403, ContentType.TextPlain);
        }

    }
}
