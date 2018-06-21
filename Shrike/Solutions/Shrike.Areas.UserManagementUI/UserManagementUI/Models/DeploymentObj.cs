using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Models
{
    public class DeploymentObj
    {
        public string DatabaseUrl { get; set; }

        public DataBase Database { get; set; }

        public EmailServer EmailServer { get; set; }

        public FileStore FileStore { get; set; }

    }
}