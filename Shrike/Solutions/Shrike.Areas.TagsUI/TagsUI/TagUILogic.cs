using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using AppComponents.Web;
using Lok.Unik.ModelCommon.Interfaces;

namespace Shrike.Areas.TagsUI.TagsUI
{
    using AppComponents;
    using Lok.Unik.ModelCommon.Client;
    using Lok.Unik.ModelCommon.ItemRegistration;
    using Tag = Models.Tag;
    using Tag.BusinessLogic;
    using Models;
    using DAL.Manager;
    using log4net;

    public class TagUILogic
    {
        private readonly TagBusinessLogic _tagBusinessLogic;
        private readonly TagCategoryUILogic _tagCategoryUiLogic;
        private static readonly ILog _log = ClassLogger.Create(typeof (TagUILogic));

        public TagUILogic()
        {
            _tagBusinessLogic = new TagBusinessLogic();
            _tagCategoryUiLogic = new TagCategoryUILogic();
        }

        private IEnumerable<TagFilter> GetTagsFromUser()
        {
            var tags = GetCommonTagsExceptByCategoryColor<User>(new List<KnownColor>());
            var modelTagFilter =
                tags.Select(tagFilter => new TagFilter { Category = tagFilter.Category.Name, }).GroupBy(
                    x => x.Category);

            return
                modelTagFilter.Select(
                    tag =>
                    new TagFilter { Category = tag.Key, TagFilterValues = GetTagsByFilter(EntityType.User, tag.Key).ToList() }).
                    ToList();
        }

        private IEnumerable<TagFilter> GetTagsFromDeviceRegistration()
        {// TODO: review cal from UILogic directly to Manager
            var tags =
                new TagManager().GetAllTagByItemRegistration().Where(
                    tag => tag.Category.Color != KnownColor.Transparent).ToList();

            var modelTagFilter =
                tags.Select(tagFilter => new TagFilter { Category = tagFilter.Category.Name, }).GroupBy(
                    x => x.Category);

            return
                modelTagFilter.Select(
                    tag =>
                    new TagFilter
                    {
                        Category = tag.Key,
                        TagFilterValues = GetTagsByFilter(EntityType.ItemRegistration, tag.Key).ToList()
                    }).ToList();
        }
        
        public IEnumerable<TagFilterValue> GetTagsByFilter(EntityType entity, string attribute)
        {
            var fakeTags = _tagBusinessLogic.GetTagsByCategoryName(entity,attribute);

            //var fakeTags = new TagManager().GetAll(entity).Where(x => x.Category.Name == attribute).ToList();

            var filters =
                fakeTags.Select(
                    fakeTag =>
                    new TagFilterValue { Value = fakeTag.Value, Color = fakeTag.Category.Color, Id = fakeTag.Id.ToString() }).ToList();
            return filters;
        }

        #region Converters

        public static IEnumerable<Tag> ToModelTags(IEnumerable<Lok.Unik.ModelCommon.Client.Tag> entityTags)
        {
            return entityTags.Select(ToModelTags).ToList();
        }

        public static Tag ToModelTags(Lok.Unik.ModelCommon.Client.Tag tag)
        {
            var result = new Tag();
            if (tag != null)
                result = new Tag
                {
                    Id = tag.Id,
                    Name = tag.Attribute,
                    Category = tag.Category.Name,
                    Color = tag.Category.Color.ToString(),
                    Type = tag.Type.ToString()
                };
            return result;
        }

        public static IList<Lok.Unik.ModelCommon.Client.Tag> ToCommonTags(IEnumerable<Tag> modelTags, ApplicationUser creatorPrincipal)
        {
            return modelTags != null
                       ? modelTags.Select(modelTag => ToCommonTags(modelTag, creatorPrincipal)).ToList()
                       : new List<Lok.Unik.ModelCommon.Client.Tag>();
        }

