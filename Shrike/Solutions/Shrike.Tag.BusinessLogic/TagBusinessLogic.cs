using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Shrike.Tag.BusinessLogic
{
    using DAL.Manager;
    using Lok.Unik.ModelCommon.Client;
    using Lok.Unik.ModelCommon.Interfaces;
    using Lok.Unik.ModelCommon.ItemRegistration;
    using ItemRegistration.DAL;

    public class TagBusinessLogic
    {
        private readonly TagManager _tagManager;

        public TagBusinessLogic()
        {
            _tagManager = new TagManager();
        }

        #region Getters

        public IList<Tag> GetTags<T>() where T : ITaggableEntity
        {
            var taggableEntity = typeof(T);

            if (typeof(ITaggableEntity).IsAssignableFrom(taggableEntity))
            {
                if (taggableEntity == typeof(User))
                    return _tagManager.GetAllTagByUser();
                return taggableEntity == typeof(ItemRegistration) 
                    ? _tagManager.GetAllTagByItemRegistration() 
                    : _tagManager.GetAllTagsFrom<T>();
            }

            return new List<Tag>();
        }

        public IList<Tag> GetTags<T>(Guid id) where T : ITaggableEntity
        {
            var taggableEntity = typeof(T);

            if (typeof(ITaggableEntity).IsAssignableFrom(taggableEntity))
            {
                if (taggableEntity == typeof(User))
                {
                    var current = new UserManager().GetById(id);

                    if (current != null)
                        return current.Tags;
                }

                if (taggableEntity == typeof(ItemRegistration))
                {
                    var current = new ItemRegistrationManager().GetById(id);
                    if (current != null)
                        return current.Tags;
                }

                var entityTags = _tagManager.GetAllTagsFrom<T>(id);
                if (entityTags != null)
                    return entityTags;

            }

            return null;

        }

        #endregion

        #region Setters

        //public Tag AddDefault<T> (string name, string id) where T : ITaggableEntity
        //{
        //    return _tagManager.AddDefault<T>(name, id);
        //}

        public void AssignTags<T>(Guid id, IList<Tag> tags, bool clear) where T : ITaggableEntity 
        {
            var entityType = typeof(T);

            if (!typeof(ITaggableEntity).IsAssignableFrom(entityType)) return;

            if (entityType == typeof(User))
            {
                _tagManager.AssignTagsToUser(id, tags, clear);
            }
            else if (entityType == typeof(ItemRegistration))
            {
                _tagManager.AssignTagsToItemRegistration(id, tags, clear);
            }
            else
                _tagManager.AssignTags<T>(id, tags, clear);
        }

        public void AddToLinkEntities(string linkName, Tag leftGroup, Tag rightGroup, string creatorPrincipalId)
        {
            _tagManager.AddToLinkEntities(linkName, leftGroup, rightGroup, creatorPrincipalId);
        }

        #endregion

        #region Remove

        public void RemoveTags<T>(Guid id) where T : ITaggableEntity
        {
            if (typeof(ITaggableEntity).IsAssignableFrom(typeof(T)))
                _tagManager.RemoveTags<T>(id);
        }

        #endregion

        public IEnumerable<Tag> GetTagsExceptByCategoryColor<T>(List<KnownColor> invalidCategories) where T : ITaggableEntity
        {
            var taggableEntity = typeof(T);

            if (typeof(ITaggableEntity).IsAssignableFrom(taggableEntity))
            {
                return _tagManager.GetTagsExceptByCategoryColor<T>(invalidCategories);
            }

            return new List<Tag>();
        }

        public IEnumerable<Tag> GetTagsByCategoryName(EntityType entity, string categoryName)
        {
            return _tagManager.GetTagsByCategoryName(entity, categoryName);
        }

    }
}
