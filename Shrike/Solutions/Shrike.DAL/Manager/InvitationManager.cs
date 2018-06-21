namespace Shrike.DAL.Manager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AppComponents.Raven;
    using AppComponents.Web;
    using AppComponents.Web.Authentication;
    using AppComponents.Extensions.ExceptionEx;

    using ModelCommon.RavenDB;

    using Raven.Client;

    using AppComponents;

    using System.Globalization;

    using AppComponents.ControlFlow;

    using Lok.Unik.ModelCommon.Client;

    using Tenancy.DAL.Managers;

    using log4net;

    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class InvitationManager
    {
        private const string InvitationIdFormat = "Invitations/{0}";

        private readonly ILog _log = ClassLogger.Create(typeof(InvitationManager));

        private readonly IApplicationAlert _applicationAlert = Catalog.Factory.Resolve<IApplicationAlert>();

        public Invitation CreateInvitation(Invitation invitation)
        {
            using (var session = ReturnContext())
            {
                session.Store(invitation);
                var uId = invitation.AcceptingUserId;
                uId = uId.Split('/').LastOrDefault();

                AssingInvitationToUser(invitation, uId);

                session.SaveChanges();
            }
            return invitation;
        }

        public Invitation CreateInvitation(Invitation invitation, string tenancyName)
        {
            using (ContextRegistry.NamedContextsFor(GetType()))
            {
                using (
                    var session = (
                        DocumentStoreLocator.GetContextualTenancy() == Tenants.SuperAdmin ?
                        DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute) :
                        DocumentStoreLocator.ContextualResolve()
                        )
                    )
                {
                    session.Store(invitation);
                    var uId = invitation.AcceptingUserId;
                    uId = uId.Split('/').LastOrDefault();

                    AssingInvitationToUser(invitation, uId);

                    session.SaveChanges();
                }
            }

            return invitation;
        }

        private static void AssingInvitationToUser(Invitation invitation, string uId)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var manager = new UserManager();
                var user = manager.GetById(new Guid(uId), session);

                if (user != null)
                {
                    user.InvitationId = String.Format(InvitationIdFormat, invitation.Id);
                }

                session.SaveChanges();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idInv">id of the invitation that has the address that will receive the email</param>
        /// <param name="currentUrl">Used to create the email content url that will serve for accepting invitations</param>
        /// <param name="currentUser">the user sending the email</param>
        /// <returns></returns>
        public bool SendInvitationId(Guid idInv, Uri currentUrl, ApplicationUser currentUser)
        {
            using (var session = ReturnContext())
            {
                var invitation =
                    session.Query<Invitation>().FirstOrDefault(
                        i => i.Id == idInv && i.Status != InvitationStatus.Deleted);

                if (invitation == null)
                {
                    return false;
                }

                if (currentUrl != null)
                {
                    var emailSender = this.CreateEmailSender(
                        invitation.SentTo, invitation.Tenancy, invitation.AuthorizationCode.UrlEncodedCode, currentUrl);
                    emailSender.Send();
                }

                invitation.DateSent = DateTime.UtcNow;
                invitation.Status = invitation.ResentTimes > 0 ? InvitationStatus.ReSent : InvitationStatus.Sent;
                invitation.ResentTimes++;
                invitation.InvitingUserId = currentUser.PrincipalId;
                session.SaveChanges();

                new UserManager().SetStatusUserByPrincipalId(invitation.AcceptingAppUserId);
                return true;
            }
        }

        public bool SendInvitationEmail(string sendTo, Uri currentUrl, ApplicationUser currentUser)
        {
            using (var session = ReturnContext())
            {
                var invitation =
                    session.Query<Invitation>().FirstOrDefault(
                        i => i.SentTo == sendTo && i.Status != InvitationStatus.Deleted);

                //send owner invitation for being a customer admin
                if (invitation == null)
                {
                    return false;
                }

                if (currentUrl != null)
                {
                    var emailSender = this.CreateEmailSender(
                        invitation.SentTo, invitation.Tenancy, invitation.AuthorizationCode.UrlEncodedCode, currentUrl);
                    emailSender.Send();
                }

                invitation.DateSent = DateTime.UtcNow;
                invitation.Status = invitation.ResentTimes > 0 ? InvitationStatus.ReSent : InvitationStatus.Sent;
                invitation.ResentTimes++;
                invitation.InvitingUserId = currentUser.PrincipalId;
                session.SaveChanges();

                new UserManager().SetStatusUserByPrincipalId(invitation.AcceptingAppUserId);
                return true;
            }

        }

        public bool SendInvitationEmail(string sendTo, Uri currentUrl, ApplicationUser currentUser, string tenancy)
        {
            using (ContextRegistry.NamedContextsFor(GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var invitation =
                        session.Query<Invitation>().FirstOrDefault(
                            i => i.SentTo == sendTo && i.Status != InvitationStatus.Deleted);

                    if (invitation == null) return false;

                    if (currentUrl != null)
                    {
                        var emailSender = this.CreateEmailSender(
                            invitation.SentTo, invitation.Tenancy, invitation.AuthorizationCode.UrlEncodedCode, currentUrl);
                        emailSender.Send();
                    }

                    invitation.DateSent = DateTime.UtcNow;
                    invitation.Status = invitation.ResentTimes > 0 ? InvitationStatus.ReSent : InvitationStatus.Sent;
                    invitation.ResentTimes++;
                    invitation.InvitingUserId = currentUser.PrincipalId;
                    session.SaveChanges();

                    new UserManager().SetStatusUserByPrincipalId(invitation.AcceptingAppUserId);
                    return true;
                }
            }

        }

        private SendEmail CreateEmailSender(
            string sentTo, string tenancy, string invitationUrlEncodedCode, Uri requestUrl, string emailTemplateName = "sendInvitationOwner")
        {
            var config = Catalog.Factory.Resolve<IConfig>();
            var sender = config.Get(SendEmailSettings.EmailReplyAddress, String.Empty);
            var encodedCode = AuthorizationCode.UrlEncode(invitationUrlEncodedCode);

            var registrationUrl = String.Format(
                "{0}://{1}:{2}/{3}/UserManagementUI/Account/Register",
                requestUrl.Scheme,
                requestUrl.Host,
                requestUrl.Port,
                tenancy);

            var acceptInvitationUrl =
                String.Format(
                    "{0}://{1}:{2}/{3}/UserManagementUI/OwnerInvitation/AcceptInvitation/{4}",
                    requestUrl.Scheme,
                    requestUrl.Host,
                    requestUrl.Port,
                    tenancy,
                    encodedCode);

            var emailSender = SendEmail.CreateFromTemplateSMTP(
                sender,
                new[] { sentTo },
                emailTemplateName,
                tenancy,
                sentTo,
                acceptInvitationUrl,
                registrationUrl,
                encodedCode);
            return emailSender;
        }

        public string GetEmailContent(
            string invitationEmail, string invitationTenancy, string invitationCode, Uri currentUrl, string emailTemplateName)
        {
            var sender = this.CreateEmailSender(invitationEmail, invitationTenancy, invitationCode, currentUrl, emailTemplateName);
            return sender.Content;
        }

        public Invitation GetInvitationByEmail(string sentToEmail)
        {
            Invitation invitation;
            using (var session = ReturnContext())
            {
                invitation =
                    session.Query<Invitation>().FirstOrDefault(
                        i => i.SentTo.Equals(sentToEmail, StringComparison.InvariantCultureIgnoreCase));
            }
            return invitation;
        }

        public Invitation GetInvitationByAppUserId(string code)
        {
            var invitation = new Invitation();
            var invitations = AllInvitations();
            foreach (var invitation1 in invitations.Where(invitation1 => invitation1.AcceptingAppUserId.Equals(code)))
            {
                invitation = invitation1;
                break;
            }
            return invitation;
        }

        public string GetInvitationId(Guid invitationId)
        {
            return String.Format(InvitationIdFormat, invitationId);
        }

        public Invitation GetInvitationById(Guid id)
        {
            Invitation invitation;

            using (var session = ReturnContext())
            {
                invitation = session.Load<Invitation>(id);
                if (invitation != null) invitation.AcceptingUser = session.Load<ApplicationUser>(invitation.AcceptingAppUserId);
            }

            if (invitation == null)
            {
                if (TenantManager.CurrentTenancy.Equals(
                    DefaultRoles.SuperAdmin, StringComparison.InvariantCultureIgnoreCase))
                {
                    var tenants = new TenantManager().GetAllTenants();

                    foreach (var tenant in tenants)
                    {
                        using (var tenantSession = DocumentStoreLocator.Resolve(tenant.Site))
                        {
                            invitation = tenantSession.Load<Invitation>(id);
                        }
                        if (invitation != null) break;
                    }
                }
            }

            if (invitation != null
                &&
                (invitation.AcceptingUser == null
                 && !invitation.Role.Equals(DefaultRoles.SuperAdmin, StringComparison.InvariantCultureIgnoreCase)))
            {
                using (var coreSession = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    invitation.AcceptingUser = coreSession.Load<ApplicationUser>(invitation.AcceptingAppUserId);
                }
            }

            return invitation;
        }

        public Guid GetInvitationIdByEmail(string sentToEmail)
        {
            using (var session = ReturnContext())
            {
                return (from inv in session.Query<Invitation>()
                        where
                            inv.SentTo.Equals(sentToEmail, StringComparison.InvariantCultureIgnoreCase)
                            && inv.Status != InvitationStatus.Deleted
                        select inv.Id).FirstOrDefault();
            }
        }

        public bool DeleteById(Guid id)
        {
            using (var mainSession = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                using (var session = ReturnContext())
                {
                    var invitation =
                        session.Query<Invitation>().SingleOrDefault(i => i.Id == id && i.Status != InvitationStatus.Deleted);

                    if (invitation != null)
                    {
                        invitation.Status = InvitationStatus.Deleted;

                        var user = mainSession.Load<User>(invitation.AcceptingUserId);
                        var appUser = mainSession.Load<ApplicationUser>(user.AppUserId);

                        appUser.Status = UserStatus.Deleted;
                        mainSession.SaveChanges();

                        session.SaveChanges();
                        return true;
                    }
                }
            }
            return false;
        }

        private Invitation SearchInvitationByCode(string code, IDocumentSession session)
        {
            var invitation =
                session.Query<Invitation>().ToList().SingleOrDefault(
                    i => i.AuthorizationCode.Code == code && i.Status != InvitationStatus.Deleted);

            return invitation;
        }

        public Invitation GetInvitationByAuthCode(string code)
        {
            var invitingTenant = InvitationAuthCode.GetInvitingTenancyFromCode(code);
            if (!string.IsNullOrEmpty(invitingTenant))
            {
                Invitation invitation;

                using (
                    var session = invitingTenant.Equals(
                        DefaultRoles.SuperAdmin, StringComparison.InvariantCultureIgnoreCase)
                                      ? DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute)
                                      : DocumentStoreLocator.Resolve(
                                          string.Format(
                                              "{0}{1}/{2}",
                                              DocumentStoreLocator.SchemeRavenRoute,
                                              UnikContextTypes.UnikTenantContextResourceKind,
                                              invitingTenant)))
                {
                    invitation = SearchInvitationByCode(code, session);
                }

                return invitation;
            }

            return null;
        }

        /// <summary>
        /// Invitations can be made by the system owner or a manager of a tenant. Then invitations made by system owner will go to the core db
        /// and other invitations to the tenant db. Tenant will be acquired from the current logged user. Without a tenant core db will be used.
        /// </summary>
        /// <returns></returns>
        private IDocumentSession ReturnContext()
        {
            string tenancy = null;
            var tenanciesUris = ContextRegistry.ContextsOf("Tenancy");
            if (tenanciesUris.Any())
            {
                tenancy = tenanciesUris.First().Segments.LastOrDefault();
            }

            using (var ctx = ContextRegistry.NamedContextsFor(GetType()))
            {
                return string.IsNullOrEmpty(tenancy)
                       || DefaultRoles.SuperAdmin.Equals(tenancy, StringComparison.InvariantCultureIgnoreCase)
                           ? DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute)
                           : DocumentStoreLocator.ContextualResolve();
            }
        }

        private IDocumentSession ReturnContext(string tenancy)
        {
            using (var ctx = ContextRegistry.NamedContextsFor(GetType()))
            {
                return string.IsNullOrEmpty(tenancy)
                       || DefaultRoles.SuperAdmin.Equals(tenancy, StringComparison.InvariantCultureIgnoreCase)
                           ? DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute)
                           : DocumentStoreLocator.ContextualResolve();
            }
        }

        public void UpdateInvitation(string sentTo, int expirationTime, string tenancy)
        {
            using (var session = ReturnContext())
            {
                var invitation =
                    session.Query<Invitation>().SingleOrDefault(
                        i => i.SentTo == sentTo && i.Status != InvitationStatus.Deleted);

                if (invitation == null)
                {
                    return;
                }
                invitation.ExpirationTime = expirationTime;
                invitation.Tenancy = tenancy;
                session.SaveChanges();
            }
        }

        public bool RejectInvitation(Invitation invitation)
        {
            try
            {
                using (var session = ReturnContext())
                {
                    var invit = session.Load<Invitation>(invitation.Id);

                    if (invit != null)
                    {
                        if (invitation.AcceptingUser != null)
                        {
                            invit.AcceptingUser.Status = UserStatus.RejectInvitation;
                        }
                        invit.Status = InvitationStatus.Rejected;
                        session.SaveChanges();
                    }
                    else
                    {
                        var tenants = session.Query<Tenant>().ToArray();
                        foreach (Tenant tenant in tenants)
                        {
                            using (var tenantContext = DocumentStoreLocator.Resolve(tenant.Site))
                            {
                                var invitationTenant = tenantContext.Load<Invitation>(invitation.Id);

                                if (invitationTenant != null)
                                {
                                    if (invitationTenant.AcceptingUser != null)
                                    {
                                        invitationTenant.AcceptingUser.Status = UserStatus.RejectInvitation;
                                    }
                                    invitationTenant.Status = InvitationStatus.Rejected;
                                    tenantContext.SaveChanges();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("InvitationManager: An exception occurred with the following message: {0}", ex.Message);
                _applicationAlert.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());
            }
            return true;
        }

        public bool UpdateInvitationData(
            Invitation invitation, ApplicationUser appUserToUpdate, IDocumentSession session, bool saveChanges = true)
        {
            try
            {
                if (DefaultRoles.SuperAdmin.Equals(invitation.InvitingTenant))
                {
                    UpdateInvitationData(invitation, session, appUserToUpdate);
                }
                else
                {
                    using (ContextRegistry.NamedContextsFor(GetType()))
                    {
                        using (
                        var tenantSession =
                            DocumentStoreLocator.ContextualResolve())
                        {
                            var inv = tenantSession.Load<Invitation>(invitation.Id);
                            if (inv != null)
                            {
                                UpdateInvitationData(inv, tenantSession, appUserToUpdate);
                                tenantSession.SaveChanges();
                            }
                        }
                    }
                }

                if (saveChanges)
                {
                    session.SaveChanges();
                }

                return true;
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("InvitationManager: An exception occurred with the following message: {0}", ex.Message);
                _applicationAlert.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());
                return false;
            }
        }

        private static void UpdateInvitationData(
            Invitation invitation, IDocumentSession session, ApplicationUser appUserToUpdate)
        {
            invitation.AcceptingUser = appUserToUpdate;
            invitation.Status = InvitationStatus.Accepted;
            invitation.LastAcceptedDate = DateTime.UtcNow;

            var invitationsToDelete =
                session.Query<Invitation>().Where(
                    i =>
                    i.SentTo.Equals(invitation.SentTo, StringComparison.InvariantCultureIgnoreCase)
                    && i.Status != InvitationStatus.Accepted && i.Status != InvitationStatus.Deleted
                    && i.Tenancy == invitation.Tenancy).ToArray();
            if (!invitationsToDelete.Any())
            {
                return;
            }
            foreach (var toDelete in invitationsToDelete)
            {
                toDelete.Status = InvitationStatus.Active;
            }
        }

        public bool UserHasInvitation(string contactEmail, string tenancy, out string code)
        {
            bool existInvitation;
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                existInvitation = ExistValidInvitation(contactEmail, tenancy, session, out code);
            }

            if (!existInvitation)
            {
                if (!DefaultRoles.SuperAdmin.Equals(tenancy, StringComparison.InvariantCultureIgnoreCase))
                {
                    using (ContextRegistry.NamedContextsFor(GetType()))
                    {
                        using (var session1 = DocumentStoreLocator.ContextualResolve())
                        {
                            existInvitation = ExistValidInvitation(contactEmail, tenancy, session1, out code);
                        }
                    }
                }
            }

            return existInvitation;
        }

        private static bool ExistValidInvitation(
            string contactEmail, string tenancy, IDocumentSession session, out string code)
        {
            var invitation = (from o in session.Query<Invitation>()
                              where
                                  o.SentTo.Equals(contactEmail, StringComparison.OrdinalIgnoreCase)
                                  && o.Tenancy.Equals(tenancy, StringComparison.OrdinalIgnoreCase)
                                  && (o.Status == InvitationStatus.Sent || o.Status == InvitationStatus.ReSent)
                              select o).FirstOrDefault();

            var existInvitation = invitation != null;
            code = existInvitation ? invitation.AuthorizationCode.UrlEncodedCode : null;
            return existInvitation;
        }

        public IEnumerable<Invitation> GetAllOwnerInvitations()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q =
                    session.Query<Invitation>().Where(
                        invitation =>
                        invitation.Status != InvitationStatus.Deleted
                        &&
                        invitation.Role.Equals(DefaultRoles.TenantOwner, StringComparison.InvariantCultureIgnoreCase));

                return q.ToArray();
            }
        }

        public IEnumerable<Invitation> AllInvitations()
        {
            var tenancy = string.Empty;
            var tenancyUris = ContextRegistry.ContextsOf("Tenancy");
            var invitations = new List<Invitation>();

            if (tenancyUris.Any())
            {
                tenancy = tenancyUris.First().Segments.LastOrDefault();
            }

            if (!string.IsNullOrEmpty(tenancy)
                &&
                tenancy.Equals(
                    DefaultRoles.SuperAdmin.ToString(CultureInfo.InvariantCulture),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                var tenantCollection = new TenantManager().GetAllTenants();

                using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    var q =
                        session.Query<Invitation>().Where(invitation => invitation.Status != InvitationStatus.Deleted);
                    invitations.AddRange(q);
                }

                foreach (var tenant in tenantCollection)
                {
                    using (var globalSession = DocumentStoreLocator.Resolve(tenant.Site))
                    {
                        var q =
                            globalSession.Query<Invitation>().Where(
                                invitation => invitation.Status != InvitationStatus.Deleted);
                        invitations.AddRange(q);
                    }
                }

                return invitations;
            }

            using (var session = ReturnContext(tenancy))
            {
                var q = session.Query<Invitation>().Where(invitation => invitation.Status != InvitationStatus.Deleted);
                return q.ToArray();
            }
        }

        public IEnumerable<Invitation> GetInvitationsFrom(string id)
        {
            var tenancy = string.Empty;
            var tenancyUris = ContextRegistry.ContextsOf("Tenancy");
            var invitations = new List<Invitation>();

            if (tenancyUris.Any())
            {
                tenancy = tenancyUris.First().Segments.LastOrDefault();
            }

            if (!string.IsNullOrEmpty(tenancy)
                &&
                tenancy.Equals(
                    DefaultRoles.SuperAdmin.ToString(CultureInfo.InvariantCulture),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    var q =
                        session.Query<Invitation>().Where(
                            invitation =>
                            invitation.Status != InvitationStatus.Deleted && invitation.InvitingUserId == id);
                    invitations.AddRange(q);
                }

                var tenantCollection = new TenantManager().GetAllTenants();
                foreach (var tenant in tenantCollection)
                {
                    using (var globalSession = DocumentStoreLocator.Resolve(tenant.Site))
                    {
                        var q =
                            globalSession.Query<Invitation>().Where(
                                invitation =>
                                invitation.Status != InvitationStatus.Deleted && invitation.InvitingUserId == id);
                        invitations.AddRange(q);
                    }
                }

                return invitations;
            }

            using (var session = ReturnContext(tenancy))
            {
                var q =
                    session.Query<Invitation>().Where(
                        invitation => invitation.Status != InvitationStatus.Deleted && invitation.InvitingUserId == id);
                return q.ToArray();
            }
        }

        public string GetRoleInvitationForUser(string relationshipAppUserId)
        {
            string roleInvitation;

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                roleInvitation = GetRoleInvitationFromUserId(relationshipAppUserId, session);
            }

            if (roleInvitation == null
                &&
                !TenantManager.CurrentTenancy.Equals(
                    DefaultRoles.SuperAdmin,
                    StringComparison.OrdinalIgnoreCase))
            {
                using (ContextRegistry.NamedContextsFor(GetType()))
                {
                    using (var session = DocumentStoreLocator.ContextualResolve())
                    {
                        roleInvitation = GetRoleInvitationFromUserId(relationshipAppUserId, session);
                    }
                }
            }

            return roleInvitation;
        }

        private string GetRoleInvitationFromUserId(string relationshipAppUserId, IDocumentSession session)
        {
            var q =
                session.Query<Invitation>().Where(invitation => invitation.AcceptingUserId == relationshipAppUserId).
                    Select(inv => inv.Role);
            var roleInvitation = q.FirstOrDefault();
            return roleInvitation;
        }
    }
}