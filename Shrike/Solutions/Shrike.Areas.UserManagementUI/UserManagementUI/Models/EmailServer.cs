using System.ComponentModel.DataAnnotations;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Models
{
    public class EmailServer
    {
        [Required]
        [DataType(DataType.Text)]
        public string SmtpServer { get; set; }

        [Required]
        public int Port { get; set; }

        public bool IsSsl { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public CommandStatus Status { get; set; }

        [Required]
        [RegularExpression(
            @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
            ErrorMessage = "Not a valid email")]
        [DataType(DataType.EmailAddress)]
        public string ReplyAddress { get; set; }
    }

    public class TestEmailServer
    {
        [Required]
        [DataType(DataType.Text)]
        public string SmtpServer { get; set; }

        [Required]
        public int Port { get; set; }

        public bool IsSsl { get; set; }

        [Required]
        [DataType(DataType.Text)]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public CommandStatus Status { get; set; }

        [Required]
        [RegularExpression(
            @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
            ErrorMessage = "Not a valid email")]
        [DataType(DataType.EmailAddress)]
        public string TestEmailAddress { get; set; }

        [Required]
        [RegularExpression(
            @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
            ErrorMessage = "Not a valid email")]
        [DataType(DataType.EmailAddress)]
        public string ReplyAddress { get; set; }
    }
}