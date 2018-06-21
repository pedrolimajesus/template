using AppComponents;
using AppComponents.ControlFlow;
using AppComponents.Extensions.EnumEx;
using AppComponents.Raven;
using Lok.Unik.ModelCommon.Aware;
using Lok.Unik.ModelCommon.Client;
using Lok.Unik.ModelCommon.ItemRegistration;
using Raven.Client;
using Shrike.TimeFilter.DAL.Manager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Shrike.ItemRegistration.DAL
{
    /// <summary>
    /// provides services for invites and registration of users, devices, and other items.
    /// </summary>
    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class ItemRegistrationManager
    {
        /// <summary>
        /// Registers an item on a previous invite.
        /// </summary>
        /// <param name="registrationCode"></param>
        /// <param name="itemRegistrationCode"></param>
        /// <returns>Result of the registration operation.</returns>
        public ItemRegistrationResult RegisterItem(string registrationCode, out Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration itemRegistrationCode)
        {
            var itemId = Guid.NewGuid();
            var retval = new Lok.Unik.ModelCommon.ItemRegistration.ItemRegistrationResult
                             {
                                 ItemId = itemId,
                                 PassCodeName = registrationCode,
                                 Result = ResultCode.RegistrationAccepted
                             };

            using (var coreSession = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
            {
                // get the item matching the given registration code
                itemRegistrationCode =
                    coreSession.Query<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration>().FirstOrDefault(dr => dr.PassCode == registrationCode);

                // not found? registration not accepted.
                if (null == itemRegistrationCode)
                {
                    retval.ItemId = Guid.Empty;
                    retval.AuthCode = string.Empty;
                    retval.Result = ResultCode.RegistrationInvalidPasscode;
                    return retval;
                }

                // save the item authorization code used to log it in
                var ac = new Lok.Unik.ModelCommon.ItemRegistration.AuthCode
                    {
                        Principal = itemId.ToString(),
                        AuthCodeId = Guid.NewGuid(),
                        Code = AuthorizationCode.GenerateCode(),
                        Tenant = itemRegistrationCode.TenancyId
                    };

                retval.AuthCode = ac.Code;
                retval.PassCodeId = itemRegistrationCode.Id;
                coreSession.Store(ac);
                coreSession.SaveChanges();
            }

            return retval;
        }

        /// <summary>
        /// Saves the item registration
        /// </summary>
        /// <param name="registrationCode"></param>
        /// <returns></returns>
        public bool SaveItemRegistration(Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration registrationCode)
        {
            using (var session = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
            {
                var itemRegistrationCode =
                    session.Query<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration>().FirstOrDefault(dr => dr.PassCode == registrationCode.PassCode);

                if (null != itemRegistrationCode) return false;

                registrationCode.TimeRegistered = DateTime.UtcNow;
                session.Store(registrationCode);
                session.SaveChanges();
                return true;
            }

        }

        /// <summary>
        /// saves the item registration with tags
        /// </summary>
        /// <param name="name"> </param>
        /// <param name="passcode"></param>
        /// <param name="type"></param>
        /// <param name="newTags"></param>
        /// <param name="selectTag"></param>
        /// <param name="facilityId"> </param>
        /// <returns></returns>
        public bool SaveItemRegistration(
            string name, string passcode,
            string type, IList<Tag> newTags,
            IList<string> selectTag, string facilityId = null)
        {
            using (var session = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
            {
                var tenancy = ContextRegistry.ContextsOf("Tenancy").First().Segments[1];

                var query = from entity in session.Query<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration>()
                            where entity.TenancyId == tenancy
                            select entity;

                // all item registrations in the tenant
                var dRegistration = query.ToArray();

                var oldTag = new List<Tag>();

                // list of existing tags matching the selection
                var oldTagFind = new List<Tag>();

                if (null != selectTag && selectTag.Count > 0)
                {
                    // record all distinct tags used for all registered items
                    oldTag.AddRange(dRegistration.SelectMany(deviceRegistration => deviceRegistration.Tags));
                    oldTag = oldTag.Distinct().ToList();

                    // for every tag selected
                    foreach (var tag2 in selectTag.Where(tag => !string.IsNullOrEmpty(tag)))
                    {
                        var tag3 = tag2;

                        // find all recorded tags matching the selection tags
                        foreach (var tag1 in oldTag.Where(tag1 => tag3.Equals(tag1.Value)))
                        {
                            oldTagFind.Add(tag1);
                            break;
                        }
                    }
                }

                oldTagFind = oldTagFind.Distinct().ToList();

                foreach (var old in oldTagFind)
                {
                    newTags.Add(old);
                }

                // retain all old tags, add in a grouping tag
                var dr = new Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration
                {
                    Id = Guid.NewGuid(),
                    TenancyId = tenancy,
                    PassCode = passcode,
                    Name = name,
                    Tags = newTags,
                    TimeRegistered = DateTime.UtcNow,
                    Type = type,
                    FacilityId = facilityId
                };

                //Add Default Tag for Grouping. 
                dr.Tags.Add(
                    new Tag
                    {
                        Id = Guid.NewGuid(),
                        Type = TagType.ItemRegistration,
                        Attribute = dr.Name,
                        Value = dr.PassCode,
                        CreateDate = DateTime.UtcNow,
                        Category = new TagCategory { Name = TagType.ItemRegistration.EnumName(), Color = KnownColor.Transparent }
                    });

                session.Store(dr);
                session.SaveChanges();
                return true;
            }
        }

        /// <summary>
        /// Get all registered items for the current tenant
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration> GetAll()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                return GetAll(session);
            }

        }

        /// <summary>
        /// Get all registered items using the given document session 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private static IEnumerable<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration> GetAll(IDocumentSession session)
        {
            var tenancy = ContextRegistry.ContextsOf("Tenancy").First().Segments[1];
            var query = from entity in session.Query<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration>() where entity.TenancyId == tenancy select entity;
            return query;
        }

        /// <summary>
        /// Gets item registrations with tags matching the given criteria and time category
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public IEnumerable<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration> Get(string criteria = null, TimeCategories time = TimeCategories.All)
        {
            var filteredItemRegistrationList = new List<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration>();

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var tenancy = ContextRegistry.ContextsOf("Tenancy").First().Segments[1];
                if (!string.IsNullOrEmpty(criteria))
                {
                    var query = from entity in session.Query<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration>()
                                where
                                    entity.TenancyId == tenancy && entity.Tags.Any(tag => tag.Id.ToString() == criteria)
                                select entity;

                    filteredItemRegistrationList.AddRange(query);
                }

                //filter by criteria over registered time
                if (time != TimeCategories.All)
                {
                    var registrations = from entity in session.Query<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration>()
                                        where entity.TenancyId == tenancy
                                        select entity;

                    var aFunction = TimeFilterManager.GetTimeFilterDateComparison(time);

                    return (from itemRegistration in registrations.ToArray()
                            let dateCreation = itemRegistration.TimeRegistered
                            where aFunction(dateCreation)
                            select itemRegistration).ToArray();
                }

                if (string.IsNullOrEmpty(criteria) && time == TimeCategories.All)
                    return GetAll(session);
            }

            return filteredItemRegistrationList;
        }

        /// <summary>
        /// Gets a registration item by id
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration GetById(Guid itemId)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                return session.Load<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration>(itemId);

            }
        }

        /// <summary>
        /// Updates the tag list on the identified registration item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="tags"></param>
        private void UpdateTagList(Guid itemId, IList<Tag> tags)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var current = session.Load<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration>(itemId);
                current.Tags = tags;
                session.SaveChanges();
            }
        }

        /// <summary>
        /// deletes an item registration.
        /// </summary>
        /// <param name="id"></param>
        public void DeleteItemRegistration(Guid id)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var current = session.Load<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration>(id);
                session.Delete(current);
                session.SaveChanges();
            }
        }

        public bool IsUnique(string name, string passCode)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var registrations = session.Query<Lok.Unik.ModelCommon.ItemRegistration.ItemRegistration>();

                if (!registrations.Any()) return true;

                return Enumerable.All(registrations,
                                      itemRegistration =>
                                      !itemRegistration.Name.Equals(name) &&
                                      !itemRegistration.PassCode.Equals(passCode));
            }
        }
    }
}