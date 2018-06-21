using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shrike.Areas.TagsUI.TagsUI.Models
{
    public class TagFilter
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public List<TagFilterValue> TagFilterValues { get; set; }
    }
}