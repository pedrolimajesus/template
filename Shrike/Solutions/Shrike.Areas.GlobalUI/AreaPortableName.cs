using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shrike.Areas.GlobalUI
{
    public class AreaPortableName
    {
        public static Shrike.Areas.GlobalUI.GlobalUI.GlobalUIAreaRegistration areaGlobalUI = new GlobalUI.GlobalUIAreaRegistration();

        public static string AreaName
        {
            get { return areaGlobalUI.AreaName; }
        }
    }
}