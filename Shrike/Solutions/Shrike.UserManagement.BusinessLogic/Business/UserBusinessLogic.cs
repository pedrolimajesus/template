using AppComponents;
using AppComponents.Web;
using Lok.Unik.ModelCommon.Aware;
using Lok.Unik.ModelCommon.Client;
using Shrike.DAL.Manager;
using Shrike.TimeFilter.DAL.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace Shrike.UserManagement.BusinessLogic.Business
{


    public class UserBusinessLogic
    {
        private readonly UserManager userManager;

        private readonly RoleManager roleManager;

        public UserBusinessLogic()
        {
            this.userManager = new UserManager();
            roleManager = new RoleManager();
        }

        public IEnumerable<User> GetAllUsers()
        {
            return userManager.GetAllUsers();
        }

        private const string AllValidUsersKey = "AllValidUsers";

        public IEnumerable<User> GetAllValidUsers()
        {
            var context = HttpContext.Current;

            if (context == null)
            {
                return userManager.GetAllValidUsers();
            }

            var users = (IEnumerable<User>)context.Items[AllValidUsersKey];

            if (users == null)
            {
                users = userManager.GetAllValidUsers();
                context.Items[AllValidUsersKey] = users;
            }

            return users;
        }

        public IEnumerable<User> GetAllValidUsers(TimeCategories time = TimeCategories.All)
        {
            var listUsers = GetAllValidUsers();
            if (time == TimeCategories.All)
            {
                return new List<User>(listUsers);
            }

            var aFunction = TimeFilterManager.GetTimeFilterDateComparison(time);

            return
                (from user in listUsers.ToArray()
                 let timeCreate = user.AppUser.DateCreated
                 where aFunction(timeCreate)
                 select user).ToArray();
        }

        public ApplicationUser GetSuperAdmin(string email)
        {
            var applicationUser = userManager.GetSuperAdmin(email);
            return applicationUser;
        }

        public User GetByEmail(string email)
        {
            var userInvited = userManager.GetByEmail(email);
            return userInvited;
        }

        public void CreateNewUser(User userInvited, ApplicationUser creatorPrincipal)
        {
            userManager.SaveUser(userInvited, creatorPrincipal);
        }

        public string GetSystemIOwnerId()
        {
            return roleManager.GetRoleIdByRoleName(DefaultRoles.SuperAdmin, Roles.ApplicationName);
        }

        public bool NotFoundUserWithEmail(string email)
        {
            return userManager.NotFoundUserWithEmail(email);
        }

        public bool IsAdmin(IEnumerable<string> roles)
        {
            return
                roles.Any(
                    role =>
                    role.ToLowerInvariant().Split('/').Last().Equals(DefaultRoles.TenantOwner.ToLowerInvariant()));
        }

        public IEnumerable<string> GetTenantRoles()
        {
            return RoleManager.TenantRoles;
        }

        public void Remove(Guid guid, ApplicationUser currentUser)
        {
            this.userManager.Remove(guid, currentUser);
        }

        public void EnableDisable(Guid id, string status)
        {
            this.userManager.EnableDisable(id, status);
        }

        public void ChangeRoleUser(Guid id, string roleToChange, string userEmail, ApplicationUser currentUser)
        {
            this.userManager.ChangeRoleUser(id, roleToChange, userEmail, currentUser);
        }

        public User GetUserEntityById(Guid guid)
        {
            return this.userManager.GetById(guid);
        }

        //for send new password
        public bool SendResetPasswordEmail(string sendTo, string principalId, Uri currentUrl, string tenancy)
        {
            if (currentUrl != null)
            {
                var emailSender = this.CreateEmailSenderResetPassword(sendTo, principalId, tenancy, currentUrl);
                emailSender.Send();
            }
            return true;
        }

        private SendEmail CreateEmailSenderResetPassword(
            string sentTo, string principalId, string tenancy, Uri requestUrl)
        {
            var config = Catalog.Factory.Resolve<IConfig>();
            var sender = config.Get(SendEmailSettings.EmailReplyAddress, String.Empty);
            const string emailTemplateName = "createNewPassword";

            var newPasswordReset = new PasswordReset { ContactEmail = sentTo, PrincipalId = principalId, ResetDateTime = DateTime.UtcNow, Tenancy = tenancy };
            var authCode = new AuthorizationCode();
            newPasswordReset.AuthCode = authCode;
            newPasswordReset.Code = authCode.UrlEncodedCode;

            this.userManager.SavePasswordReset(newPasswordReset);

            var createPasswordUrl = String.Format(
                "{0}://{1}:{2}/{3}/UserManagementUI/Account/ResetPassword/{4}",
                requestUrl.Scheme,
                requestUrl.Host,
                requestUrl.Port,
                tenancy,
                newPasswordReset.Code);

            var emailSender = SendEmail.CreateFromTemplateSMTP(
                sender, new[] { sentTo }, emailTemplateName, tenancy, sentTo, createPasswordUrl);
            return emailSender;
        }

        public User GetById(Guid id)
        {
            return this.userManager.GetById(id);
        }

        public void UpdateUserData(Guid id, User userCommon)
        {
            this.userManager.UpdateUserData(id, userCommon);
        }
    }
}