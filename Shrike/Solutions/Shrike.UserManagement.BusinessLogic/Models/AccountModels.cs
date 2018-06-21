using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shrike.UserManagement.BusinessLogic.Models
{
    public class ChangePasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password:")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password:")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password:")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Id { get; set; }
    }

    public class LoginModel
    {
        [Required]
        [Display(Name = "User name:")]
        public string UserName { get; set; }

        [Display(Name = "OpenId:")]
        public string OpenId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password:")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterModel
    {

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\s]*$", ErrorMessage = "Not a valid First Name")]
        [Display(Name = "First Name:")]
        public string FirstName { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\s]*$", ErrorMessage = "Not a valid Last Name")]
        [Display(Name = "Last Name:")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "User Name:")]
        [RegularExpression(@"^[a-zA-Z0-9\/\.-_]*$", ErrorMessage = "Not a valid User Name")]
        public string UserName { get; set; }

        [Required]
        [RegularExpression(@"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Not a valid email")]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email address:")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password:")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password:")]
       // [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        //[Required]
        [Display(Name = "Authentication Invite Code:")]
        public string AuthenticationCode { get; set; }
    }

    public class RegisterOpenIdModel
    {
        [Required]
        [Display(Name = "Open Id:")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email address:")]
        public string Email { get; set; }

        //[Required]
        [Display(Name = "Authentication Invite Code:")]
        public string AuthenticationCode { get; set; }
    }

    public class TakeOwnerShipModel
    {
        [Required]
        [Display(Name = "Pass Code:")]
        public string PassCode { get; set; }
    }

    public class AuthenticationInviteCodeModel
    {
        [Required]
        [Display(Name = "Authentication Invite Code:")]
        public string AuthenticationCode { get; set; }
    }
}