using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using AppComponents;
using Shrike.Resources;


namespace Shrike.UserManagement.BusinessLogic.Models
{
    public class ForgotPasswordUser
    {
        [Display(Name = "Contact Email:")]
        public string ContactEmail { set; get; }
    }
}
