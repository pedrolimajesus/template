using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.Security;
using AppComponents.ControlFlow;
using AppComponents.Raven;
using AppComponents.Web;
using Lok.Unik.ModelCommon.Client;
using ModelCommon.RavenDB;
using Raven.Client;


namespace Shrike.DAL.Manager
{
    using AppComponents;

    using Tenancy.DAL.Managers;

    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class UserManager
    {
        //just check if it exist for registering more data. It should be authenticated on openid provider
        public bool ValidateOpendIdUser(string username, string currentTenancy)
        {
            bool retValue;
            if (string.IsNullOrEmpty(username))
            {
                retValue = false;
            }
            else
            {
                HttpContext.Current.Items["ApplicationUser"] = null;

                using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    var user = (from u in session.Query<ApplicationUser>()
                                where
                                    u.UserName == username
                                    && u.Tenancy.Equals(currentTenancy, StringComparison.OrdinalIgnoreCase)
                                select u).SingleOrDefault();

                    if (user == null)
                    {
                        retValue = false;
                    }
                    else
                    {
                        //set the user on the Principal provider
                        HttpContext.Current.Items["ApplicationUser"] = user;
                        user.DateLastLogin = DateTime.UtcNow;
                        user.IsOnline = true;
                        user.FailedPasswordAttempts = 0;
                        user.FailedPasswordAnswerAttempts = 0;
                        session.SaveChanges();

                        retValue = true;
                    }
                }
            }

            return retValue;
        }

        public IEnumerable<User> GetAllCurrentTenancyUsers()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q =
                    from appUser in
                        session.Query<ApplicationUser>().Customize(x => x.Include<ApplicationUser>(u => u.ContainerId))
                    where appUser.Tenancy == TenantManager.CurrentTenancy
                    select appUser;

                var list = GetUsers(session, q);

