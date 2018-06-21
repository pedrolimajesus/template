using System;
using System.Collections.Generic;
using Shrike.UserManagement.BusinessLogic.Business.NodeTestingProviders;

namespace Shrike.UserManagement.BusinessLogic.Business
{
    using AppComponents;

    using DAL.Manager;

    using AppComponents.Web;

    using Shrike.ExceptionHandling.Exceptions;
    using Shrike.Tenancy.Web;

    public class OwnerInvitationBusinessLogic
    {
        private readonly InvitationManager invitationManager = new InvitationManager();

        public Invitation GetInvitationByAuthCode(string code)
        {
            return string.IsNullOrEmpty(code) ? null : invitationManager.GetInvitationByAuthCode(code);
        }

        public Invitation GetInvitationByAppUserId(string code)
        {
            return invitationManager.GetInvitationByAppUserId(code);
        }

        public Invitation GetInvitationModelByEmail(string sentToEmail)
        {
            var invitation = invitationManager.GetInvitationByEmail(sentToEmail);
            return invitation;
        }

        public Invitation GetInvitationsModelById(Guid id)
        {
            var invitation = invitationManager.GetInvitationById(id);
            return invitation;
        }

        public bool InvitationRejected(Invitation invitation)
        {
            return invitationManager.RejectInvitation(invitation);
        }

        public Guid GetInvitationIdByEmail(string sentToEmail)
        {
            return invitationManager.GetInvitationIdByEmail(sentToEmail);
        }

        public bool DeleteInvitation(Guid id)
        {
            return invitationManager.DeleteById(id);
        }

        public bool UpdateInvitation(Invitation invitation, string appUserId)
        {
            return new UserManager().UpdateUserData(appUserId, invitation);
        }

        public void UpdateInvitation(string sentTo, int expirationTime, string tenancy)
        {
            invitationManager.UpdateInvitation(sentTo, expirationTime, tenancy);
        }

        public bool UserHasInvitation(string contactEmail, string tenancy, out string code)
        {
            return invitationManager.UserHasInvitation(contactEmail, tenancy, out code);
        }

        public IEnumerable<Invitation> AllInvitations()
        {
            return invitationManager.AllInvitations();
        }

        public IEnumerable<Invitation> GetAllOwnerInvitations()
        {
            return invitationManager.GetAllOwnerInvitations();
        }

        public Invitation CreateInvitation(Invitation invitation)
        {
            var config = Catalog.Factory.Resolve<IConfig>();
            var defaultDbName = config[CommonConfiguration.DefaultDataDatabase];

            if (defaultDbName.Equals(invitation.Tenancy, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new BusinessLogicException("The core database cannot be used as tenancy name.");
            }

            //Create the tenant when invitation has been sent (Only for SuperAdmin and TenantOwner Role).
            if (invitation.InvitingTenant == Tenants.SuperAdmin && invitation.Role == DefaultRoles.TenantOwner)
            {
                TenantHelper.GetOrCreate(invitation.Tenancy);
            }

            return invitationManager.CreateInvitation(invitation);
        }

        public Invitation CreateInvitation(Invitation invitation, string tenancy)
        {
            return invitationManager.CreateInvitation(invitation, tenancy);
        }

        public string GetEmailContent(string sentTo, string tenancy, string authorizationCode, Uri uri, string emailTemplateName)
        {
            return invitationManager.GetEmailContent(sentTo, tenancy, authorizationCode, uri, emailTemplateName);
        }

        public bool SendInvitationEmail(string email, Uri url, ApplicationUser currentUser)
        {
            return invitationManager.SendInvitationEmail(email, url, currentUser);
        }

        public void SendInvitationEmail(string email, Uri url, ApplicationUser currentUser, string tenancy)
        {
            invitationManager.SendInvitationEmail(email, url, currentUser, tenancy);
        }

        public IEnumerable<Invitation> GetInvitationsFrom(string id)
        {
            return invitationManager.GetInvitationsFrom(id);
        }

        public void SendInvitationId(Guid guid, Uri uri, ApplicationUser applicationUser)
        {
            invitationManager.SendInvitationId(guid, uri, applicationUser);
        }

        public string GetRoleInvitationForUser(string relationshipAppUserId)
        {
            return invitationManager.GetRoleInvitationForUser(relationshipAppUserId);
        }

        public void TestSmtpServer()
        {
            var emailServerConfig = new DeploymentBusinessLogic().GetEmailServerConfiguration();

            var server = emailServerConfig.SmtpServer ?? string.Empty;
            var port = emailServerConfig.Port;

            if (server.Contains(" "))
            {
                throw new BusinessLogicException("Please check the deployment configuration: " + server);
            }

            var emailServerTestingProvider = new EmailServerTestingProvider();
            emailServerTestingProvider.TestSmtpServer(server, port);
        }
    }
}