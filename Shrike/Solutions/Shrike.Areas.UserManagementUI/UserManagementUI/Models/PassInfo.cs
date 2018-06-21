using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Models
{
    public class PassInfo
    {
        public string UserId { get; set; }
        
        [Required]
        [DataType(DataType.Password)]
        [DisplayName("New Password")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [DisplayName("Confirm Password")]
        public string ConfirmPassword { get; set; }
    }
}