using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Models
{
    public enum CommandStatus
    {
        None = 0,
        TestPassed = 1,
        TestFailed = 2,
        SavePassed = 3,
        SaveFailed = 4,
        RestorePassed = 5,
        RestoreFailed = 6
    }

}