        public static Lok.Unik.ModelCommon.Client.Tag ToCommonTags(Tag tag, ApplicationUser creatorPrincipal)
        {
            var result = new Lok.Unik.ModelCommon.Client.Tag();
            try
            {
                if (tag != null && !string.IsNullOrEmpty(tag.Name) && !string.IsNullOrEmpty(tag.Category))
                {
                    if (!string.IsNullOrEmpty(tag.Name.Trim()) && !string.IsNullOrEmpty(tag.Category.Trim()))
                        result = new Lok.Unik.ModelCommon.Client.Tag
                            {
                                Id = tag.Id,
                                Attribute = tag.Name,
                                Value = tag.Name,
                                CreateDate = tag.CreateDate,
                                Category = TagCategoryUILogic.ToCommonTagCategory(tag.Category, tag.Color),
                                Type =
                                    (TagType)
                                    Enum.Parse(typeof(TagType), tag.Type),

                                CreatorPrincipalId = creatorPrincipal == null ? String.Empty : creatorPrincipal.Id
                            };
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("{0} \n {1}", ex.ToString(), ex.InnerException == null ? string.Empty : "InnerException: " + ex.InnerException.ToString());
            }
            return result;
        }

        public static string ToModelTagType(string entity)
        {
            switch (entity)
            {
                case UITaggableEntity.User:
                    return TagType.User.ToString();
                case UITaggableEntity.ItemRegistration:
                    return TagType.ItemRegistration.ToString();

                default:
                    return
                        Enum.GetNames(typeof(TagType)).FirstOrDefault(
                            x => x.ToLowerInvariant().Equals(entity.ToLowerInvariant()));
            }
        }

        public static TagType ToCommonTagType(string tagType)
        {
            return (TagType)Enum.Parse(typeof(TagType), tagType);
        }

        public static object CreateGenericType(string className)
        {
            var assembly = typeof(User).Assembly;

            var thypeObj =
                assembly.GetTypes().FirstOrDefault(
                    x => x.Name.Equals(className, StringComparison.InvariantCultureIgnoreCase));

            //var fullAssembly = string.Format("{0}.{1}, {2}", thisNamespace, thypeObj.Name, assembly.GetName());
            var fullAssembly = string.Format("{0},{1}", thypeObj.FullName, thypeObj.Assembly.GetName());
            var obj = Activator.CreateInstance(Type.GetType(fullAssembly));
            return obj;
        }

        #endregion

        public IList<TagFilter> GetTagsFromEntities(string entityType)
        {
            var tags = new List<TagFilter>();

            switch (entityType)
            {
                case UITaggableEntity.User:
                    tags.AddRange(GetTagsFromUser());
                    break;

                case UITaggableEntity.ItemRegistration:
                    tags.AddRange(GetTagsFromDeviceRegistration());
                    break;
            }

            return tags;
        }

        public void DeleteTag(string id, string entity)
        {
            try
            {
                switch (entity)
                {
                    case UITaggableEntity.User:
                        _tagBusinessLogic.RemoveTags<User>(Guid.Parse(id));
                        break;
                    case UITaggableEntity.ItemRegistration:
                        _tagBusinessLogic.RemoveTags<ItemRegistration>(Guid.Parse(id));
                        break;

                    default:
                        var thisType = ToModelTagType(entity);
                        var genericType = CreateGenericType(thisType);
                        var method = typeof(TagUILogic).GetMethod("RemoveTags", new[] { typeof(Guid) });
                        var genericMethod = method.MakeGenericMethod(new[] { genericType.GetType() });
                        genericMethod.Invoke(this, new object[] { Guid.Parse(id) });
                        break;
                }

            }
            catch (Exception exception)
            {
                _log.ErrorFormat("An Exception occurred with the following message: {0}", exception.ToString());
            }
        }

        public DataTagUi NewSelectedTag(DataTagUi tagUi)
        {
            try
            {
                var tagUiResponse = new DataTagUi { Entity = tagUi.Entity };
                var categories = _tagCategoryUiLogic.GetAllTagCategories();
                tagUiResponse.TagsCategories = categories;

                var categoriesColor = new List<KnownColor> { KnownColor.Transparent };

                switch (tagUi.Entity)
                {
                    case UITaggableEntity.User:
                        tagUiResponse.AllTags = GetTagsExceptByCategoryColor<User>(categoriesColor);
                        var test = GetAllTagsByEntity<User>();
                        var userTags = GetAllTagsByEntity<User>(new Guid(tagUi.Id));
                        userTags = userTags.Where(t => t.Color != KnownColor.Transparent.ToString()).ToList();
                        tagUiResponse.Id = tagUi.Id;
                        tagUiResponse.TagsEntity = userTags;
                        break;

                    case UITaggableEntity.ItemRegistration:
                        tagUiResponse.AllTags = GetTagsExceptByCategoryColor<ItemRegistration>(categoriesColor);
                        var itemRegistrationTags = GetAllTagsByEntity<ItemRegistration>(new Guid(tagUi.Id));
                        userTags =
                            itemRegistrationTags.Where(t => t.Color != KnownColor.Transparent.ToString()).ToList();
                        tagUiResponse.Id = tagUi.Id;
                        tagUiResponse.TagsEntity = userTags;
                        break;

                    default:
                        var thisType = ToModelTagType(tagUi.Entity);
                        var genericType = CreateGenericType(thisType);
                        var getAllTagsByEntityMethod = typeof (TagUILogic).GetMethod("GetTagsExceptByCategoryColor",
                            new Type[] {typeof (List<KnownColor>)});
                        var genericMethodMaker =
                            getAllTagsByEntityMethod.MakeGenericMethod(new[] {genericType.GetType()});
                        tagUiResponse.AllTags =
                            ((IEnumerable<Tag>) genericMethodMaker.Invoke(this, new object[] {categoriesColor})).ToList();

                        var method = typeof (TagUILogic).GetMethod("GetAllTagsByEntity", new[] {typeof (Guid)});
                        var genericMethod = method.MakeGenericMethod(new[] {genericType.GetType()});
                        var methodInvoke = genericMethod.Invoke(this, new object[] {Guid.Parse(tagUi.Id)});
                        userTags =
                            ((List<Tag>) methodInvoke).Where(t => t.Color != KnownColor.Transparent.ToString()).ToList();
                        tagUiResponse.Id = tagUi.Id;
                        tagUiResponse.TagsEntity = userTags;
                        break;
                }

                return tagUiResponse;
            }
            catch (Exception exception)
            {
                if (exception.InnerException != null)
                {
                    _log.ErrorFormat("An Exception occurred with the following message: {0}", exception.ToString());
                    _log.ErrorFormat("\n InnerException: {0}", exception.InnerException.ToString());
                }
                else
                    _log.ErrorFormat("An Exception occurred with the following message: {0}", exception.ToString());
                return null;
            }
        }

        public void SaveNewSelectedTag(DataTagUi tagUi, string list, string name, string category, string type, ApplicationUser creatorPrincipal)
        {
            if (creatorPrincipal == null) throw new ArgumentNullException("creatorPrincipal");

            try
            {
                tagUi.AllTags = GetSelectedTag(type, list);
                tagUi.TagsCategories = GetNewTagCategory(name, category);

                var modelTags = tagUi.AllTags as Tag[] ?? tagUi.AllTags.ToArray();

                if (modelTags.Any())
                {
                    var entityTags = ToCommonTags(modelTags, creatorPrincipal);
                    var entityId = Guid.Parse(tagUi.Id);
                    var clear = true;

                    switch (tagUi.Entity)
                    {
                        case UITaggableEntity.User:
                            _tagBusinessLogic.AssignTags<User>(entityId, entityTags, clear);
                            break;

                        case UITaggableEntity.ItemRegistration:
                            _tagBusinessLogic.AssignTags<ItemRegistration>(entityId, entityTags, clear);
                            break;

                        default:
                            var thisType = ToModelTagType(tagUi.Entity);
                            var genericType = CreateGenericType(thisType);
                            var method = typeof(TagUILogic).GetMethod("AssignTags", new[] { typeof(Guid), typeof(IList<Lok.Unik.ModelCommon.Client.Tag>), typeof(bool) });
                            var genericMethod = method.MakeGenericMethod(new[] { genericType.GetType() });
                            var result = genericMethod.Invoke(this, new object[] { entityId, entityTags, clear });

                            break;
                    }

                }

                if (tagUi.TagsCategories.Count() != 0)
                {
                    var tagsCategory = tagUi.TagsCategories;
                    var tags = CollectTagCategories(tagUi.Entity, tagsCategory);

                    switch (tagUi.Entity)
                    {
                        case UITaggableEntity.User:
                            _tagBusinessLogic.AssignTags<User>(Guid.Parse(tagUi.Id), ToCommonTags(tags, creatorPrincipal), false);
                            break;

                        case UITaggableEntity.ItemRegistration:
                            _tagBusinessLogic.AssignTags<ItemRegistration>(Guid.Parse(tagUi.Id), ToCommonTags(tags, creatorPrincipal), false);
                            break;

                        default:
                            var thisType = ToModelTagType(tagUi.Entity);
                            var genericType = CreateGenericType(thisType);
                            var method = typeof(TagUILogic).GetMethod("AssignTags", new[] { typeof(Guid), typeof(IList<Lok.Unik.ModelCommon.Client.Tag>), typeof(bool) });
                            var genericMethod = method.MakeGenericMethod(new[] { genericType.GetType() });
                            genericMethod.Invoke(this, new object[] { Guid.Parse(tagUi.Id), ToCommonTags(tags, creatorPrincipal), false });
                            break;
                    }
                }
            }

            catch (Exception exception)
            {
                _log.ErrorFormat("An Exception occurred with the following message: {0} \n {1}", exception.ToString(), exception.InnerException == null ? string.Empty : "InnerException: " + exception.InnerException.ToString());
                throw new ApplicationException("Error saving new tag", exception);
            }
        }

        public IEnumerable<Tag> GetSelectedTag(string type, string list)
        {
            var namesTag = list.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
            var tagsComplete = new List<Tag>();
            var allTag = new List<Tag>();

            switch (type)
            {
                case UITaggableEntity.User:
                    allTag = GetTagsExceptByCategoryColor<User>(new List<KnownColor> { KnownColor.Transparent }).ToList();
                    break;

                case UITaggableEntity.ItemRegistration:
                    allTag =
                        GetAllTagsByEntity<ItemRegistration>().Where(t => t.Color != KnownColor.Transparent.ToString()).
                            ToList();
                    break;

                default:
                    var thisType = ToModelTagType(type);
                    var genericType = CreateGenericType(thisType);
                    var method = typeof(TagUILogic).GetMethod("GetAllTagsByEntity", Type.EmptyTypes);
                    var genericMethod = method.MakeGenericMethod(new[] { genericType.GetType() });
                    allTag = ((List<Tag>)genericMethod.Invoke(this, null)).Where(
                            t => t.Color != KnownColor.Transparent.ToString()).ToList();
                    break;
            }

            foreach (var tag in allTag)
            {
                tagsComplete.AddRange(from nametag in namesTag
                                      where !string.IsNullOrEmpty(nametag)
                                      where nametag.Equals(tag.Name, StringComparison.OrdinalIgnoreCase)
                                      select tag);
            }

            return tagsComplete;
        }

        private IEnumerable<Models.TagCategory> GetNewTagCategory(string name, string category)
        {
            var categories = new List<Models.TagCategory>();
            var allcategories = _tagCategoryUiLogic.GetAllTagCategories();
            var arrcategories = category.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var arrname = name.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < arrcategories.Length; i++)
            {
                if (!string.IsNullOrEmpty(arrcategories[i]))
                {
                    categories.AddRange(from tagCategory in allcategories
                                        let newCategory = arrcategories[i]
                                        let tagCategory1 = tagCategory.Name
                                        where newCategory.Equals(tagCategory1)
                                        select new Models.TagCategory
                                        {
                                            Name = arrname[i],
                                            Category = tagCategory.Name,
                                            Color = tagCategory.Color
                                        });
                }
            }
            return categories;
        }

