using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Shrike.UserManagement.BusinessLogic.Models
{
    using Tags.UI.Models;

    public class ApplicationModel
    {

        public ApplicationModel()
        {
            Tags = new Collection<Tag>();
            DeviceAssociates = new List<Device>();
        }

        public Guid Id { get; set; }

        [Required]
        [DisplayName("Name")]
        public string Name { get; set; }

        [Required]
        [DisplayName("File Tranfer")]
        public Uri FileTransferUri { get; set; }

        public string ServerFile { get; set; }
        [DisplayName("Icon")]
        public string IconPath { get; set; }

        public string Version { get; set; }

        [Required]
        [DisplayName("App Controller")]
        public string ManagedAppController { get; set; }

        [DisplayName("Executable In")]
        public string RelativeExePath { get; set; }

        [DisplayName("Application")]
        public string RelativeConfigPath { get; set; }

        [DisplayName("Installation In")]
        public string InstallationPath { get; set; }

        [DisplayName("File Size")]
        public string DeploymentFileSize { get; set; }

        [DisplayName("Additional Args")]
        public string AdditionalArgs { get; set; }

        [Required]
        [DisplayName("Controller Args")]
        public string ControllerArgs { get; set; }

        [Required]
        [DisplayName("Content Index")]
        public string ContentIndex { get; set; }

        public ICollection<Tag> Tags { get; set; }

        public IList<Device> DeviceAssociates { get; set; }

    }
}
