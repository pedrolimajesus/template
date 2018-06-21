using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using AppComponents.Web;
//using Microsoft.Web.Mvc;

namespace Shrike.UserManagement.BusinessLogic.Models
{
    public class OwnerInvitationModel
    {
        public Guid Id { get; set; }

        [Required]
        [RegularExpression(@"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Not a valid email")]
        [Display(Name = "Send to email")]
        public string SentTo { get; set; }

        private string _tenancy;

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\/\.-_]*$", ErrorMessage = "Not a valid Tenancy")]
        [Display(Name = "Invite to Tenancy")]
        public string Tenancy { get { return _tenancy; } set { _tenancy = value!=null? value.ToLowerInvariant():value; } }

        [Required]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "Not a valid number")]
        [Display(Name = "Expire in days")]
        //in days
        [Range(1, 90, ErrorMessage = "Invitation should expire between 1 and 90 days")]
        public int ExpirationTime { get; set; }

        [ReadOnly(true)]
        [Display(Name = "Authorization Code")]
        public string AuthorizationCode { get; set; }

        [ReadOnly(true)]
        [Display(Name = "Last Invitation Date")]
        public DateTime DateSent { get; set; }

        [ReadOnly(true)]
        [Display(Name = "Accepting User")]
        public string AcceptingUserName { get; set; }

        [ReadOnly(true)]
        [Display(Name = "Role")]
        public string Role { get; set; }

        //used or not
        [ReadOnly(true)]

        [Display(Name = "Current status")]
        public InvitationStatus Status { get; set; }

        [ReadOnly(true)]
        [Display(Name = "Times resent")]
        public int ResentTimes { get; set; }

        [ReadOnly(true)]
        [Display(Name = "Email Content")]
        public string EmailContent { get; set; }

        public string InvitingUserId { get; set; }

        public string InvitingTenancy { get; set; }

        public string AcceptingUserId { get; set; }
    }
}
