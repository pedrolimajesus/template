using System;
using System.Collections.Generic;
using System.Linq;
using Lok.Unik.ModelCommon.ItemRegistration;

namespace Shrike.Areas.TagsUI.TagsUI
{
    using Shrike.Tag.BusinessLogic;
    using Lok.Unik.ModelCommon.Client;
    using Models;

    public class GroupLinkUILogic
    {
        private readonly GroupLinkBusinessLogic _groupLinkBusinessLogic;

        public GroupLinkUILogic()
        {
            _groupLinkBusinessLogic = new GroupLinkBusinessLogic();
        }

        #region Setters

        public void AddToLinkEntities(Models.GroupLink groupLink, string groupOne, string groupTwo, string creatorPrincipalId, string type)
        {
           ResolveGroupLink(groupLink, groupOne, groupTwo, creatorPrincipalId, type);
        }

        #endregion

        #region Getters

        public IEnumerable<Models.GroupLink> GetAllGroupLinks()
        {
            return ToModelGroupLink(_groupLinkBusinessLogic.GetAllGroupLink());
        }

        public IEnumerable<Models.GroupLink> GetAllGroupLinks(string type)
        {
            var entityType = ResolveUIEntity(type);
            var links = new List<Models.GroupLink>();

            if (null != entityType)
            {
                if (entityType == typeof(User))
                    links.AddRange(ToModelGroupLink(_groupLinkBusinessLogic.GetAllGroupLinks<User>()));

                if(entityType == typeof(ItemRegistration))
                    links.AddRange(ToModelGroupLink(_groupLinkBusinessLogic.GetAllGroupLinks<ItemRegistration>()));
            }

            return links;
        }

        public Models.GroupLink GetAvailableGroup(string type, string leftText, string rightText)
        {
            var group = new Models.GroupLink
            {
                LeftText = string.IsNullOrEmpty(leftText) ? "Group One" : leftText,
                RightText = string.IsNullOrEmpty(rightText) ? "Group Two" : rightText
            };

            var tagLists = ResolveUIEntityGroup(type);
            if (!tagLists.Any()) return null;

            if (tagLists.Keys.First().Count > 0) group.LeftGroupTags = TagUILogic.ToModelTags(tagLists.Keys.ElementAt(0)).ToList();
            if (tagLists.Values.First().Count > 0) group.RightGroupTags = TagUILogic.ToModelTags(tagLists.Values.ElementAt(0)).ToList();

            return group;
        }

        //TODO Move to UI App
        private Dictionary<IList<Lok.Unik.ModelCommon.Client.Tag>, IList<Lok.Unik.ModelCommon.Client.Tag>> ResolveUIEntityGroup(string type)
        {
            var tagLists = new Dictionary<IList<Lok.Unik.ModelCommon.Client.Tag>, IList<Lok.Unik.ModelCommon.Client.Tag>>();

            switch (type)
            {
                case UITaggableEntity.User:
                    tagLists = _groupLinkBusinessLogic.GetAvailableGroup<User, Kiosk>();
                    break;

                //case UITaggableEntity.Application:
                //    tagLists = _groupLinkBusinessLogic.GetAvailableGroup<Application, Kiosk>(); 
                //    break;

                //case UITaggableEntity.ItemRegistration:
                //    tagLists = _groupLinkBusinessLogic.GetAvailableGroup<ItemRegistration, Kiosk>(); 
                //    break;

                //case UITaggableEntity.ContentPackage:
                //    tagLists = _groupLinkBusinessLogic.GetAvailableGroup<ContentPackage, Kiosk>(); 
                //    break;

                //case UITaggableEntity.SchedulePlan:
                //    _groupLinkBusinessLogic.GetAvailableGroup<SchedulePlan, Kiosk>(); 
                //    break;

                //case UITaggableEntity.Device:
                //    _groupLinkBusinessLogic.GetAvailableGroup<Kiosk, SchedulePlan>();
                //    break;
            }

            return tagLists;
        }

