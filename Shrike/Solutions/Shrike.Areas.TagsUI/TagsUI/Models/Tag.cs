using System;
using System.Collections.Generic;

namespace Shrike.Areas.TagsUI.TagsUI.Models
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    public class Tag
    {
        [Required]
        [DisplayName("Value:")]
        public string Name { get; set; }

        [DisplayName("Category:")]
        public string Category { get; set; }

        [DisplayName("Color:")]
        public string Color { get; set; }

        [Required]
        [DisplayName("Type:")]
        public string Type { get; set; }

        public IEnumerable<string> Entities { get; set; }

        [DisplayName("Id:")]
        public Guid Id { get; set; }

        public DateTime CreateDate { get; set; }
    }
}
