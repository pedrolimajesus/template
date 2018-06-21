using System;
using System.Collections.Generic;
using System.Linq;

namespace Shrike.ItemRegistration.BusinessLogic
{
    using AppComponents;
    using AppComponents.ControlFlow;

    using Lok.Unik.ModelCommon.Client;
    using Lok.Unik.ModelCommon.ItemRegistration;

    using Shrike.DAL.Manager;
    using DAL;
    using Lok.Unik.ModelCommon.Aware;

    public class ItemRegistrationBusinessLogic : IItemRegistrationAPI
    {
        private readonly ItemRegistrationManager _itemRegistrationManager = new ItemRegistrationManager();

        private readonly TagManager _tagManager = new TagManager();

        public ItemRegistrationResult RegisterItem<TItem>(string registrationCode, string description = null)
        {
            ItemRegistration itemRegistration;

            var result = _itemRegistrationManager.RegisterItem(registrationCode, out itemRegistration);
            if (result.Result == ResultCode.RegistrationAccepted)
            {
                var typeItemRegistrationManager = Catalog.Factory.Resolve<IItemRegistrationManager<TItem>>();
                if (!string.IsNullOrEmpty(description))
                    result.Description = description;

                typeItemRegistrationManager.RegisterItem(result, itemRegistration);
            }

            return result;
        }

        public bool AddItemRegistration(string name, string passcode, IList<Tag> itemTags)
        {
            var item = new ItemRegistration { PassCode = passcode, Name = name };
            if (itemTags != null) item.Tags = itemTags;
            var tenancy = ContextRegistry.ContextsOf("Tenancy").First().Segments[1];
            item.TenancyId = tenancy;
            return _itemRegistrationManager.SaveItemRegistration(item);
        }

        public bool AddItemRegistration(
            string name, string passcode,
            string type, IList<Tag> itemTags,
            IList<string> selectedTags, string facilityId = null)
        {
            return _itemRegistrationManager.SaveItemRegistration(name, passcode, type, itemTags, selectedTags, facilityId);
        }

        public void DeleteItemRegistration(Guid itemId)
        {
            _itemRegistrationManager.DeleteItemRegistration(itemId);
        }

        public IEnumerable<ItemRegistration> Get(string criteria = null, TimeCategories time = TimeCategories.All)
        {
            return _itemRegistrationManager.Get(criteria, time);
        }

        public ItemRegistration GetById(Guid itemId)
        {
            return _itemRegistrationManager.GetById(itemId);
        }

        public IEnumerable<Tag> GetTagsFromItemRegistration()
        {
            var tags = _tagManager.GetAllTagByItemRegistrationNonDefault();
            return tags;
        }

        public bool IsUnique(string name, string passCode)
        {
            return _itemRegistrationManager.IsUnique(name, passCode);
        }
    }
}