        private static IEnumerable<Tag> CollectTagCategories(string entity, IEnumerable<Models.TagCategory> tagsCategories)
        {
            return tagsCategories.Select(tagsCategory => new Tag
            {
                Id = Guid.NewGuid(),
                Name = tagsCategory.Name,
                CreateDate = DateTime.UtcNow,
                Category = tagsCategory.Category,
                Color = tagsCategory.Color,
                Type = ToModelTagType(entity)
            }).ToList();
        }

        public void AssignTags<T>(Guid id, IList<Lok.Unik.ModelCommon.Client.Tag> tags, bool clear) where T : ITaggableEntity
        {
            _tagBusinessLogic.AssignTags<T>(id, tags, clear);
        }

        public IEnumerable<Tag> GetAllTagsByEntity<T>() where T : ITaggableEntity
        {
            var tags = _tagBusinessLogic.GetTags<T>();
            return ToModelTags(tags);
        }

        /// <summary>
        /// Returns all the Tags collection from the Entity T
        /// The Tags collection returned not includes the Tags
        /// that have his atribute Tag.Category.Color in the list invalidCategoriesColor
        /// </summary>
        /// <typeparam name="T">The entity that contains the Tags</typeparam>
        /// <param name="invalidCategoriesColor">The Tag.Category.Color that dont must to include in the resultset</param>
        /// <returns>All the Tags collection from the current Entity T received.</returns>
        public IEnumerable<Tag> GetTagsExceptByCategoryColor<T>(List<KnownColor> invalidCategoriesColor) where T : ITaggableEntity
        {
            var tags = _tagBusinessLogic.GetTagsExceptByCategoryColor<T>(invalidCategoriesColor);
            return tags.Select(ToModelTags);
        }