        //TODO: Move to UI App
        private void ResolveGroupLink(Models.GroupLink groupLink, string groupOne, string groupTwo, string creatorPrincipalId, string type)
        {
            switch (type)
            {
                case UITaggableEntity.User:
                    _groupLinkBusinessLogic.AddToLinkEntities<User, Kiosk>(ToCommonGroupLink(groupLink), groupOne, groupTwo, creatorPrincipalId);
                    break;

                //case UITaggableEntity.Application:
                //    _groupLinkBusinessLogic.AddToLinkEntities<Application, Kiosk>(ToCommonGroupLink(groupLink), groupOne, groupTwo, creatorPrincipalId);
                //    break;

                //case UITaggableEntity.ItemRegistration:
                //    _groupLinkBusinessLogic.AddToLinkEntities<ItemRegistration, Kiosk>(ToCommonGroupLink(groupLink), groupOne, groupTwo, creatorPrincipalId);

                //    break;

                //case UITaggableEntity.ContentPackage:
                //    _groupLinkBusinessLogic.AddToLinkEntities<ContentPackage, Kiosk>(ToCommonGroupLink(groupLink), groupOne, groupTwo, creatorPrincipalId);

                //    break;

                //case UITaggableEntity.SchedulePlan:
                //    _groupLinkBusinessLogic.AddToLinkEntities<SchedulePlan, Kiosk>(ToCommonGroupLink(groupLink), groupOne, groupTwo, creatorPrincipalId);

                //    break;

                //case UITaggableEntity.Device:
                //    _groupLinkBusinessLogic.AddToLinkEntities<Kiosk, SchedulePlan>(ToCommonGroupLink(groupLink), groupOne, groupTwo, creatorPrincipalId);

                //    break;
            }
        }

        private static Type ResolveUIEntity(string type)
        {
            switch (type)
            {
                case UITaggableEntity.User:
                    return typeof(User);

                case UITaggableEntity.ItemRegistration:
                    return typeof(ItemRegistration);
            }

            return null;
        }

        #endregion

        public void RemoveGroupLink(string groupId)
        {
            _groupLinkBusinessLogic.RemoveGroupLink(Guid.Parse(groupId));
        }

        #region Converters

        public static Models.GroupLink ToModelGroupLink(Lok.Unik.ModelCommon.Client.GroupLink groupLink)
        {
            return new Models.GroupLink
            {
                Id = groupLink.Id,
                Name = groupLink.Name,
                GroupOne = groupLink.GroupOne,
                GroupTwo = groupLink.GroupTwo,
                CreatorPrincipalId = groupLink.CreatorPrincipalId,
                CreateDate = groupLink.CreateDate
            };
        }

        public static IEnumerable<Models.GroupLink> ToModelGroupLink(IEnumerable<Lok.Unik.ModelCommon.Client.GroupLink> groupLinks)
        {
            return groupLinks.Select(link => new Models.GroupLink
            {
                Id = link.Id,
                Name = link.Name,
                GroupOne = link.GroupOne,
                GroupTwo = link.GroupTwo,
                CreatorPrincipalId = link.CreatorPrincipalId,
                CreateDate = link.CreateDate
            }).ToList();
        }

        public static Lok.Unik.ModelCommon.Client.GroupLink ToCommonGroupLink(Models.GroupLink groupLink)
        {
            return new Lok.Unik.ModelCommon.Client.GroupLink
            {
                Id = groupLink.Id,
                Name = groupLink.Name,
                GroupOne = groupLink.GroupOne,
                GroupTwo = groupLink.GroupTwo,
                CreatorPrincipalId = groupLink.CreatorPrincipalId,
                CreateDate = groupLink.CreateDate
            };
        }

        public static IList<Lok.Unik.ModelCommon.Client.GroupLink> ToCommonGroupLink(IEnumerable<Models.GroupLink> groupLinks)
        {
            return groupLinks.Select(link => new Lok.Unik.ModelCommon.Client.GroupLink
            {
                Id = link.Id,
                Name = link.Name,
                GroupOne = link.GroupOne,
                GroupTwo = link.GroupTwo,
                CreatorPrincipalId = link.CreatorPrincipalId,
                CreateDate = link.CreateDate
            }).ToList();

        }

        #endregion

        public bool ExistGroups(Models.GroupLink group)
        {
            return (group.LeftGroupTags != null && group.RightGroupTags != null);
        }

        public IEnumerable<Models.GroupLink> GetAllGroupLinksWithGroupTwoType(TagType tagType)
        {
            return ToModelGroupLink(_groupLinkBusinessLogic.GetAllGroupLinkWithGroupTwoType(tagType));
        }
    }
}
