using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Shrike.Areas.ErrorManagementUI.ErrorManagementUI.Helpers
{
    public static class ContentType
    {
        public static string TextPlain = "text/plain";
        //other contentType, HTML may be.
    }

    public enum StatusCode
    {
        Http400 = 400,
        Http404 = 404,
        Http403 = 403
        //
    }

    public class CustomErrors
    {
        public static string UnknownError = "Unknown failure";
        public static string Error400 = "Bad Request";
        public static string Error401 = "Unauthorized";
        public static string Error402 = "Payment Required";
        public static string Error403 = "Forbidden";
        public static string Error404 = "Not Found";
        public static string Error405 = "Method Not Allowed";
        public static string Error408 = "Request Timeout";


        /// <summary>
        /// Set the Layout for the errors page, in agreement to the type of Request realized.
        /// </summary>
        public static void SetLayoutForErrorPage(bool isAjax, bool? mainLayout=false)
        {
            if (HttpContext.Current.Request.IsAuthenticated)
            {
                HttpContext.Current.Items["Layout"] = isAjax ? null : "~/Views/Shared/_ShrikePartialLayout.cshtml";
            }
            else
            {
                HttpContext.Current.Items["Layout"] = "~/Views/Shared/_ErrorPartialLayout.cshtml";
            }

            if (mainLayout.HasValue && mainLayout.Value)
                HttpContext.Current.Items["Layout"] = "~/Views/Shared/_ShrikePartialLayout.cshtml";
        }
    }


}
