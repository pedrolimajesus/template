using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Shrike.Areas.TagsUI.TagsUI.Models
{
    using Lok.Unik.ModelCommon.Client;

    public class TagFilterValue
    {
        public string Id { get; set; }
        public string Value { get; set; }
        //public CategoryColor Color { get; set; }
        public KnownColor Color { get; set; }
    }
}