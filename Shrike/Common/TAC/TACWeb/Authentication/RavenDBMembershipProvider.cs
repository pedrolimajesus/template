using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Web.Configuration;
using System.Diagnostics;
using System.Web;

using AppComponents.Raven;

namespace AppComponents.Web
{
    using global::Raven.Client;

    public enum PasswordAttemptKinds
    {
        PasswordAttempt,

        PasswordAnswerAttempt
    }

    public class RavenDBMembershipProvider : MembershipProvider
    {
        #region Private Members

        private const string ProviderName = "RavenDBMembershipProvider";

        private int _maxInvalidPasswordAttempts;

        private int _passwordAttemptWindow;

        private int _minRequiredNonAlphanumericCharacters;

        private int _minRequiredPasswordLength;

        private string _passwordStrengthRegularExpression;

        private bool _enablePasswordReset;

        private bool _enablePasswordRetrieval;

        private bool _requiresQuestionAndAnswer;

        private bool _requiresUniqueEmail;

        private MembershipPasswordFormat _passwordFormat;

        private string _hashAlgorithm;

        private string _validationKey;

        

        #endregion

        #region Overriden Public Members

        public override string ApplicationName { get; set; }

        public override bool EnablePasswordReset
        {
            get
            {
                return _enablePasswordReset;
            }
        }

        public override bool EnablePasswordRetrieval
        {
            get
            {
                return _enablePasswordRetrieval;
            }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get
            {
                return _maxInvalidPasswordAttempts;
            }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get
            {
                return _minRequiredNonAlphanumericCharacters;
            }
        }

        public override int MinRequiredPasswordLength
        {
            get
            {
                return _minRequiredPasswordLength;
            }
        }

        public override int PasswordAttemptWindow
        {
            get
            {
                return _passwordAttemptWindow;
            }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get
            {
                return _passwordFormat;
            }
        }

        public override string PasswordStrengthRegularExpression
        {
            get
            {
                return _passwordStrengthRegularExpression;
            }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get
            {
                return _requiresQuestionAndAnswer;
            }
        }

        public override bool RequiresUniqueEmail
        {
            get
            {
                return _requiresUniqueEmail;
            }
        }

        private string CurrentTenancy
        {
            get
            {
                return HttpContext.Current.Request.RequestContext.RouteData.GetRequiredString("tenant");
            }
        }

        #endregion

 
        #region Overriden Public Functions

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config", "There are no membership configuration settings.");
            }
            if (string.IsNullOrEmpty(name))
            {
                name = ProviderName;
            }
            if (string.IsNullOrEmpty(config["description"]))
            {
                config["description"] = "An Asp.Net membership provider for the RavenDB document database.";
            }

            base.Initialize(name, config);

            InitConfigSettings(config);
            InitPasswordEncryptionSettings(config);

            
           

        
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            var args = new ValidatePasswordEventArgs(username, newPassword, false);
            OnValidatingPassword(args);
            if (args.Cancel)
            {
                throw new MembershipPasswordException("The new password is not valid.");
            }
            //TODO: Implement the validation to set diferent password of previus password 
            //if(!ValidateUser(username,oldPassword))
            //{
            //    throw new MembershipPasswordException(
            //            "Invalid username or old password. "
            //            + "You must supply valid credentials to change your password.");                
            //}
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                ApplicationUser user;
                if (CurrentTenancy == Tenants.SuperAdmin)
                {
                    user = (from u in session.Query<ApplicationUser>()
                                where u.UserName == username select u).SingleOrDefault();
                }
                else
                {
                    user = (from u in session.Query<ApplicationUser>()
                                where u.UserName == username
                                    && u.Tenancy.Equals(CurrentTenancy, StringComparison.OrdinalIgnoreCase)
                                select u).SingleOrDefault();                
                }