                return list;
            }
        }

        public IEnumerable<User> GetAllUsers()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var query =
                    (from users in
                         session.Query<ApplicationUser>().Customize(x => x.Include<ApplicationUser>(u => u.ContainerId))
                     select users);
                var userList = GetUsers(session, query);

                return userList;
            }
        }

        public IEnumerable<User> GetAllValidUsers()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                IEnumerable<ApplicationUser> q;
                if (TenantManager.CurrentTenancy == DefaultRoles.SuperAdmin)
                {
                    var roleId = session.Query<ApplicationRole>().Where(ar => ar.Name == DefaultRoles.TenantOwner).
                        Select(ar => ar.Id).First();

                    q =
                        from appUser in
                            session.Query<ApplicationUser>().Customize(
                                x => x.Include<ApplicationUser>(u => u.ContainerId))
                        where
                            appUser.AccountRoles.Contains(roleId)
                            && (appUser.Status == UserStatus.Enabled || appUser.Status == UserStatus.Disabled
                             || appUser.Status == UserStatus.New)
                        select appUser;
                }
                else
                {
                    var loggedUser = HttpContext.Current != null ? HttpContext.Current.User as ApplicationUser : null;
                    if (loggedUser == null)
                    {
                        q =
                            from appUser in
                                session.Query<ApplicationUser>().Customize(
                                    x => x.Include<ApplicationUser>(u => u.ContainerId))
                            where
                                appUser.Tenancy == TenantManager.CurrentTenancy && (appUser.Status == UserStatus.Enabled
                                || appUser.Status == UserStatus.Disabled || appUser.Status == UserStatus.New)
                            select appUser;
                    }
                    else
                    {
                        q =
                            from appUser in
                                session.Query<ApplicationUser>().Customize(
                                    x => x.Include<ApplicationUser>(u => u.ContainerId))
                            where
                                appUser.Tenancy == TenantManager.CurrentTenancy
                                && (appUser.Status == UserStatus.Enabled
                                || appUser.Status == UserStatus.Disabled
                                || appUser.Status == UserStatus.New)
                                && appUser.PrincipalId != loggedUser.PrincipalId
                            select appUser;
                    }
                }

                var list = GetUsers(session, q);

                return list;
            }
        }

        private static IEnumerable<User> GetUsers(IDocumentSession session, IEnumerable<ApplicationUser> q)
        {
            var list = new List<User>();
            foreach (var applicationUser in q)
            {
                var userId = applicationUser.ContainerId;

                if (string.IsNullOrEmpty(userId))
                {
                    continue;
                }

                var aUser = session.Load<User>(userId);
                if (aUser == null) continue;

                aUser.AppUser = applicationUser;
                list.Add(aUser);
            }

            return list;
        }

        /// <summary>
        /// tenant user in all roles except tenant owner
        /// </summary>
        /// <returns></returns>
        public IEnumerable<User> GetTenantUsers()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q =
                    (from appUser in
                         session.Query<ApplicationUser>().Customize(x => x.Include<ApplicationUser>(u => u.ContainerId))
                     where appUser.Tenancy == TenantManager.CurrentTenancy
                     select appUser).ToList();

                var list = GetUsers(session, q);
                return list.ToList();
            }
        }

        public User GetByEmail(string email)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q =
                    from appUser in
                        session.Query<ApplicationUser>().Customize(x => x.Include<ApplicationUser>(u => u.ContainerId))
                    where appUser.Tenancy == TenantManager.CurrentTenancy && appUser.ContactEmail == email
                    select appUser;

                var list = GetUsers(session, q);

                return list.FirstOrDefault();
            }
        }

        public void SetStatusUserByPrincipalId(string appUserId)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var user =
                    session.Query<ApplicationUser>().FirstOrDefault(
                        i => i.PrincipalId.Equals(appUserId, StringComparison.InvariantCultureIgnoreCase));

                if (user != null)
                {
                    if (user.Status == UserStatus.Deleted)
                    {
                        user.Status = UserStatus.Invited;
                        session.SaveChanges();
                    }
                }
            }
        }

        public void SaveUser(User user, ApplicationUser creatorPrincipal)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                //check if two documents with a reference are stored.
                if (user.AppUser != null)
                {
                    session.Store(user.AppUser);
                    user.AppUserId = user.AppUser.PrincipalId;
                }

                //Add a default Tag at Create Action
                if (user.AppUser != null)
                    user.Tags.Add(new TagManager().AddDefault<User>(user.AppUser.UserName, user.Id.ToString(),
                                                                    creatorPrincipal.Id));

                //it should create an Id if it is empty
                session.Store(user);

                if (user.AppUser != null)
                {
                    user.AppUser.ContainerId = string.Format("Users/{0}", user.Id);
                }
                session.SaveChanges();
            }
        }

        public void RemoveAll()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                //check if two documents with a reference are stored.
                var all = from user in session.Query<User>() select user;
                foreach (var user in all)
                {
                    session.Delete(user);
                }

                var allAppUser = from user in session.Query<ApplicationUser>() select user;
                foreach (var user in allAppUser)
                {
                    session.Delete(user);
                }

                session.SaveChanges();
            }
        }

        public void UpdateUsername(User user, string newUsernameTest)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var aUser = GetCompleteUserQuery(user.Id);
                aUser.AppUser.UserName = newUsernameTest;
                session.SaveChanges();
            }
        }

        private User GetByInvitationId(Guid invitationId, IDocumentSession session)
        {
            var invitationDbId = new InvitationManager().GetInvitationId(invitationId);
            var aUser = from u in session.Query<User>() where u.InvitationId == invitationDbId select u;
            return aUser.FirstOrDefault();
        }

        /// <summary>
        /// Provides a way to Add additional User Fields into ApplicationUsers
        /// </summary>
        /// <param name="applicationUserId"></param>
        /// <param name="invitation"></param>
        public bool UpdateUserData(string applicationUserId, Invitation invitation)
        {
            if (string.IsNullOrEmpty(applicationUserId))
            {
                return false;
            }
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var applicationUser = session.Load<ApplicationUser>(applicationUserId);
                if (applicationUser == null)
                {
                    return false;
                }

                AddRoleToApplicationUser(invitation.Role, Roles.ApplicationName, applicationUser, session, false);

                applicationUser.Tenancy = invitation.Tenancy;
                applicationUser.Status = UserStatus.Enabled;

                if (invitation.AcceptingUser == null) invitation.AcceptingUser = applicationUser;
               
                applicationUser.ContactFirstName = invitation.AcceptingUser.ContactFirstName;
                applicationUser.ContactLastName = invitation.AcceptingUser.ContactLastName;

                var userWithTags = GetByInvitationId(invitation.Id, session);
                if (userWithTags != null)
                {
                    userWithTags.AppUser = applicationUser;
                    userWithTags.InvitationId = invitation.Id.ToString();
                }

                //Add or Update default Tag for grouping.
                var tempUserQuery = 
                    from u in session.Query<User>() 
                    where u.AppUserId == applicationUser.Id select u;

                var tempUser = tempUserQuery.First();

                if (tempUser.Tags == null)
                {
                    tempUser.Tags = new List<Tag>
                                        {
                                            new Tag
                                                {
                                                    Id = Guid.NewGuid(),
                                                    Type = TagType.User,
                                                    Attribute = applicationUser.UserName,
                                                    Value = tempUser.Id.ToString(),
                                                    Category =
                                                        new TagCategory
                                                            {
                                                                Name = "DefaultCategory",
                                                                Color = KnownColor.Transparent
                                                            },
                                                    CreateDate = applicationUser.DateCreated,
                                                }
                                        };
                }

                else
                {
                    var defaultTag = tempUser.Tags.FirstOrDefault(x => x.Category.Color == KnownColor.Transparent);
                    if (defaultTag != null)
                    {
                        defaultTag.Value = tempUser.Id.ToString();
                        defaultTag.Attribute = applicationUser.UserName;
                    }
                }

                var result = new InvitationManager().UpdateInvitationData(invitation, applicationUser, session, false);
                session.SaveChanges();
                return result;
            }
        }

        public void UpdateUserData(Guid id, User user)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var applicationUser = session.Load<ApplicationUser>(user.AppUser.Id);

                applicationUser.UserName = user.AppUser.UserName;
                applicationUser.ContactFirstName = user.AppUser.ContactFirstName;
                applicationUser.ContactLastName = user.AppUser.ContactLastName;
                applicationUser.ContactEmail = user.AppUser.ContactEmail;
                session.SaveChanges();
            }
        }

        public void AddRoleToUser(string applicationUserId, string roleName, string applicationName)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var applicationUser = session.Load<ApplicationUser>(applicationUserId);
                AddRoleToApplicationUser(roleName, applicationName, applicationUser, session);
            }
        }

        private void AddRoleToApplicationUser(
            string roleName,
            string applicationName,
            ApplicationUser applicationUser,
            IDocumentSession session,
            bool saveChanges = true)
        {
            var roleId = (from r in session.Query<ApplicationRole>()
                          where (r.Name == roleName || r.Id == roleName) && r.ApplicationName == applicationName
                          select r.Id).FirstOrDefault();

            if (applicationUser != null && !string.IsNullOrEmpty(roleId)
                && !applicationUser.AccountRoles.Contains(roleId))
            {
                applicationUser.AccountRoles.Add(roleId);
            }

            if (saveChanges)
            {
                session.SaveChanges();
            }
        }

        public User GetById(Guid userId, IDocumentSession session)
        {
            var aUser = GetCompleteUserQuery(userId);
            if (!string.IsNullOrEmpty(aUser.InvitationId))
            {
                var invitation = session.Load<Invitation>(aUser.InvitationId);

                if (invitation == null)
                {
                    using (
                        var oSession =
                            DocumentStoreLocator.ContextualResolve(UnikContextTypes.UnikTenantContextResourceKind))
                    {
                        invitation = oSession.Load<Invitation>(aUser.InvitationId);
                    }
                }

                aUser.Invitation = invitation;
            }

            return aUser;
        }

        public User GetById(Guid userId)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                return GetById(userId, session);
            }
        }

        public void Remove(Guid userId, ApplicationUser currentUser)
        {
            var aUser = GetCompleteUserQuery(userId);

            if (currentUser.Tenancy.Equals(DefaultRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {

                    if (!string.IsNullOrEmpty(aUser.InvitationId))
                    {
                        var invitation = session.Load<Invitation>(aUser.InvitationId);
                        invitation.Status = InvitationStatus.Deleted;
                        session.SaveChanges();
                    }

                    else
                    {
                        //    //no send invitation..
                        var invitation = new InvitationManager().GetInvitationByEmail(aUser.AppUser.ContactEmail);

                        if (invitation != null)
                        {
                            var current = session.Load<Invitation>(invitation.Id);
                            current.Status = InvitationStatus.Deleted;
                            session.SaveChanges();
                        }
                    }
                }
            }
            else
            {
                using (ContextRegistry.NamedContextsFor(GetType()))
                {
                    using (var sessionInv = DocumentStoreLocator.ContextualResolve())
                    {
                        if (!string.IsNullOrEmpty(aUser.InvitationId))
                        {
                            var invitation = sessionInv.Load<Invitation>(aUser.InvitationId);
                            invitation.Status = InvitationStatus.Deleted;
                            sessionInv.SaveChanges();
                        }
                    }
                }
            }
            using (var sessionUser = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                aUser.AppUser.Status = UserStatus.Deleted;
                aUser.AppUser.AccountRoles = null;
                var current = sessionUser.Load<ApplicationUser>(aUser.AppUser.Id);
                current.Status = aUser.AppUser.Status;
                sessionUser.SaveChanges();
            }
        }

        private static User GetCompleteUserQuery(Guid userId)
        {
            User aUser;
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                //aUser =
                //    session.Query<User>().Customize(x => x.Include<User>(u => u.AppUserId)).FirstOrDefault(
                //        x => x.Id == userId);
                aUser = session.Include("AppUserId").Load<User>(userId);

                // aUser = session.Load<User>(userId).Customize(x => x.Include<User>(u => u.AppUserId));
                if (aUser == null)
                {
                    throw new ApplicationException("User not found " + userId);
                }

                var appUser = session.Load<ApplicationUser>(aUser.AppUserId);
                //aUser.AppUser = session.Load<ApplicationUser>(aUser.AppUserId);

                if (appUser == null)
                {
                    throw new ApplicationException("Application User not found " + aUser.AppUserId);
                }

                aUser.AppUser = appUser;
            }
            return aUser;
        }

        public void EnableDisable(Guid id, string status)
        {
            //action Disable

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var aUser = GetCompleteUserQuery(id);
                aUser.AppUser.Status = status.Equals("Enabled") ? UserStatus.Enabled : UserStatus.Disabled;

                var current = session.Load<ApplicationUser>(aUser.AppUser.Id);
                current.Status = aUser.AppUser.Status;
                session.SaveChanges();
            }
        }

        public void ChangeRoleUser(Guid userId, string newRoleToChange, string userEmail, ApplicationUser changingUser)
        {
            var aUser = GetCompleteUserQuery(userId);
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var newRole = session.Load<ApplicationRole>(newRoleToChange);

                if (changingUser.Tenancy.Equals(DefaultRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
                {
                    //    //no send invitation..
                    var invitation = new InvitationManager().GetInvitationByEmail(userEmail);

                    if (invitation != null)
                    {


                        var current = session.Load<Invitation>(invitation.Id);
                        current.Role = newRole.Name;
                        session.SaveChanges();

                    }
                }
                else
                {
                    //tenant
                    var invitation = new InvitationManager().GetInvitationByEmail(aUser.AppUser.ContactEmail);

                    if (invitation != null)
                    {
                        using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
                        {
                            using (var sessionInv = DocumentStoreLocator.ContextualResolve())
                            {

                                var current = sessionInv.Load<Invitation>(invitation.Id);
                                current.Role = newRole.Name;
                                sessionInv.SaveChanges();
                            }
                        }
                    }
                }

                if (aUser.AppUser == null)
                {
                    return;
                }
                if (aUser.AppUser.AccountRoles == null)
                {
                    return;
                }


                var currentUser = session.Load<ApplicationUser>(aUser.AppUser.Id);

                if (newRole != null && !currentUser.AccountRoles.Contains(newRole.Id))
                {
                    //Changing current role by the new one
                    currentUser.AccountRoles.Clear();
                    currentUser.AccountRoles.Add(newRole.Id);
                }
                session.SaveChanges();

            }
        }

        public void NewTagMapping(Guid id, Tag newtag)
        {
            //var aUser = GetById(id);
            //using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            //{
            //    var tag = new Tag
            //    {
            //        Id = Guid.NewGuid(),
            //        Value = newtag.Name,
            //        CreateDate = DateTime.UtcNow,
            //        Attribute = newtag.Name,
            //        Category = new TagCategory
            //        {
            //            Color =
            //                (CategoryColor)
            //                Enum.Parse(typeof(CategoryColor), newtag.Color),
            //            Name = newtag.Category
            //        },
            //        Type = TagType.User
            //    };
            //    aUser.Tags.Add(tag);
            //    var current = session.Load<User>(aUser.Id);
            //    current.Tags = aUser.Tags;
            //    session.SaveChanges();
            //}


        }

        public void NewTagMapping(Guid id, string nameValue, string colorValue, string opValue)
        {
            var aUser = GetById(id);
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                Tag tag = new Tag();
                tag.Id = Guid.NewGuid();
                tag.Value = nameValue;
                tag.CreateDate = DateTime.UtcNow;
                tag.Attribute = nameValue;
                //tag.Category = new TagCategory { Color = (CategoryColor)Enum.Parse(typeof(CategoryColor), colorValue), Name = opValue };
                tag.Category = new TagCategory { Color = (KnownColor)Enum.Parse(typeof(KnownColor), colorValue), Name = opValue };
                aUser.Tags.Add(tag);
                var current = session.Load<User>(aUser.Id);
                current.Tags = aUser.Tags;
                session.SaveChanges();
            }
        }

        public void ListTagMapping(Guid id, List<string> select)
        {
            var aUser = GetById(id);
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var tags = GetTagsFromUser();
                var defaultTag = aUser.Tags.FirstOrDefault(t => t.Category.Color == KnownColor.Transparent);
                aUser.Tags.Clear();
                aUser.Tags.Add(defaultTag);
                if (select.Count != 0)
                {
                    foreach (var name in select)
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            aUser.Tags.Add(tags.FirstOrDefault(x => x.Value.Equals(name)));
                        }
                    }
                }

                var current = session.Load<User>(aUser.Id);
                List<Tag> userTag = aUser.Tags.Distinct().ToList();
                current.Tags = userTag;
                session.SaveChanges();
            }
        }

        private List<Tag> GetAllTag()
        {
            List<Tag> allTag = null;
            var allUser = GetAllCurrentTenancyUsers();
            allTag.AddRange(allUser.SelectMany(user => user.Tags));
            var result = allTag.Distinct().ToList();

            return result;

        }

        public List<Tag> GetTagsFromUser()
        {
            var users = GetAllCurrentTenancyUsers();
            var tags = new List<Tag>();
            if (users.Any())
            {
                tags.AddRange(users.Where(user => user.Tags != null).SelectMany(user => user.Tags));
                if (tags.Count > 1)
                {
                    tags = RemoveDuplicateTags(tags);
                }
            }
            return tags;
        }

        private static List<Tag> RemoveDuplicateTags(IEnumerable<Tag> tags)
        {
            var result1 = tags.Distinct().ToList();
            return result1;
        }

        public void RemoveSelectedTagsFromUser(Guid id)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var aUser = session.Load<User>(id);
                if (aUser != null)
                {
                    aUser.Tags.Clear();
                }
                session.SaveChanges();
            }
        }

        public void RemoveSelectedTagsFromUser(Guid id, IEnumerable<string> selected)
        {
            var aUser = GetById(id);
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                if (aUser != null)
                {
                    foreach (var tag in selected)
                    {
                        aUser.Tags.Remove(aUser.Tags.FirstOrDefault(x => x.Value == tag));
                    }
                }
                var current = session.Load<User>(aUser.Id);
                current.Tags = aUser.Tags;
                session.SaveChanges();
            }
        }

        public static void CreateIndexes()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var config = Catalog.Factory.Resolve<IConfig>();
                var dbName = config[CommonConfiguration.DefaultDataDatabase];
                IndexesManager.CreateIndexes(session.Advanced.DocumentStore, dbName, typeof(ApplicationUser_ByUserName));
            }
        }

        internal User GetSuperAdmin()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q =
                    from appUser in
                        session.Query<ApplicationUser>().Customize(x => x.Include<ApplicationUser>(u => u.ContainerId))
                    where appUser.Tenancy == DefaultRoles.SuperAdmin
                    select appUser;

                var list = GetUsers(session, q);

                return list.FirstOrDefault();
            }
        }

        public ApplicationUser GetSuperAdmin(string email)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q =
                    from appUser in
                        session.Query<ApplicationUser>().Customize(x => x.Include<ApplicationUser>(u => u.ContainerId))
                    where appUser.Tenancy == DefaultRoles.SuperAdmin && appUser.ContactEmail == email
                    select appUser;

                return q.ToArray().FirstOrDefault();
            }
        }

        public IEnumerable<User> GetUsersByRole(string role)
        {
            var users = GetAllCurrentTenancyUsers();
            return users.Where(user => user.AppUser.AccountRoles.Contains(role)).ToList();
        }

        public System.Security.Principal.IPrincipal GetById(string principalId)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var user = session.Load<ApplicationUser>(principalId);
                return user;
            }
        }

        public bool NotFoundUserWithEmail(string email)
        {

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var userFound =
                    session.Query<ApplicationUser>().Any(
                        au => au.ContactEmail == email && au.Status != UserStatus.Deleted);
                //it is already a case insensitive search
                return !userFound;
            }
        }

        public void SavePasswordReset(PasswordReset newPasswordReset)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                session.Store(newPasswordReset);
                session.SaveChanges();
            }
        }

        public PasswordReset GetResetPasswordByCode(string code)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var pr = session.Load<PasswordReset>(code);
                return pr;
            }
        }

    }
}