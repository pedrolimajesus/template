using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Models
{
    public class LicenseModel
    {
        public Guid Id { get; set; }

        public string LicenseName { get; set; }

        public string FileServerPath { get; set; }

    }
}