using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shrike.Areas.GlobalUI.GlobalUI.Controllers
{
     [Authorize]
    public class FilterController : Controller
    {
        //
        // GET: /MenuLeft/

        public ActionResult Index()
        {
            return View();
        }

    }
}
