using System.Security.Principal;
using System.Web.Security;
using AppComponents;
using AppComponents.Raven;

namespace Shrike.UserManagement.BusinessLogic.Business
{
    using System;
    using System.IO;
    using System.Web;
    using System.Linq;
    using AppComponents.Web;
    using DAL.Manager;
    using Tenancy.Web;
    using Models;


    public class AccountBusinessLogic
    {
        private const string DefaultAccountIcon = "/Content/Theme/Default/Images/Command/user.png";
        private const string AvatarLocation = "/Application_Upload/uploads/Photos/";
        private UserManager _userManager = new UserManager();
        /// <summary>
        /// Retrieves the IPrincipal from ApplicationUser documents by using its principalId which should be unique
        /// </summary>
        /// <param name="principalId">unique id for a user for the hole system</param>
        /// <returns>The IPrincipal instance from ApplicationUser</returns>
        public IPrincipal GetUserFromPrincipalId(string principalId)
        {
            return _userManager.GetById(principalId);
        }

        public bool SuperAdminExist()
        {
            var owners = Roles.GetUsersInRole(DefaultRoles.SuperAdmin);
            return owners != null && owners.Any();
        }

        public Lok.Unik.ModelCommon.Client.User SuperAdmin()
        {
            return new Lok.Unik.ModelCommon.Client.User();
        }

        public void ValidateOwner(TakeOwnerShipModel model)
        {
            var owners = Roles.GetUsersInRole(DefaultRoles.SuperAdmin);
            if (owners != null && owners.Any())
            {
                throw new ApplicationException("Current application already has an owner");
            }

            //if (model.PassCode == Guid.Empty)
            if (string.IsNullOrEmpty(model.PassCode))
            {
                throw new ApplicationException(
                    "The value should be a GUID. For ex.: e9642097-7d56-49a8-a25e-316beb5feebf");
            }
        }

        public bool AddRoleToUser(TakeOwnerShipModel model, string userName)
        {
            var config = Catalog.Factory.Resolve<IConfig>();
            var takeOwnerShipPassCode = config["TakeOwnershipPassCode"];
            //if (model.PassCode == Guid.Parse(takeOwnerShipPassCode))
            if (!string.IsNullOrEmpty(model.PassCode) && model.PassCode == takeOwnerShipPassCode)
            {
                if (!Roles.RoleExists(DefaultRoles.SuperAdmin))
                {
                    Roles.CreateRole(DefaultRoles.SuperAdmin);
                }
                new UserManager().AddRoleToUser(userName, DefaultRoles.SuperAdmin, Roles.ApplicationName);

                return true;
            }
            return false;
        }

        public void UpdateUser(RegisterModel model, Invitation invitation)
        {
            if (invitation == null)
            {
                return;
            }
            invitation.AcceptingUser.ContactFirstName = model.FirstName;
            invitation.AcceptingUser.ContactLastName = model.LastName;

            new UserManager().UpdateUserData(invitation.AcceptingAppUserId, invitation);
        }

        public bool ValidateOpendIdUser(string username, string currentTenancy)
        {
            return _userManager.ValidateOpendIdUser(username, currentTenancy);
        }

        public string GetRoleIdByRoleName(string owner, string appName)
        {
            return new RoleManager().GetRoleIdByRoleName(owner, appName);
        }

        public PasswordReset GetResetPasswordByCode(string code)
        {
            return _userManager.GetResetPasswordByCode(code);
        }

        public bool ValidatePasscode(TakeOwnerShipModel model)
        {
            var config = Catalog.Factory.Resolve<IConfig>();
            var takeOwnerShipPassCode = config["TakeOwnershipPassCode"];
            //return model.PassCode == Guid.Parse(takeOwnerShipPassCode);
            return model.PassCode == takeOwnerShipPassCode;
        }
    }
}
