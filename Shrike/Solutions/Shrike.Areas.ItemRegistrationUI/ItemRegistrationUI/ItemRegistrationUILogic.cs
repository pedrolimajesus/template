using AppComponents.Web;
using Lok.Unik.ModelCommon.Aware;
using Shrike.ItemRegistration.BusinessLogic;
using Shrike.Areas;
using System;
using System.Collections.Generic;
using Shrike.Areas.TagsUI.TagsUI;

namespace Shrike.Areas.ItemRegistrationUI.ItemRegistrationUI
{
    public class ItemRegistrationUILogic
    {
        private readonly ItemRegistrationBusinessLogic _itemRegistrationBusinessLogic = new ItemRegistrationBusinessLogic();
        private readonly GroupLinkUILogic _groupLinkBusinessLogic = new GroupLinkUILogic();

        public IEnumerable<TagsUI.TagsUI.Models.Tag> GetTagsFromItemRegistration()
        {
            return TagUILogic.ToModelTags(_itemRegistrationBusinessLogic.GetTagsFromItemRegistration());
        }

        public IEnumerable<TagsUI.TagsUI.Models.GroupLink> GetListUserDevicesGroupLink()
        {
            return _groupLinkBusinessLogic.GetAllGroupLinksWithGroupTwoType(Lok.Unik.ModelCommon.Client.TagType.Device);
        }

        public IEnumerable<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration> Get(string criteria = null, TimeCategories time = TimeCategories.All)
        {
            return _itemRegistrationBusinessLogic.Get(criteria, time);
        }

        public Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration GetById(Guid itemId)
        {
            return _itemRegistrationBusinessLogic.GetById(itemId);
        }

        public void Delete(Guid id)
        {
            _itemRegistrationBusinessLogic.DeleteItemRegistration(id);
        }

        public void AddItemRegistration(
            string name, string passCode,
            string itemRegistrationType,
            IEnumerable<TagsUI.TagsUI.Models.Tag> newTags,
            IList<string> selectedTags,
            ApplicationUser creatorPrincipal,
            string facilityId = null)
        {
            var tagList = TagUILogic.ToCommonTags(newTags, creatorPrincipal);
            _itemRegistrationBusinessLogic.AddItemRegistration(name, passCode, itemRegistrationType, tagList,
                                                               selectedTags, facilityId);
        }


        public bool IsUnique(string name, string passCode)
        {
            return _itemRegistrationBusinessLogic.IsUnique(name, passCode);
        }
    }
}