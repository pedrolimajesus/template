using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Shrike.UserManagement.BusinessLogic.Business;
using Shrike.UserManagement.BusinessLogic.Models;
using Shrike.Areas.TagsUI.TagsUI;

namespace Shrike.Areas.UserManagementUI.UserManagementUI
{
    using AppComponents.Web;

    using Shrike.Areas.UserManagementUI.UILogic;
    using Shrike.Areas.UserManagementUI.UserManagementUI.Models;
    using Shrike.DAL.Manager;
    using Lok.Unik.ModelCommon.Aware;
    using Shrike.TimeZone.Contracts;

    public class UserUILogic
    {
        private readonly UserBusinessLogic _userBusinessLogic;

        private readonly OwnerInvitationBusinessLogic _ownerInvitationBusinessLogic;

        private readonly TagUILogic _tagUILogic;

        private ITimeZoneService timeZoneService = AppComponents.Catalog.Factory.Resolve<ITimeZoneService>();
        public UserUILogic()
        {
            _userBusinessLogic = new UserBusinessLogic();
            _ownerInvitationBusinessLogic = new OwnerInvitationBusinessLogic();
            _tagUILogic = new TagUILogic();
        }

        public IEnumerable<User> GetAllUsers()
        {
            var list = _userBusinessLogic.GetAllUsers();
            return list.Select(this.EntityToModel);
        }

        private User EntityToModel(Lok.Unik.ModelCommon.Client.User dbUser)
        {
            var user = new User
                {
                    Username =
                        string.IsNullOrEmpty(dbUser.AppUser.UserName)
                            ? dbUser.AppUser.ContactEmail
                            : dbUser.AppUser.UserName,
                    Email = dbUser.AppUser.ContactEmail,
                    Tags = TagUILogic.ToModelTags(dbUser.Tags),
                    Roles = dbUser.AppUser.AccountRoles,
                    Id = dbUser.Id,
                    Status = dbUser.AppUser.Status,
                    DateCreated = timeZoneService.ConvertUtcToLocal(dbUser.AppUser.DateCreated, "MM/dd/yyyy HH:mm:ss"),
                    AdminOver = dbUser.AppUser.Tenancy
                };

            var relationshipAppUserId = dbUser.AppUser.ContainerId;
            user.RoleInvitation = GetRoleInvitationForUserId(relationshipAppUserId);

            return user;
        }

        private const string UserIdKey = "UserId";
        private string GetRoleInvitationForUserId(string relationshipAppUserId)
        {
            var context = HttpContext.Current;
            if (context == null)
            {
                return this._ownerInvitationBusinessLogic.GetRoleInvitationForUser(relationshipAppUserId);
            }

            var role = context.Items[UserIdKey + relationshipAppUserId] as string;
            if (string.IsNullOrEmpty(role))
            {
                role = this._ownerInvitationBusinessLogic.GetRoleInvitationForUser(relationshipAppUserId);
                context.Items[UserIdKey + relationshipAppUserId] = role;
            }
            return role;
        }

        public IEnumerable<User> GetAllValidUsers()
        {
            var listInvitations = _ownerInvitationBusinessLogic.AllInvitations();

            return
                this._userBusinessLogic.GetAllValidUsers().Select(
                    this.EntityToModel);
        }

        public bool NotFoundUserWithEmail(string email)
        {
            return _userBusinessLogic.NotFoundUserWithEmail(email);
        }

        public void SendInvitation(User user, string currentTenancy, Uri url, ApplicationUser currentUser)
        {
            var invitation = new OwnerInvitationModel
                {
                    Tenancy = currentTenancy,
                    SentTo = user.Email,
                    ExpirationTime = user.ExpirationTime,
                    Role = user.Roles.First(),
                    Status = InvitationStatus.New,
                    InvitingTenancy = currentTenancy
                };

            var ic = new InvitationUILogic();
            var invitationDb = ic.CreateDBInvitation(invitation, currentUser);

            user.Id = new Guid(invitationDb.AcceptingUserId.Split('/').Last());

            if (string.IsNullOrEmpty(user.Username))
            {
                user.Username = user.Email;
            }

            var success = ic.SendInvitationEmail(user.Email, url, currentUser);
        }

        public void SendInvitation(
            User user, string newTenancy, string invitingTenancy, Uri url, ApplicationUser applicationUser)
        {
            var invitation = new OwnerInvitationModel
                {
                    Tenancy = newTenancy,
                    SentTo = user.Email,
                    ExpirationTime = user.ExpirationTime,
                    Role = user.Roles.First(),
                    Status = InvitationStatus.New,
                    InvitingTenancy = invitingTenancy
                };

            var ic = new InvitationUILogic();
            var invitationDb = ic.CreateDBInvitation(invitation, newTenancy, applicationUser);

            user.Id = new Guid(invitationDb.AcceptingUserId.Split('/').Last());

            if (string.IsNullOrEmpty(user.Username))
            {
                user.Username = user.Email;
            }

            //once invitation is sent it creates the local tenant
            ic.SendInvitationEmail(user.Email, url, applicationUser, newTenancy);
        }

        internal bool IsAdmin(User user)
        {
            return _userBusinessLogic.IsAdmin(user.Roles);
        }

        internal void Remove(Guid guid, ApplicationUser currentUser)
        {
            _userBusinessLogic.Remove(guid, currentUser);
        }

        internal void EnableDisable(Guid id, string status)
        {
            _userBusinessLogic.EnableDisable(id, status);
        }

        internal void ChangeRoleUser(Guid userId, string roleToChangeTo, string userEmail, ApplicationUser changingUser)
        {
            _userBusinessLogic.ChangeRoleUser(userId, roleToChangeTo, userEmail, changingUser);
        }

        internal IEnumerable<User> GetAllValidUsers(TimeCategories time)
        {
            var allUsers = _userBusinessLogic.GetAllValidUsers(time);
            return allUsers.Select(EntityToModel);
        }

        public User GetUserById(Guid id)
        {
            var details = new UserManager().GetById(id);
            var userDetails = new User
                                  {
                                      Id = details.Id,
                                      Username = details.AppUser.UserName,
                                      FirstName = details.AppUser.ContactFirstName,
                                      LastName = details.AppUser.ContactLastName,
                                      Email = details.AppUser.ContactEmail,
                                      Roles = details.AppUser.AccountRoles,
                                      Tags = TagUILogic.ToModelTags(details.Tags)
                                  };

            return userDetails;
        }

        public void UpdateUser(Guid id, User user)
        {
            var userCommon = _userBusinessLogic.GetById(id);
            userCommon.AppUser.UserName = user.Username.Trim();
            userCommon.AppUser.ContactFirstName = user.FirstName.Trim();
            userCommon.AppUser.ContactLastName = user.LastName.Trim();
            userCommon.AppUser.ContactEmail = user.Email.Trim();
            _userBusinessLogic.UpdateUserData(id, userCommon);
        }
    }
}