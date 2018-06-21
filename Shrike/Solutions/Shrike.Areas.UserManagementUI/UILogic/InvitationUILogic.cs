using AppComponents;
using AppComponents.ControlFlow;
using AppComponents.Web;
using AppComponents.Web.Authentication;
using Lok.Unik.ModelCommon.Aware;
using Shrike.TimeFilter.DAL.Manager;
using Shrike.UserManagement.BusinessLogic.Business;
using Shrike.UserManagement.BusinessLogic.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shrike.Areas.UserManagementUI.UILogic
{
    public class InvitationUILogic
    {
        private readonly OwnerInvitationBusinessLogic invitationBusinessLogic = new OwnerInvitationBusinessLogic();

        private readonly UserBusinessLogic userBusinessLogic = new UserBusinessLogic();

        public OwnerInvitationModel GetInvitationModelByModelId(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var code = AuthorizationCode.UrlDecode(id);
            var invitation = invitationBusinessLogic.GetInvitationByAuthCode(code);
            return this.DbToModel(invitation);
        }

        #region Convertion Methods

        public OwnerInvitationModel DbToModel(Invitation invitation)
        {
            if (invitation == null) return null;

            return new OwnerInvitationModel
                {
                    Id = invitation.Id,
                    SentTo = invitation.SentTo,
                    Tenancy = invitation.Tenancy,
                    AuthorizationCode =
                        invitation.AuthorizationCode == null ? string.Empty : invitation.AuthorizationCode.UrlEncodedCode,
                    DateSent = invitation.DateSent,
                    AcceptingUserName =
                        invitation.AcceptingUser == null ? string.Empty : invitation.AcceptingUser.UserName,
                    ExpirationTime = invitation.ExpirationTime,
                    Status = invitation.Status,
                    ResentTimes = invitation.ResentTimes,
                    InvitingTenancy = invitation.InvitingTenant,
                    AcceptingUserId = invitation.AcceptingUserId,
                    Role = invitation.Role,
                    InvitingUserId = invitation.InvitingUserId
                };
        }

        public Invitation ModelToEntity(OwnerInvitationModel model)
        {
            if (model == null) return null;

            ApplicationUser acceptingAppUser = null;

            if (!string.IsNullOrEmpty(model.AcceptingUserId))
            {
                var id = model.AcceptingUserId.Split('/').Last();
                var guid = Guid.Parse(id);
                var acceptingUser = new UserBusinessLogic().GetUserEntityById(guid);
                acceptingAppUser = acceptingUser.AppUser;
            }

            return new Invitation
                {
                    Id = model.Id,
                    SentTo = model.SentTo,
                    AuthorizationCode =
                        new InvitationAuthCode { EmailedTo = model.SentTo, InvitingTenancy = model.InvitingTenancy },
                    DateSent = DateTime.UtcNow,
                    ExpirationTime = model.ExpirationTime,
                    ResentTimes = 0,
                    Status = model.Status,
                    Tenancy = model.Tenancy,
                    Role = model.Role,
                    InvitingTenant = model.InvitingTenancy,
                    AcceptingUser = acceptingAppUser
                };
        }

        #endregion

        public Invitation CreateInvitationEntityFromModel(OwnerInvitationModel model, DateTime expirationTime, ApplicationUser creatorPrincipal)
        {
            var email = model.SentTo;
            var userInvited = userBusinessLogic.GetByEmail(email);

            if (userInvited == null)
            {
                userInvited = new Lok.Unik.ModelCommon.Client.User
                    {
                        AppUser =
                            new ApplicationUser
                                {
                                    Tenancy = model.Tenancy,
                                    ContactEmail = model.SentTo,
                                    Status = UserStatus.Invited,
                                    DateCreated = DateTime.UtcNow,
                                    PrincipalId = string.Format("formsauthentication/{0}", Guid.NewGuid())
                                }
                    };

                userBusinessLogic.CreateNewUser(userInvited, creatorPrincipal);
            }

            var list = ContextRegistry.ContextsOf("Principal");
            var principalId = list.Single().LocalPath.TrimStart('/');

            var invitation = this.ModelToEntity(model);
            invitation.AcceptingUser = userInvited.AppUser;
            if (invitation.AuthorizationCode != null)
            {
                invitation.AuthorizationCode.ExpirationTime = expirationTime;
            }

            invitation.InvitingUserId = principalId;

            return invitation;
        }

        public string GetSuperAdminId()
        {
            return userBusinessLogic.GetSystemIOwnerId();
        }

        public IEnumerable<OwnerInvitationModel> GetInvitations(TimeCategories time)
        {
            var invitations = invitationBusinessLogic.GetAllOwnerInvitations();
            return this.GetInvitations(invitations, time);
        }

        public IEnumerable<OwnerInvitationModel> GetInvitations()
        {
            var invitations = invitationBusinessLogic.GetAllOwnerInvitations();
            return invitations == null ? null : invitations.Select(this.DbToModel).ToArray();
        }

        public IEnumerable<OwnerInvitationModel> GetInvitations(IEnumerable<Invitation> invitationList, TimeCategories time)
        {
            if (time == TimeCategories.All) return invitationList.Select(this.DbToModel);

            var aFunction = TimeFilterManager.GetTimeFilterDateComparison(time);
            var invitationsTime = new List<OwnerInvitationModel>();
            if (aFunction != null)
            {
                invitationsTime.AddRange(
                    from invitation in invitationList.ToList()
                    let timeDataSent = invitation.DateSent
                    where aFunction(timeDataSent)
                    select this.DbToModel(invitation));
            }
            return invitationsTime;
        }

        internal Invitation CreateDBInvitation(OwnerInvitationModel model, ApplicationUser creatorPrincipal)
        {
            var expirationTime = DateTime.UtcNow.AddDays(model.ExpirationTime);
            model.Status = InvitationStatus.New;

            var invitation = this.CreateInvitationEntityFromModel(model, expirationTime, creatorPrincipal);

            return invitationBusinessLogic.CreateInvitation(invitation);
        }

        internal Invitation CreateDBInvitation(OwnerInvitationModel model, string tenancy, ApplicationUser user)
        {
            var expirationTime = DateTime.UtcNow.AddDays(model.ExpirationTime);
            model.Status = InvitationStatus.New;

            var invitation = this.CreateInvitationEntityFromModel(model, expirationTime, user);

            return invitationBusinessLogic.CreateInvitation(invitation, tenancy);
        }

        public bool UpdateInvitation(OwnerInvitationModel model, string appUserId)
        {
            return invitationBusinessLogic.UpdateInvitation(this.ModelToEntity(model), appUserId);
        }

        public void UpdateInvitation(OwnerInvitationModel model)
        {
            invitationBusinessLogic.UpdateInvitation(model.SentTo, model.ExpirationTime, model.Tenancy);
        }

        private string GetEmailContent(OwnerInvitationModel invitation, Uri uri, string emailTemplateName)
        {
            return invitationBusinessLogic.GetEmailContent(invitation.SentTo, invitation.Tenancy, invitation.AuthorizationCode, uri, emailTemplateName);
        }

        public bool SendInvitationEmail(string email, Uri url, ApplicationUser currentUser)
        {
            return invitationBusinessLogic.SendInvitationEmail(email, url, currentUser);
        }

        public void SendInvitationEmail(string email, Uri url, ApplicationUser currentUser, string tenancy)
        {
            //it creates tenant also
            invitationBusinessLogic.SendInvitationEmail(email, url, currentUser, tenancy);
        }

        internal void InvitationRejected(OwnerInvitationModel invitation)
        {
            invitationBusinessLogic.InvitationRejected(this.ModelToEntity(invitation));
        }

        public IEnumerable<OwnerInvitationModel> GetInvitationsFrom(string id)
        {
            var list = invitationBusinessLogic.GetInvitationsFrom(id);
            return this.GetInvitations(list, TimeCategories.All);
        }

        internal IEnumerable<OwnerInvitationModel> GetInvitationsFrom(string id, TimeCategories time)
        {
            var list = invitationBusinessLogic.GetInvitationsFrom(id);
            return this.GetInvitations(list, time);
        }

        internal OwnerInvitationModel GetInvitationsModelById(Guid id, Uri url)
        {
            var invitation = invitationBusinessLogic.GetInvitationsModelById(id);
            var invitationModel = this.DbToModel(invitation);
            invitationModel.EmailContent = this.GetEmailContent(invitationModel, url, "sendInvitationDetails");
            return invitationModel;
        }

        internal void SendInvitationId(Guid guid, Uri uri, ApplicationUser applicationUser)
        {
            invitationBusinessLogic.SendInvitationId(guid, uri, applicationUser);
        }

        internal OwnerInvitationModel GetInvitationsModelById(Guid guid)
        {
            var invitation = invitationBusinessLogic.GetInvitationsModelById(guid);
            var im = this.DbToModel(invitation);
            im.AcceptingUserName = string.IsNullOrEmpty(im.AcceptingUserName) ? "-" : im.AcceptingUserName;
            return im;
        }

        public bool Remove(Guid invitationId)
        {
            return invitationBusinessLogic.DeleteInvitation(invitationId);
        }

        internal void CreateInvitation(OwnerInvitationModel newModel, Uri requestUrl, ApplicationUser user = null)
        {
            newModel.Role = "TenantOwner";
            newModel.InvitingTenancy = Tenants.SuperAdmin;

            invitationBusinessLogic.TestSmtpServer();

            //this controller always use views to send invitation to TenantOwner from the SuperAdmin
            var invitation = this.CreateDBInvitation(newModel, user);
            this.SendInvitationId(invitation.Id, requestUrl, invitation.AcceptingUser);
        }

        public bool NotFoundUserWithEmail(string email)
        {
            return userBusinessLogic.NotFoundUserWithEmail(email);
        }
    }
}