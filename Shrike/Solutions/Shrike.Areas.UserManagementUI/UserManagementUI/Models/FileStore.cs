using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Models
{
    public class FileStore
    {
        public string FolderPath { get; set; }
        public CommandStatus Status { get; set; }
    }
}