using System.Drawing;

namespace Shrike.Areas.TagsUI.TagsUI
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using Lok.Unik.ModelCommon.Client;

    using DAL.Manager;

    public class TagCategoryUILogic
    {
        private readonly TagCategoryManager _tagCategoryManager = new TagCategoryManager();

        public List<Models.TagCategory> GetAllTagCategories()
        {
            return (from tagCategory in (_tagCategoryManager.GetAll())
                    select new Models.TagCategory { Name = tagCategory.Name, Color = tagCategory.Color.ToString() }).
                ToList();
        }

        #region Converters

        public static TagCategory ToCommonTagCategory(string name, string color)
        {
            //return new TagCategory { Name = name, Color = (CategoryColor)Enum.Parse(typeof(CategoryColor), color) };
            return new TagCategory { Name = name, Color = (KnownColor)Enum.Parse(typeof(KnownColor), color) };
        }

        public static TagCategory ToCommonTagCategory(Models.TagCategory model)
        {
            return ToCommonTagCategory(model.Name, model.Color);
        }

        public static Models.TagCategory ToModelTagCategory(TagCategory tagCategory)
        {
            return new Models.TagCategory { Name = tagCategory.Name, Color = tagCategory.Color.ToString() };
        }

        #endregion

        public IEnumerable<TagCategory> ToCommonTagsCategory(IEnumerable<Models.TagCategory> tagsCategory)
        {
            return tagsCategory.Select(ToCommonTagCategory);
        }

        public IEnumerable<Models.TagCategory> GetNotDefault()
        {
            var entities = _tagCategoryManager.GetNotDefault();
            return entities.Select(ToModelTagCategory).ToArray();
        }

        public void AddTagCategory(Models.TagCategory newCategory)
        {
            _tagCategoryManager.AddTagCategory(ToCommonTagCategory(newCategory));
        }

        public bool IsUnique(string categoryName, KnownColor knownColor)
        {
            var tagCategory = new TagCategory
                {
                    Color = knownColor,
                    Name = categoryName,
                };

            return _tagCategoryManager.IsUnique(tagCategory);
        }

        public void Create(string categoryName, KnownColor knownColor)
        {
            var tagCategory = new TagCategory
                                  {
                                      Color = knownColor,
                                      Name = categoryName
                                  };

            _tagCategoryManager.Create(tagCategory);
        }
    }
}