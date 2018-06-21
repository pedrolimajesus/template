using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Models
{
    public class LoggingConfigModel
    {
        public String IdNode { get; set; }
        public string File { get; set; }
        public string ClassFilter { get; set; }
        public int LogLevel { get; set; } 

    }
}