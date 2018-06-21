namespace Shrike.Areas.UserManagementUI.UserManagementUI.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Web;

    using AppComponents.Web;

    public class User
    {
        public User()
        {
            //AssociateDevices = new List<Device>();
        }

        [Required]
        [RegularExpression(@"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Not a valid email")]
        [DataType(DataType.EmailAddress)]
        [DisplayName("Email address:")]
        public virtual string Email { get; set; }

        [Required]
        [DisplayName("Roles:")]
        public virtual ICollection<string> Roles { get; set; }

        [DisplayName("Tags:")]
        public virtual IEnumerable<Shrike.Areas.TagsUI.TagsUI.Models.Tag> Tags { get; set; }

        [DisplayName("Admin Over:")]
        public virtual string AdminOver { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "Not a valid Number")]
        [DisplayName("Expire after days:")]
        [Range(1, 90, ErrorMessage = "Invitation should expire between 1 and 90 days")]
        public int ExpirationTime { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\/\.-_]*$", ErrorMessage = "Not a Valid User Name")]
        [DisplayName("Username:")]
        public string Username { get; set; }
        public Guid Id { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\s]*$", ErrorMessage = "Not a valid First Name")]
        [DisplayName("First Name:")]
        public string FirstName { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\s]*$", ErrorMessage = "Not valid Last Name")]
        [DisplayName("Last Name:")]
        public string LastName { get; set; }
        
        [DisplayName("Status:")]
        public UserStatus Status { get; set; }

        [DisplayName("Date Created:")]
        public string DateCreated { get; set; }

        public string RoleInvitation { get; set; }

        //public IList<Device> AssociateDevices { get; set; }

    }
}
