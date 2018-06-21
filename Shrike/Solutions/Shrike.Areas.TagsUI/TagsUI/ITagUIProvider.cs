using System.Collections.Generic;
using Lok.Unik.ModelCommon.Interfaces;
using Shrike.Areas.TagsUI.TagsUI.Models;

namespace Shrike.Areas.TagsUI.TagsUI
{
    public interface ITagUIProvider
    {
        void DeleteTag(string id, string entity);

        IEnumerable<Models.Tag> GetSelectedTag(string type, string list);

        DataTagUi NewSelectedTag(DataTagUi tagUi);

        void SaveNewSelectedTag(DataTagUi tagUi, string list, string name, string category, string type);
    }

    public interface ITagCategoryUIProvider
    {
        IEnumerable<TagCategory> GetNewTagCategory(string name, string category);
    }
}