                user.PasswordHash = EncodePassword(newPassword.Trim(), user.PasswordSalt);
                session.SaveChanges();
            }
            
            return true;
        }

        public override bool ChangePasswordQuestionAndAnswer(
            string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            //password attempt tracked in validateuser
            if (!ValidateUser(username, password))
            {
                throw new MembershipPasswordException(
                    "You must supply valid credentials to change " + "your question and answer.");
            }

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var user = (from u in session.Query<ApplicationUser>()
                            where
                                u.UserName == username
                                && u.Tenancy.Equals(CurrentTenancy, StringComparison.OrdinalIgnoreCase)
                            select u).SingleOrDefault();

                user.PasswordQuestion = newPasswordQuestion.Trim();
                user.PasswordAnswer = EncodePassword(newPasswordAnswer.Trim(), user.PasswordSalt);
                session.SaveChanges();
            }
            return true;
        }

        public override MembershipUser CreateUser(
            string username,
            string password,
            string email,
            string passwordQuestion,
            string passwordAnswer,
            bool isApproved,
            object providerUserKey,
            out MembershipCreateStatus status)
        {
            HttpContext.Current.Items["ApplicationUser"] = null;

            var args = new ValidatePasswordEventArgs(username, password, true);
            OnValidatingPassword(args);
            if (args.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            //If we require a question and answer for password reset/retrieval and they were not provided throw exception
            if (((_enablePasswordReset || _enablePasswordRetrieval) && _requiresQuestionAndAnswer)
                && string.IsNullOrEmpty(passwordAnswer))
            {
                throw new ArgumentException(
                    "Requires question and answer is set to true and a question and answer were not provided.");
            }

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                if (RequiresUniqueEmail)
                {
                    var existingUser = session.Query<ApplicationUser>().Any(x => x.ContactEmail == email);

                    if (existingUser)
                    {
                        status = MembershipCreateStatus.DuplicateEmail;
                        return null;
                    }
                }

                ApplicationUser applicationUser;
                var isNew = true;
                var invitation = providerUserKey as Invitation;
                if (invitation == null)
                {
                    ValidateUniqueUsername(username, CurrentTenancy, session);

                    applicationUser = new ApplicationUser();
                    this.SetUserData(username, password, email, passwordQuestion, passwordAnswer, isApproved, applicationUser);

                    //Set owner invitation info
                    applicationUser.Tenancy = CurrentTenancy;
                }
                else
                {
                    ValidateUniqueUsername(username, invitation.Tenancy, session);

                    if (string.IsNullOrEmpty(invitation.AcceptingAppUserId))
                    {
                        applicationUser = new ApplicationUser();
                    }
                    else
                    {
                        var uId = invitation.AcceptingAppUserId;
                        applicationUser = session.Load<ApplicationUser>(uId);
                        isNew = false;
                    }

                    if (applicationUser == null)
                    {
                        applicationUser = new ApplicationUser();
                    }

                    this.SetUserData(username, password, email, passwordQuestion, passwordAnswer, isApproved, applicationUser);
                    //Updated. Previous: invitation.Tenant; 
                    applicationUser.Tenancy = invitation.Tenancy;
                    invitation.AcceptingUser = applicationUser;

                    if (!Roles.RoleExists(invitation.Role))
                    {
                        Roles.CreateRole(invitation.Role);
                    }

                    Roles.AddUserToRole(applicationUser.UserName, invitation.Role);
                }

                if (isNew)
                {
                    session.Store(applicationUser);
                }

                HttpContext.Current.Items["ApplicationUser"] = applicationUser;
                session.SaveChanges();

                status = MembershipCreateStatus.Success;

                return new MembershipUser(
                    ProviderName,
                    username,
                    applicationUser.PrincipalId,
                    email,
                    passwordQuestion,
                    applicationUser.Comment,
                    isApproved,
                    false,
                    applicationUser.DateCreated,
                    new DateTime(1900, 1, 1),
                    new DateTime(1900, 1, 1),
                    DateTime.UtcNow,
                    new DateTime(1900, 1, 1));
            }
        }

        private void ValidateUniqueUsername(string username, string tenancy, IDocumentSession session)
        {
            var q = from au in session.Query<ApplicationUser>() where au.UserName==username && au.Tenancy==tenancy && au.Status != UserStatus.Deleted select au;
            if (q.Any())
            {
                throw new ApplicationException("The username is already used. Select another username.");
            }
        }

        private void SetUserData(
            string username,
            string password,
            string email,
            string passwordQuestion,
            string passwordAnswer,
            bool isApproved,
            ApplicationUser user)
        {
            user.UserName = username.Trim();
            user.PasswordSalt = PasswordUtility.RandomSalt();
            user.PasswordHash = this.EncodePassword(password.Trim(), user.PasswordSalt);
            user.ContactEmail = email.Trim();
            user.DateCreated = DateTime.UtcNow;
            user.PasswordQuestion = string.IsNullOrEmpty(passwordQuestion) ? passwordQuestion : passwordQuestion.Trim();
            user.PasswordAnswer = string.IsNullOrEmpty(passwordAnswer)
                                      ? passwordAnswer
                                      : this.EncodePassword(passwordAnswer.Trim(), user.PasswordSalt);
            user.IsApproved = isApproved;
            user.IsLockedOut = false;
            user.IsOnline = false;
            user.Enabled = true;
            if (string.IsNullOrEmpty(user.PrincipalId))
            {
                user.PrincipalId = string.Format("formsauthentication/{0}", Guid.NewGuid());
            }
        }

        public MembershipUser CreateUser(
            string username,
            string password,
            string email,
            string fullName,
            string passwordQuestion,
            string passwordAnswer,
            bool isApproved,
            object providerUserKey,
            out MembershipCreateStatus status)
        {
            var user = CreateUser(
                username, password, email, passwordQuestion, passwordAnswer, isApproved, providerUserKey, out status);

            return user;
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                try
                {
                    var q = from u in session.Query<ApplicationUser>()
                            where
                                u.UserName == username
                                && u.Tenancy.Equals(CurrentTenancy, StringComparison.OrdinalIgnoreCase)
                            select u;
                    var user = q.SingleOrDefault();
                    if (user == null)
                    {
                        throw new NullReferenceException("User does not exist.");
                    }
                    session.Delete(user);
                    session.SaveChanges();
                    return true;
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry(ApplicationName, ex.ToString());
                    return false;
                }
            }
        }

        public override MembershipUserCollection FindUsersByEmail(
            string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            return FindUsers(u => u.ContactEmail.Contains(emailToMatch), pageIndex, pageSize, out totalRecords);
        }

        public override MembershipUserCollection FindUsersByName(
            string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            return FindUsers(u => u.UserName.Contains(usernameToMatch), pageIndex, pageSize, out totalRecords);
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            return FindUsers(null, pageIndex, pageSize, out totalRecords);
        }

        public override int GetNumberOfUsersOnline()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                return
                    (from u in session.Query<ApplicationUser>() where u.IsOnline == true select u).Count
                        <ApplicationUser>();
            }
        }

        public override string GetPassword(string username, string answer)
        {
            if (!EnablePasswordRetrieval)
            {
                throw new NotSupportedException("Password retrieval feature is not supported.");
            }

            if (PasswordFormat == MembershipPasswordFormat.Hashed)
            {
                throw new NotSupportedException("Password retrieval is not supported with hashed passwords.");
            }

            ApplicationUser user;

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q = from u in session.Query<ApplicationUser>()
                        where
                            u.UserName == username
                            && u.Tenancy.Equals(CurrentTenancy, StringComparison.OrdinalIgnoreCase)
                        select u;
                user = q.SingleOrDefault();

                if (user == null)
                {
                    throw new NullReferenceException("The specified user does not exist.");
                }

                var encodedAnswer = EncodePassword(answer, user.PasswordSalt);
                if (RequiresQuestionAndAnswer && user.PasswordAnswer != encodedAnswer)
                {
                    user.FailedPasswordAnswerAttempts++;
                    session.SaveChanges();

                    throw new MembershipPasswordException("The password question's answer is incorrect.");
                }
            }
            if (PasswordFormat == MembershipPasswordFormat.Clear)
            {
                return user.PasswordHash;
            }

            return UnEncodePassword(user.PasswordHash, user.PasswordSalt);
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            var user = GetRavenDbUser(username, userIsOnline);
            if (user != null)
            {
                return UserToMembershipUser(user);
            }
            return null;
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var user = session.Load<ApplicationUser>(providerUserKey.ToString());
                if (user != null)
                {
                    return UserToMembershipUser(user);
                }
                return null;
            }
        }

        public override string GetUserNameByEmail(string email)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q = from u in session.Query<ApplicationUser>() where u.ContactEmail == email select u.UserName;
                return q.SingleOrDefault();
            }
        }

        public override string ResetPassword(string username, string answer)
        {
            if (!EnablePasswordReset)
            {
                throw new ProviderException("Password reset is not enabled.");
            }

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                try
                {
                    var q = from u in session.Query<ApplicationUser>()
                            where
                                u.UserName == username
                                && u.Tenancy.Equals(CurrentTenancy, StringComparison.OrdinalIgnoreCase)
                            select u;
                    var user = q.SingleOrDefault();
                    if (user == null)
                    {
                        throw new HttpException("The user to reset the password for could not be found.");
                    }
                    if (user.PasswordAnswer != EncodePassword(answer, user.PasswordSalt))
                    {
                        user.FailedPasswordAttempts++;
                        session.SaveChanges();
                        throw new MembershipPasswordException("The password question's answer is incorrect.");
                    }
                    var newPassword = Membership.GeneratePassword(8, 2);
                    user.PasswordHash = EncodePassword(newPassword, user.PasswordSalt);
                    session.SaveChanges();
                    return newPassword;
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry(ApplicationName, ex.ToString());
                    throw;
                }
            }
        }

        public override bool UnlockUser(string userName)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var user =
                    session.Query<ApplicationUser>().SingleOrDefault(
                        x => x.UserName == userName && x.Tenancy == CurrentTenancy);

                if (user == null)
                {
                    return false;
                }

                user.IsLockedOut = false;
                session.SaveChanges();
                return true;
            }
        }

        public override void UpdateUser(MembershipUser user)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q = from u in session.Query<ApplicationUser>()
                        where u.UserName == user.UserName && u.Tenancy == GetDbUserTenancy(user)
                        select u;
                var dbUser = q.SingleOrDefault();

                if (dbUser == null)
                {
                    throw new HttpException("The user to update could not be found.");
                }

                dbUser.UserName = user.UserName.Trim();
                dbUser.ContactEmail = user.Email.Trim();
                dbUser.DateCreated = user.CreationDate;
                dbUser.DateLastLogin = user.LastLoginDate;
                dbUser.IsOnline = user.IsOnline;
                dbUser.IsApproved = user.IsApproved;
                dbUser.IsLockedOut = user.IsLockedOut;

                session.SaveChanges();
            }
        }

        private string GetDbUserTenancy(MembershipUser membershipUseruser)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                //var user =
                //    (from u in session.Query<ApplicationUser>()
                //     where u.PrincipalId == (string)membershipUseruser.ProviderUserKey
                //     select u).SingleOrDefault();
                var user = session.Load<ApplicationUser>((string)membershipUseruser.ProviderUserKey);
                return user != null ? user.Tenancy : null;
            }
        }

       

        public override bool ValidateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
               
                return false;
            }

            var retValue = false;
            HttpContext.Current.Items["ApplicationUser"] = null;
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var user = (from u in session.Query<ApplicationUser>()
                            where
                                u.UserName == username
                                && u.Tenancy.Equals(CurrentTenancy, StringComparison.OrdinalIgnoreCase)
                            select u).SingleOrDefault();

                if (user == null)
                {
                  
                    return false;
                }

                if ((user.Status == UserStatus.Disabled || user.Status == UserStatus.Deleted || user.Status == UserStatus.RejectInvitation))
                {
                    HttpContext.Current.Session["ErrorMessage"] = "Your user account is disabled. Please contact your system administrator.";
                   
                    return false;
                }

                if (user.PasswordHash == EncodePassword(password, user.PasswordSalt))
                {
                    user.DateLastLogin = DateTime.UtcNow;
                    user.IsOnline = true;
                    user.FailedPasswordAttempts = 0;
                    user.FailedPasswordAnswerAttempts = 0;

                    retValue = true;
                }
                else
                {
                    user.LastFailedPasswordAttempt = DateTime.UtcNow;
                    user.FailedPasswordAttempts++;
                    user.IsLockedOut = IsLockedOutValidationHelper(user);
                   
                }

                HttpContext.Current.Items["ApplicationUser"] = user;
                session.SaveChanges();
            }

            return retValue;
        }

        #endregion

        #region Private Helper Functions

        private bool IsLockedOutValidationHelper(ApplicationUser user)
        {
            long minutesSinceLastAttempt = DateTime.UtcNow.Ticks - user.LastFailedPasswordAttempt.Ticks;
            if (user.FailedPasswordAttempts >= MaxInvalidPasswordAttempts
                && minutesSinceLastAttempt < this.PasswordAttemptWindow)
            {
                return true;
            }
            return false;
        }

        private ApplicationUser UpdatePasswordAttempts(
            ApplicationUser u, PasswordAttemptKinds attemptKind, bool signedInOk)
        {
            var minutesSinceLastAttempt = DateTime.UtcNow.Ticks - u.LastFailedPasswordAttempt.Ticks;
            if (signedInOk || minutesSinceLastAttempt > this.PasswordAttemptWindow)
            {
                u.LastFailedPasswordAttempt = new DateTime(1900, 1, 1);
                u.FailedPasswordAttempts = 0;
                u.FailedPasswordAnswerAttempts = 0;

                SaveRavenUser(u);
                return u;
            }
            else
            {
                u.LastFailedPasswordAttempt = DateTime.UtcNow;
                if (attemptKind == PasswordAttemptKinds.PasswordAttempt)
                {
                    u.FailedPasswordAttempts++;
                }
                else
                {
                    u.FailedPasswordAnswerAttempts++;
                }
                if (u.FailedPasswordAttempts > MaxInvalidPasswordAttempts
                    || u.FailedPasswordAnswerAttempts > MaxInvalidPasswordAttempts)
                {
                    u.IsLockedOut = true;
                }
            }

            SaveRavenUser(u);
            return u;
        }

        private MembershipUserCollection FindUsers(
            Func<ApplicationUser, bool> predicate, int pageIndex, int pageSize, out int totalRecords)
        {
            var membershipUsers = new MembershipUserCollection();
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var q = from u in session.Query<ApplicationUser>() select u;
                IEnumerable<ApplicationUser> results = predicate != null ? q.Where(predicate) : q;

                totalRecords = results.Count();
                var pagedUsers = results.Skip(pageIndex * pageSize).Take(pageSize);
                foreach (var user in pagedUsers)
                {
                    membershipUsers.Add(UserToMembershipUser(user));
                }
            }
            return membershipUsers;
        }

        private MembershipUser UserToMembershipUser(ApplicationUser user)
        {
            return new MembershipUser(
                ProviderName,
                user.UserName,
                user.PrincipalId,
                user.ContactEmail,
                user.PasswordQuestion,
                user.Comment,
                user.IsApproved,
                user.IsLockedOut,
                user.DateCreated,
                user.DateLastLogin.HasValue ? user.DateLastLogin.Value : new DateTime(1900, 1, 1),
                new DateTime(1900, 1, 1),
                new DateTime(1900, 1, 1),
                new DateTime(1900, 1, 1));
        }

        private ApplicationUser GetRavenDbUser(string username, bool userIsOnline)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                ApplicationUser user;
                if (Tenants.SuperAdmin == CurrentTenancy)
                {
                    var q = from u in session.Query<ApplicationUser>()
                            where u.UserName == username select u;
                    user = q.SingleOrDefault();
                }
                else
                {
                    var q = from u in session.Query<ApplicationUser>()
                            where
                                u.UserName == username
                                && u.Tenancy.Equals(CurrentTenancy, StringComparison.OrdinalIgnoreCase)
                            select u;
                    user = q.SingleOrDefault();                
                }
                
                user.IsOnline = userIsOnline;                                
                session.SaveChanges();
                return user;
            }
        }

        private void SaveRavenUser(ApplicationUser user)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                session.Store(user);
                session.SaveChanges();
            }
        }

        private void InitConfigSettings(NameValueCollection config)
        {
            ApplicationName = GetConfigValue(
                config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            _maxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
            _passwordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
            _minRequiredNonAlphanumericCharacters =
                Convert.ToInt32(GetConfigValue(config["minRequiredAlphaNumericCharacters"], "1"));
            _minRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
            _passwordStrengthRegularExpression =
                Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], String.Empty));
            _enablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
            _enablePasswordRetrieval = Convert.ToBoolean(GetConfigValue(config["enablePasswordRetrieval"], "true"));
            _requiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
            _requiresUniqueEmail = Convert.ToBoolean(GetConfigValue(config["requiresUniqueEmail"], "true"));
        }

        private void InitPasswordEncryptionSettings(NameValueCollection config)
        {
            var cfg =
                WebConfigurationManager.OpenWebConfiguration(
                    System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            var machineKey = cfg.GetSection("system.web/machineKey") as MachineKeySection;
            _hashAlgorithm = machineKey.ValidationAlgorithm;
            _validationKey = machineKey.ValidationKey;

            if (machineKey.ValidationKey.Contains("AutoGenerate"))
            {
                if (PasswordFormat != MembershipPasswordFormat.Clear)
                {
                    throw new ProviderException(
                        "Hashed or Encrypted passwords are not supported with auto-generated keys.");
                }
            }

            var passFormat = config["passwordFormat"] ?? "Hashed";

            switch (passFormat)
            {
                case "Hashed":
                    _passwordFormat = MembershipPasswordFormat.Hashed;
                    break;
                case "Encrypted":
                    _passwordFormat = MembershipPasswordFormat.Encrypted;
                    break;
                case "Clear":
                    _passwordFormat = MembershipPasswordFormat.Clear;
                    break;
                default:
                    throw new ProviderException("The password format from the custom provider is not supported.");
            }
        }

        private string EncodePassword(string password, string salt)
        {
            var encodedPassword = password;

            switch (_passwordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    encodedPassword = Convert.ToBase64String(EncryptPassword(Encoding.Unicode.GetBytes(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    if (string.IsNullOrEmpty(salt))
                    {
                        throw new ProviderException("A random salt is required with hashed passwords.");
                    }
                    encodedPassword = PasswordUtility.HashPassword(password, salt, _hashAlgorithm, _validationKey);
                    break;
                default:
                    throw new ProviderException("Unsupported password format.");
            }
            return encodedPassword;
        }

        private string UnEncodePassword(string encodedPassword, string salt)
        {
            var password = encodedPassword;

            switch (_passwordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    password = Encoding.Unicode.GetString(DecryptPassword(Convert.FromBase64String(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    throw new ProviderException("Hashed passwords do not require decoding, just compare hashes.");
                default:
                    throw new ProviderException("Unsupported password format.");
            }
            return password;
        }

        private string GetConfigValue(string value, string defaultValue)
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        #endregion
    }
}