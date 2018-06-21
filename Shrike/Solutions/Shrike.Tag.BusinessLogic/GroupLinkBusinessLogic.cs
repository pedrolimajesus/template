using System;
using System.Collections.Generic;
using System.Linq;
using Lok.Unik.ModelCommon.Interfaces;

namespace Shrike.Tag.BusinessLogic
{
    using DAL.Manager;
    using Lok.Unik.ModelCommon.Client;

    public class GroupLinkBusinessLogic
    {
        private readonly GroupLinkManager _groupLinkManager;
        private readonly TagBusinessLogic _tagBusinessLogic;

        public GroupLinkBusinessLogic()
        {
            _groupLinkManager = new GroupLinkManager();
            _tagBusinessLogic = new TagBusinessLogic();
        }

        #region Getters

        public IEnumerable<GroupLink> GetAllGroupLink()
        {
            return _groupLinkManager.GetAllGroupLink();
        }

        public IList<GroupLink> GetAllGroupLinks<T>() where T : ITaggableEntity
        {
            var links = new List<GroupLink>();
            var entityTags = _tagBusinessLogic.GetTags<T>();
            var allgroups = GetAllGroupLink();

            foreach (var groupLink in allgroups)
            {
                links.AddRange(from tag in entityTags
                               where tag.Id == groupLink.GroupOne.Id
                                   || tag.Id == groupLink.GroupTwo.Id
                               select groupLink);
            }

            return links;
        }

        public Dictionary<IList<Tag>, IList<Tag>> GetAvailableGroup<TOne, TTwo>()
            where TOne : ITaggableEntity
            where TTwo : ITaggableEntity
        {
            var one = _tagBusinessLogic.GetTags<TOne>();
            var two = _tagBusinessLogic.GetTags<TTwo>();

            var toGroup = new Dictionary<IList<Tag>, IList<Tag>>(){ { one, two } };

            return toGroup;

        }

        #endregion

        public void RemoveGroupLink(Guid guid)
        {
            _groupLinkManager.RemoveLink(guid);
        }

        public void AddToLinkEntities<TOne, TTwo>(GroupLink groupLink, string groupOne , string groupTwo ,string creatorPrincipalId)
            where TOne : ITaggableEntity
            where TTwo : ITaggableEntity
        {
            var entityLeftTag = _tagBusinessLogic.GetTags<TOne>().FirstOrDefault(tag => tag.Id == Guid.Parse(groupOne));
            var entityRightTag = _tagBusinessLogic.GetTags<TTwo>().FirstOrDefault(tag => tag.Id == Guid.Parse(groupTwo));

            _tagBusinessLogic.AddToLinkEntities(groupLink.Name, entityLeftTag, entityRightTag, creatorPrincipalId);
        }

        public IEnumerable<GroupLink> GetAllGroupLinkWithGroupTwoType(TagType tagType)
        {
            return _groupLinkManager.GetAllGroupLinkWithGroupTwoType(tagType);
        }
    }
}
