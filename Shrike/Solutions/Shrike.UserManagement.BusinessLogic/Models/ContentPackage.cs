using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Client;

namespace Shrike.UserManagement.BusinessLogic.Models
{
    public class ContentPackage
    {
        public ContentPackage()
        {
            Application = new Application();
            ContentItems = new Collection<ContentItem>();
            Tags = new Collection<Tags.UI.Models.Tag>();
            DevicesAssociates = new List<Device>();

        }

        public Guid Id { get; set; }

        [Required]
        [DisplayName("Name")]
        public string Name { get; set; }

        [Required]
        [DisplayName("Description")]
        public string Description { get; set; }

        [DisplayName("Applications")]
        public Application Application { get; set; }

        public ICollection<ContentItem> ContentItems { get; set; }

        public string CreatorPrincipalId { get; set; }

        [DisplayName("Create Date")]
        public DateTime CreationDate { get; set; }

        public ICollection<Tags.UI.Models.Tag> Tags { get; set; }

        public string ConfigurationRelativePath { get; set; }

        public IList<Device> DevicesAssociates { get; set; }
    }
}
