using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shrike.Areas.UserManagementUI
{
    public class AreaPortableName
    {
        public static Shrike.Areas.UserManagementUI.UserManagementUI.UserManagementUIAreaRegistration areaManagementUI = new UserManagementUI.UserManagementUIAreaRegistration();

        public static string AreaName
        {
            get { return areaManagementUI.AreaName; }
        }
    }
}