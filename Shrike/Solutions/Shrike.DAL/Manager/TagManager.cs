using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Shrike.DAL.Manager
{
    using AppComponents;
    using AppComponents.ControlFlow;
    using AppComponents.Raven;

    using ItemRegistration.DAL;

    using Lok.Unik.ModelCommon.ItemRegistration;

    using log4net;

    using Lok.Unik.ModelCommon.Client;
    using Lok.Unik.ModelCommon.Interfaces;

    using ModelCommon.RavenDB;

    using Raven.Client;
    using Raven.Client.Linq;

    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class TagManager
    {
        private readonly ILog _log = ClassLogger.Create(typeof(TagManager));
        private const string DefaultCategory = "DefaultCategory";

        public IEnumerable<Tag> GetAllTags()
        {
            using (ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q = from tag in session.Query<Tag>() select tag;
                    var list = q.ToList();
                    return list;
                }
            }
        }

        public IEnumerable<string> GetTagUsers(Guid tagId)
        {
            var um = new UserManager();
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q = from user in session.Query<User>() where user.Tags.Any(tag => tag.Id == tagId) select user;
                var users = q.ToArray();
                if (!users.Any())
                {
                    return new string[0];
                }
                return users.Select(user => um.GetById(user.Id).AppUser.UserName).ToList();
            }
        }

        public IList<Tag> GetAllTagByUser()
        {
            var users = new UserManager().GetAllCurrentTenancyUsers();
            var tagUser = users.Where(user => user.Tags != null).SelectMany(user => user.Tags).ToList();
            tagUser = tagUser.Distinct().ToList();
            return tagUser;
        }

        public IList<Tag> GetAllTagByItemRegistration()
        {
            var items = new ItemRegistrationManager().GetAll();
            var tags = new List<Tag>();

            foreach (var app in items.Where(app => !items.Contains(app)))
            {
                tags.AddRange(app.Tags);
            }
            return tags;
        }

        public IList<Tag> GetAllTagByItemRegistrationNonDefault()
        {
            var bag = new List<Tag>();

            try
            {
                using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    var tenancy = ContextRegistry.ContextsOf("Tenancy").First().Segments[1];
                    var aTagsListList =
                        session.Query<ItemRegistration>().Where(entity => entity.TenancyId == tenancy).ToList();

                    foreach (var aTag in
                        aTagsListList.Select(entity => entity.Tags).SelectMany(
                            aTagsList =>
                            aTagsList.Where(aTag => aTag.Category.Color != KnownColor.Transparent && !bag.Contains(aTag))))

                        bag.Add(aTag);
                }
            }

            catch (Exception ex)
            {
                if (ex.InnerException != null) _log.Error(ex + " InnerException: " + ex.InnerException);
                else _log.Error(ex.Message);
            }

            return bag;
        }

        #region Tagging entities

        private const string TagIdFormat = "tags/{0}";

        public void TagEntityWithTagId(ITaggableEntity entity, Guid tagId, IDocumentSession tenantSession)
        {
            TagEntityWithTagId(entity, tagId, tenantSession, tenantSession);
        }

        public void TagEntityWithTagId(
            ITaggableEntity entity, Guid tagId, IDocumentSession tenantSession, IDocumentSession entitySession)
        {
            var tag = tenantSession.Load<Tag>(string.Format(TagIdFormat, tagId));
            this.TagEntityWithTag(entity, tag, entitySession);
        }

        public void TagEntityWithTag(ITaggableEntity entity, Tag tag, IDocumentSession aSession)
        {
            entity.Tags.Add(tag);
            aSession.SaveChanges();
        }

        public void UntagEntityWithTagId(ITaggableEntity entity, Guid tagId, IDocumentSession tenantSession)
        {
            UntagEntityWithTagId(entity, tagId, tenantSession, tenantSession);
        }

        public void UntagEntityWithTagId(
            ITaggableEntity entity, Guid tagId, IDocumentSession tenantSession, IDocumentSession entitySession)
        {
            var tag = tenantSession.Load<Tag>(string.Format(TagIdFormat, tagId));
            this.UntagEntityFromTag(entity, tag, entitySession);
        }

        public void UntagEntityFromTag(ITaggableEntity entity, Tag tag, IDocumentSession aSession)
        {
            entity.Tags.Remove(tag);
            aSession.SaveChanges();
        }

        #endregion

        #region Assign Groups to Groups

        public void LinkGroupEntities(
            string linkName,
            Guid entitiesGroupLeftId,
            Guid entitiesGroupRightId,
            string creatorPrincipalId,
            IDocumentSession tenantSession)
        {
            var tagOne = tenantSession.Load<Tag>(string.Format(TagIdFormat, entitiesGroupLeftId));
            var tagTwo = tenantSession.Load<Tag>(string.Format(TagIdFormat, entitiesGroupRightId));
            LinkGroupEntities(linkName, tagOne, tagTwo, creatorPrincipalId, tenantSession);
        }

        public void LinkGroupEntities(
            string linkName,
            Tag entitiesGroupLeft,
            Tag entitiesGroupRight,
            string creatorPrincipalId,
            IDocumentSession tenantSession)
        {
            var groupLink = new GroupLink
                {
                    GroupOne = entitiesGroupLeft,
                    GroupTwo = entitiesGroupRight,
                    CreateDate = DateTime.UtcNow,
                    CreatorPrincipalId = creatorPrincipalId,
                    Name = linkName
                };
            tenantSession.Store(groupLink);
            tenantSession.SaveChanges();

        }

        private IEnumerable<SchedulePlan> GetSchedulesByTag(Tag entitiesGroupLeft)
        {
            var schedules = new List<SchedulePlan>();

            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var query = (from sq in session.Query<SchedulePlan>() select sq).ToList();

                    schedules.AddRange(
                        from schedulePlan in query
                        from tag in schedulePlan.Tags
                        where tag.FullPath.Equals(entitiesGroupLeft.FullPath)
                        select schedulePlan);

                    return schedules;
                }
            }
        }

        public void AddToLinkEntities(string linkName, Tag leftGroup, Tag rightGroup, string creatorPrincipalId)
        {
            using (ContextRegistry.NamedContextsFor(this.GetType())) using (var session = DocumentStoreLocator.ContextualResolve())
                LinkGroupEntities(linkName, leftGroup, rightGroup, creatorPrincipalId, session);
        }

        #endregion

        public void Update(Guid tagId, string category, string name)
        {
            using (ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var dbTag = session.Load<Tag>(tagId);
                    dbTag.Value = name;
                    dbTag.Category = new TagCategoryManager().GetCategoryByName(category);
                    if (!string.IsNullOrEmpty(dbTag.Value) && dbTag.Category != null)
                    {
                        session.SaveChanges();
                    }
                }
            }
        }

        public void FakeTags()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var query = session.Query<Tag>().ToArray();
                if (!query.Any())
                {
                    var arrayTags = CreateFakeTags();
                    foreach (var arrayTag in arrayTags)
                    {
                        session.Store(arrayTag);
                    }
                    session.SaveChanges();
                }
            }
        }

        private static IEnumerable<Tag> CreateFakeTags()
        {
            var tagArray = new[]
                {
                    new Tag
                        {
                            Attribute = "Site",
                            Category = new TagCategory { Color = KnownColor.Green, Name = "Site" },
                            Value = "Houston, TX",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Site",
                            Category = new TagCategory { Color = KnownColor.Green, Name = "Site" },
                            Value = "Woodland Park, CO",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Site",
                            Category = new TagCategory { Color = KnownColor.Green, Name = "Site" },
                            Value = "Cochabamba, BA",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Site",
                            Category = new TagCategory { Color = KnownColor.Green, Name = "Site" },
                            Value = "Colorado Springs, CO",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Site",
                            Category = new TagCategory { Color = KnownColor.Green, Name = "Site" },
                            Value = "Monument, CO",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Site",
                            Category = new TagCategory { Color = KnownColor.Green, Name = "Site" },
                            Value = "Cypress, TX",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "LocationType",
                            Category = new TagCategory { Color = KnownColor.Blue, Name = "LocationType" },
                            Value = "Entrance",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "LocationType",
                            Category = new TagCategory { Color = KnownColor.Blue, Name = "LocationType" },
                            Value = "Hub",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "LocationType",
                            Category = new TagCategory { Color = KnownColor.Blue, Name = "LocationType" },
                            Value = "EscalElev",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "LocationType",
                            Category = new TagCategory { Color = KnownColor.Blue, Name = "LocationType" },
                            Value = "Outside",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "LocationType",
                            Category = new TagCategory { Color = KnownColor.Blue, Name = "LocationType" },
                            Value = "Bathroom",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Model",
                            Category = new TagCategory { Color = KnownColor.Cyan, Name = "Model" },
                            Value = "Model A",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Model",
                            Category = new TagCategory { Color = KnownColor.Cyan, Name = "Model" },
                            Value = "Model B",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Model",
                            Category = new TagCategory { Color = KnownColor.Cyan, Name = "Model" },
                            Value = "Model C",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Model",
                            Category = new TagCategory { Color = KnownColor.Cyan, Name = "Model" },
                            Value = "Model D",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Model",
                            Category = new TagCategory { Color = KnownColor.Cyan, Name = "Model" },
                            Value = "Model E",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Location",
                            Category = new TagCategory { Color = KnownColor.Magenta, Name = "Location" },
                            Value = "West Hall",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Location",
                            Category = new TagCategory { Color = KnownColor.Magenta, Name = "Location" },
                            Value = "North Hub",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Location",
                            Category = new TagCategory { Color = KnownColor.Magenta, Name = "Location" },
                            Value = "South Hall",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Location",
                            Category = new TagCategory { Color = KnownColor.Magenta, Name = "Location" },
                            Value = "East Hub",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Location",
                            Category = new TagCategory { Color = KnownColor.Magenta, Name = "Location" },
                            Value = "Atrium",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Location",
                            Category = new TagCategory { Color = KnownColor.Magenta, Name = "Location" },
                            Value = "Central",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Style",
                            Category = new TagCategory { Color = KnownColor.Red, Name = "Style" },
                            Value = "Muted",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Style",
                            Category = new TagCategory { Color = KnownColor.Red, Name = "Style" },
                            Value = "Naval",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Style",
                            Category = new TagCategory { Color = KnownColor.Red, Name = "Style" },
                            Value = "Subtle",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Style",
                            Category = new TagCategory { Color = KnownColor.Red, Name = "Style" },
                            Value = "Fire",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        },
                    new Tag
                        {
                            Attribute = "Style",
                            Category = new TagCategory { Color = KnownColor.Red, Name = "Style" },
                            Value = "Green",
                            Id = Guid.NewGuid(),
                            Type = TagType.User
                        }
                };
            return tagArray;
        }

        public IEnumerable<Tag> GetFakeTags()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var query = from tag in session.Query<Tag>() select tag;
                return query.ToArray();
            }
        }

        public Tag GetTagByFilterAttr(string tagFilter)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var query = from tag in session.Query<Tag>() select tag;

                return query.FirstOrDefault(x => x.Value == tagFilter);
            }
        }

        public Tag GetTagByFilterAttr(string tagFilter, string tagCategoryColumn)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var query = from tag in session.Query<Tag>() select tag;

                var tagCategoryEntities = query.ToArray().Where(x => x.Attribute == tagCategoryColumn);

                return tagCategoryEntities.FirstOrDefault(u => u.Value == tagFilter);
            }
        }

        /// <summary>
        /// Get All Tags From Taggable Entities.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Tag> GetAll()
        {
            var tags = new List<Tag>();

            var userTags = GetAllTagByUser();
            tags.AddRange(userTags);

            var itemTags = GetAllTagByItemRegistration();
            tags.AddRange(itemTags);

            return tags;
        }

        public IEnumerable<Tag> GetAll(EntityType entity)
        {
            var tags = new List<Tag>();

            switch (entity)
            {
                case EntityType.User:
                    var userTags = GetAllTagByUser();
                    tags.AddRange(userTags);
                    break;

                case EntityType.ItemRegistration:
                    var dRegistrationTags = GetAllTagByItemRegistration();
                    tags.AddRange(dRegistrationTags);
                    break;
            }

            return tags;
        }

        public void RemoveTags<T>(Guid id) where T : ITaggableEntity
        {
            using (ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    ResolveEntity<T>(id, session);
                    session.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Resolve the Update Tags at Clean Action.
        /// </summary>
        /// <typeparam name="T">ModelCommon TaggableEntity</typeparam>
        /// <param name="id"></param>
        /// <param name="session"> </param>
        private static void ResolveEntity<T>(Guid id, IDocumentSession session) where T : ITaggableEntity
        {
            var genericType = typeof(T);
            Tag auxTag;

            if (genericType == typeof(User) || genericType == typeof(ItemRegistration))
            {
                using (var coreSession = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    var coreEntity = coreSession.Load<T>(id);

                    if (coreEntity != null)
                    {
                        auxTag = coreEntity.Tags.FirstOrDefault(t => t.Category.Color == KnownColor.Transparent);
                        coreEntity.Tags.Clear();
                        if(auxTag!=null)
                            coreEntity.Tags.Add(auxTag);
                    }
                    coreSession.SaveChanges();
                }
            }
            else
            {
                var entity = session.Load<T>(id);

                if (entity == null) return;

                auxTag = entity.Tags.FirstOrDefault(t => t.Category.Color == KnownColor.Transparent);
                entity.Tags.Clear();
                if (auxTag != null)
                {
                    entity.Tags.Add(auxTag);
                }
            }
        }

        /// <summary>
        /// Add Default Tag
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"> </param>
        /// <param name="id"> </param>
        /// <param name="creatorPrincipalId">creator</param>
        /// <returns></returns>
        public virtual Tag AddDefault<T>(string name, string id, string creatorPrincipalId) where T : ITaggableEntity
        {
            var taggableEntity = typeof(T);
            var tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    Category = new TagCategory { Name = DefaultCategory, Color = KnownColor.Transparent },
                    Attribute = name,
                    Value = id,
                    CreateDate = DateTime.UtcNow,
                    CreatorPrincipalId = creatorPrincipalId
                };

            if (taggableEntity == typeof(ItemRegistration))
            {
                tag.Type = TagType.ItemRegistration;
                return tag;
            }

            if (taggableEntity == typeof(User))
            {
                tag.Type = TagType.User;
                return tag;
            }

            return tag;
        }

        public void AssignTagsGeneric<T>(Guid id, IList<Tag> tagList, bool clear) where T : ITaggableEntity
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                AssignTagsGeneric<T>(id, tagList, clear, session);
            }
        }

        private void AssignTagsGeneric<T>(Guid id, IList<Tag> tagsList, bool clear, IDocumentSession session)
            where T : ITaggableEntity
        {
            var entity = session.Load<T>(id);
            this.AssignTagsGeneric<T>(tagsList, clear, entity, session);
        }

        private void AssignTagsGeneric<T>(IList<Tag> tagsList, bool clear, T entity, IDocumentSession session)
            where T : ITaggableEntity
        {
            var defaultTag =
                entity.Tags.FirstOrDefault(t => t.Category != null && t.Category.Color == KnownColor.Transparent);

            if (clear)
            {
                entity.Tags.Clear();
                if (defaultTag != null)
                {
                    entity.Tags.Add(defaultTag);
                }
            }

            //do not repeat tags on the list
            if (tagsList.Any())
            {
                foreach (var tag in tagsList.Where(tag => tag != null && !entity.Tags.Contains(tag)))
                {
                    entity.Tags.Add(tag);
                }
            }

            session.SaveChanges();
        }

        public void AssignTagsToUser(Guid id, IList<Tag> tagList, bool clear)
        {
            this.AssignTagsGeneric<User>(id, tagList, clear);
        }

        public void AssignTagsToItemRegistration(Guid id, IList<Tag> tagList, bool clear)
        {
            this.AssignTagsGeneric<ItemRegistration>(id, tagList, clear);
        }

        public void AssignTags<T>(Guid id, IList<Tag> tagsList, bool clear) where T : ITaggableEntity
        {
            using (ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    AssignTagsGeneric<T>(id, tagsList, clear, session);
                }
            }
        }

        public IList<Tag> GetAllTagsFrom<T>() where T : ITaggableEntity
        {
            var tagList = new List<Tag>();

            using (ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var query = session.Query<T>();

                    var allEntityWithTags = from entity in query
                                            where entity.Tags != null && entity.Tags.Any()
                                            select entity;

                    foreach (var currentEntity in allEntityWithTags)
                        AddTags(currentEntity, tagList);
                }
            }

            return tagList;
        }

        public IList<Tag> GetAllTagsFrom<T>(Guid id) where T : ITaggableEntity
        {
            var tagList = new List<Tag>();

            using (ContextRegistry.NamedContextsFor(GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var entity = session.Load<T>(id);

                    if (entity != null)
                        AddTags(entity, tagList);
                }
            }

            return tagList;
        }

        private static void AddTags<T>(T concrete, List<Tag> tagList) where T : ITaggableEntity
        {
            foreach (var tag in concrete.Tags.Where(tag => tag != null && !tagList.Contains(tag)))
            {
                tagList.Add(tag);
            }
        }

        /// <summary>
        /// Returns all the Tags from the Entity T 
        /// where the Tag.Category.Color not includes 
        /// the CategoriesColor from the array invalidCategories.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="invalidCategories"></param>
        /// <returns></returns>
        public List<Tag> GetTagsExceptByCategoryColor<T>(List<KnownColor> invalidCategories) where T : ITaggableEntity
        {
            using (ContextRegistry.NamedContextsFor(GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    if (typeof(IDevice).IsAssignableFrom(typeof(T)))
                    {
                        var subTypeEntity = session.Query<T, AllDeviceIndex>().ToList();
                        var oto =
                            subTypeEntity.SelectMany(
                                //z => z.Tags.Where(t => !t.Category.Color.In<CategoryColor>(invalidCategories))).ToList();
                                z => z.Tags.Where(t => !t.Category.Color.In(invalidCategories))).ToList();

                        return oto;
                    }

                    else if (typeof(T) == typeof(User) || typeof(T) == typeof(ItemRegistration))
                    {
                        using (var userSession = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                        {
                            var query = userSession.Query<T>().ToList();
                            var oto = query.SelectMany(z => z.Tags.Where(t => !t.Category.Color.In(invalidCategories))).Distinct().ToList();
                            return oto;
                        }
                    }

                    else
                    {
                        var query = session.Query<T>().ToList();
                        var oto = query.SelectMany(z => z.Tags.Where(t => !t.Category.Color.In(invalidCategories))).ToList();
                        return oto;
                    }


                }
            }
        }

        public IEnumerable<Tag> GetTagsByCategoryName(EntityType entity, string categoryName)
        {
            var tags = new List<Tag>();

            using (ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    switch (entity)
                    {
                        case EntityType.User:
                            {
                                var query = session.Query<User>().ToList();
                                tags.AddRange(query.SelectMany(z => z.Tags.Where(t => t.Category.Name == categoryName)).Distinct().ToList());
                            }

                            break;

                        case EntityType.ItemRegistration:
                            {
                                var query = session.Query<ItemRegistration>().ToList();
                                tags.AddRange(query.SelectMany(z => z.Tags.Where(t => t.Category.Name == categoryName)).Distinct().ToList());
                            }

                            break;
                    }
                }
            }

            return tags;
        }
    }

    public enum EntityType
    {
        User = 1,

        ItemRegistration = 2,
    }
}