        public IEnumerable<Lok.Unik.ModelCommon.Client.Tag> GetCommonTagsExceptByCategoryColor<T>(List<KnownColor> invalidCategoriesColor) where T : ITaggableEntity
        {
            var tags = _tagBusinessLogic.GetTagsExceptByCategoryColor<T>(invalidCategoriesColor);
            return tags;
        }

        public IEnumerable<Tag> GetAllTagsByEntity<T>(Guid id) where T : ITaggableEntity
        {
            var tag = _tagBusinessLogic.GetTags<T>(id);
            return ToModelTags(tag);
        }

        public void RemoveTags<T>(Guid id) where T : ITaggableEntity
        {
            _tagBusinessLogic.RemoveTags<T>(id);
        }

        public DataTagUi GetAllDataTagUIInformation(string id, string entity)
        {
            var result = new DataTagUi();

            var tagUi = new DataTagUi
            {
                Id = id,
                Entity = entity
            };

            tagUi = NewSelectedTag(tagUi);
            if (tagUi != null)
                result = tagUi;

            if(result.TagsEntity==null)
                result.TagsEntity = new List<Tag>();

            return result;
        }

        public static bool EnableTagValidation()
        {
            bool result = false;
            try
            {
                var config = Catalog.Factory.Resolve<IConfig>();
            if (!Boolean.TryParse(config[CommonConfiguration.EnableControlNumberOfTagsValidation], out result))
                result = false;
            }
            catch(Exception ex)
            {
                result = false;

                if (ex.InnerException != null)
                    _log.Error(ex.ToString() + " \n InnerException: " + ex.InnerException.ToString());
                else
                    _log.Error(ex.ToString());
            }
            return result;
        }

    }
}