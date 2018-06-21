using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Shrike.Areas.UserManagementUI.UserManagementUI.Controllers;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Models
{

    public class DataBase
    {
        public string DatabaseUrl { get; set; }

        public CommandStatus Status { get; set; }
    }
}