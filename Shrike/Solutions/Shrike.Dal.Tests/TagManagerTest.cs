using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shrike.Dal.Tests
{
    using System.Collections.Generic;
    using System.Linq;

    using AppComponents;
    using AppComponents.Raven;

    using Lok.Unik.ModelCommon.Aware;
    using Lok.Unik.ModelCommon.Client;

    using Shrike.DAL.Manager;

    [TestClass]
    public class TagManagerTest
    {
        [TestMethod]
        public void GetAllTagsFromTest()
        {
            var manager = new TagManager();

            var allTagsOrig = manager.GetAllTagsFrom<Facility>();

            Tag newTag;
            var id = CreateFacilityWithTags(manager, out newTag);

            var allTagsNew = manager.GetAllTagsFrom<Facility>();
            Assert.IsNotNull(allTagsNew);
            Assert.IsTrue(allTagsNew.Any());
            Assert.IsTrue(allTagsNew.Contains(newTag));

            if (allTagsOrig!=null)
            {
                Assert.IsTrue(allTagsNew.Count==allTagsOrig.Count+1);
            }
        }


        [TestMethod]
        public void AssignTagsTest()
        {
            var manager = new TagManager();
            Tag newTag;
            var id = CreateFacilityWithTags(manager, out newTag);

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var ff = session.Load<Facility>(id);
                Assert.IsNotNull(ff);
                Assert.AreEqual(ff.Tags.Count(), 2);
                Assert.IsTrue(ff.Tags.Contains(newTag));
            }
        }

        private static Guid CreateFacilityWithTags(TagManager manager, out Tag newTag)
        {
            var id = Guid.NewGuid();
            var f = new Facility
                {
                    Id = id,
                    Tags =
                        new List<Tag>
                            {
                                new Tag
                                    {
                                        Id = Guid.NewGuid(),
                                        Category = new TagCategory { Color = KnownColor.Transparent, Name = "Default" },
                                        CreateDate = DateTime.UtcNow,
                                        Value = id.ToString(),
                                        Type = TagType.Facility
                                    }
                            }
                };

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                session.Store(f);
                session.SaveChanges();
            }

            var tcm = new TagCategoryManager();
            var category = tcm.GetNotDefault().First();

            var tags = f.Tags;
            newTag = new Tag
                {
                    Id = Guid.NewGuid(),
                    Category = category,
                    CreateDate = DateTime.UtcNow,
                    Value = "aValue",
                    Attribute = "Validation",
                    Type = TagType.Facility
                };
            tags.Add(newTag);

            manager.AssignTags<Facility>(id, tags, true);
            return id;
        }
    }
}