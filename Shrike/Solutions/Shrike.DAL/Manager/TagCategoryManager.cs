using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AppComponents;
using AppComponents.Raven;
using Lok.Unik.ModelCommon.Client;

namespace Shrike.DAL.Manager
{
    using Raven.Client;

    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class TagCategoryManager
    {
        public void CreateDefaultCategories()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                if (session.Query<TagCategory>().ToList().Any())
                {
                    return;
                }

                var newCategories = new List<TagCategory>
                    {
                        new TagCategory { Color = KnownColor.Red, Name = "Style" },
                        new TagCategory { Color = KnownColor.Blue, Name = "LocationType" },
                        new TagCategory { Color = KnownColor.Green, Name = "Site" },
                        new TagCategory { Color = KnownColor.Cyan, Name = "Model" },
                        new TagCategory { Color = KnownColor.Magenta, Name = "Location" }
                    };

                AddTagCategories(newCategories, session);
            }
        }

        public TagCategory GetCategoryByName(string categoryName)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var tc = session.Load<TagCategory>(categoryName);
                return tc;
            }
        }

        public IEnumerable<TagCategory> GetAll()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                return session.Query<TagCategory>().ToArray();
            }
        }

        public IEnumerable<CategoryColor> GetColors()
        {
            var colors = Enum.GetValues(typeof(CategoryColor)).Cast<CategoryColor>().ToArray();
            return colors;
        }

        public IEnumerable<TagCategory> GetNotDefault()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var list = session.Query<TagCategory>().Where(tc => tc.Color != KnownColor.Transparent).ToArray();
                return list;
            }
        }

        public void AddTagCategory(TagCategory newCategory)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var tagCategoryName = newCategory.Name;

                //As far as I know, RavenDB uses a custom LowerCaseKeywordAnalyzer, so by default queries are case-insensitive. 
                var tc = session.Load<TagCategory>(tagCategoryName);
                if (tc != null) return;
                session.Store(newCategory);
                session.SaveChanges();
            }
        }

        public void AddTagCategories(IEnumerable<TagCategory> newCategories)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                AddTagCategories(newCategories, session);
            }
        }

        private void AddTagCategories(IEnumerable<TagCategory> newCategories, IDocumentSession session)
        {
            foreach (var newCategory in newCategories)
            {
                var tagCategoryName = newCategory.Name;

                var tc = session.Load<TagCategory>(tagCategoryName);
                if (tc == null)
                {
                    session.Store(newCategory);
                }
            }

            session.SaveChanges();
        }

        public bool IsUnique(TagCategory tagCategory)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                return
                    session.Query<TagCategory>().ToArray().FirstOrDefault(
                        x => x.Name.Equals(tagCategory.Name, StringComparison.InvariantCultureIgnoreCase)) == null;
            }

        }

        public void Create(TagCategory tagCategory)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                session.Store(tagCategory);
                session.SaveChanges();
            }
        }
    }
}