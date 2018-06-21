using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Shrike.Areas.TagsUI.TagsUI.Models
{
    public class GroupLink
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "LinkName")]
        public string Name { get; set; }

        public DateTime CreateDate { get; set; }

        public string CreatorPrincipalId { get; set; }

        public Lok.Unik.ModelCommon.Client.Tag GroupOne { get; set; }
        public Lok.Unik.ModelCommon.Client.Tag GroupTwo { get; set; }

        #region UI

        public string LeftText { get; set; }
        public string RightText { get; set; }

        public List<Tag> LeftGroupTags { get; set; }
        public List<Tag> RightGroupTags { get; set; }

        #endregion

    }
}