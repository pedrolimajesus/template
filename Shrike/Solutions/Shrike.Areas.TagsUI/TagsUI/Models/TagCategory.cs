
using System.Collections.Generic;

namespace Shrike.Areas.TagsUI.TagsUI.Models
{
    public class TagCategory
    {
        public string Name { get; set; }

        public string Color { get; set; }

        public string Category { get; set; }

        public bool Assign { get; set; }

    }

    public class DataTagUi
    {
        public string Id { get; set; }

        public string Entity { get; set; }

        public string PageRequest { get; set; }

        public IEnumerable<TagCategory> TagsCategories { get; set; }

        public IEnumerable<Tag> TagsEntity { get; set; }

        public IEnumerable<Tag> AllTags { get; set; }

        public DataTagUi()
        {
            TagsCategories =  new List<TagCategory>();
            TagsEntity =  new List<Tag>();
            AllTags =  new List<Tag>();
        }
    }
}