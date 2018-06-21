using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shrike.Areas.ErrorManagementUI
{
    public class AreaPortableName
    {
        public static Shrike.Areas.ErrorManagementUI.ErrorManagementUI.ErrorManagementUIAreaRegistration areaErrorUserManagementUI = new ErrorManagementUI.ErrorManagementUIAreaRegistration();

        public static string AreaName
        {
            get { return areaErrorUserManagementUI.AreaName; }
        }
    }
}
