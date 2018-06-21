using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shrike.Dal.Tests
{
    using System.Collections.Generic;

    using Lok.Unik.ModelCommon.Client;

    using Shrike.DAL.Manager;

    [TestClass]
    public class TagCategoryManagerTest
    {
        private readonly TagCategoryManager manager = new TagCategoryManager();
        [TestInitialize]
        public void TestInitialize()
        {
            manager.CreateDefaultCategories();
        }

        [TestMethod]
        public void AddTagCategoryTest()
        {
            var name = "N" + Guid.NewGuid();

            manager.AddTagCategory(new TagCategory { Color = KnownColor.Transparent, Name = name });
            var category = manager.GetCategoryByName(name);

            Assert.IsNotNull(category);
            Assert.AreEqual(category.Name, name);
        }

        [TestMethod]
        public void AddTagCategoriesTest()
        {
            var name01 = "N" + Guid.NewGuid();
            var name02 = "N" + Guid.NewGuid();
            var categories = new List<TagCategory>
                {
                    new TagCategory { Color = KnownColor.Transparent, Name = name01 },
                    new TagCategory { Color = KnownColor.Red, Name = name02 }
                };
            
            manager.AddTagCategories(categories);

            var category01 = manager.GetCategoryByName(name01);

            Assert.IsNotNull(category01);
            Assert.AreEqual(category01.Name, name01);
            Assert.AreEqual(category01.Color, KnownColor.Transparent);

            var category02 = manager.GetCategoryByName(name02);

            Assert.IsNotNull(category02);
            Assert.AreEqual(category02.Name, name02);
            Assert.AreEqual(category02.Color, KnownColor.Red);
        }
    }
}