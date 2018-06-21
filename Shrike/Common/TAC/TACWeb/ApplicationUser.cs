// // 
// //  Copyright 2012 David Gressett
// // 
// //    Licensed under the Apache License, Version 2.0 (the "License");
// //    you may not use this file except in compliance with the License.
// //    You may obtain a copy of the License at
// // 
// //        http://www.apache.org/licenses/LICENSE-2.0
// // 
// //    Unless required by applicable law or agreed to in writing, software
// //    distributed under the License is distributed on an "AS IS" BASIS,
// //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //    See the License for the specific language governing permissions and
// //    limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;

using Raven.Client.Indexes;

namespace AppComponents.Web
{
    public enum UserStatus
    {
        New = 0, //a new user in this case just the system owner will have this state by default or any new user
        Invited = 1, //new user that has been invited
        RejectInvitation = 2, //a user that has rejected his invitation
        Enabled = 3, //or active state, default state for a user that is using the system
        Disabled = 4, //or blocked state, when a user is Disabled for any reason, it can change again to Enabled
        Deleted = 5 // or inactive state, when the user is deleted from the system and cannot be enabled again.
    }

    public class PasswordReset
    {
        [DocumentIdentifier]
        public string Code { get; set; }

        public AuthorizationCode AuthCode { get; set; }

        public DateTime ResetDateTime { get; set; }

        public string PrincipalId { get; set; }

        public string ContactEmail { get; set; }

        public string Tenancy { get; set; }
    }

    public class ApplicationUser : ApplicationPrincipal 
    {
        public ApplicationUser()
        {
            Extensions = new Dictionary<string, object>();
        }

        public string UserName { get; set; }

        public string PasswordHash { get; set; }

        public string PasswordSalt { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime? DateLastLogin { get; set; }

        public string ContactFirstName { get; set; }

        public string ContactLastName { get; set; }

        public string ContactEmail { get; set; }

        public string Handle { get; set; }

        public string PasswordQuestion { get; set; }

        public string PasswordAnswer { get; set; }

        public bool IsLockedOut { get; set; }

        public bool IsOnline { get; set; }

        public int FailedPasswordAttempts { get; set; }

        public int FailedPasswordAnswerAttempts { get; set; }

        public DateTime LastFailedPasswordAttempt { get; set; }

        public string Comment { get; set; }

        public bool IsApproved { get; set; }

        public Dictionary<string, object> Extensions { get; set; }

        public UserStatus Status { get; set; }

        public string ContainerId { get; set; }
    }

    public class ApplicationUser_ByUserName : AbstractIndexCreationTask<ApplicationUser>
    {
        public ApplicationUser_ByUserName()
        {
            Map = users => from u in users select new { u.UserName };
        }
    }

    public class ApplicationUser_ByContactEmail : AbstractIndexCreationTask<ApplicationUser>
    {
        public ApplicationUser_ByContactEmail()
        {
            Map = users => from u in users select new { u.ContactEmail };
        }
    }

    public class ApplicationUser_ByHandle : AbstractIndexCreationTask<ApplicationUser>
    {
        public ApplicationUser_ByHandle()
        {
            Map = users => from u in users select new { u.Handle };
        }
    }

    public class ApplicationUser_ByName : AbstractIndexCreationTask<ApplicationUser>
    {
        public ApplicationUser_ByName()
        {
            Map = users => from u in users select new { u.ContactFirstName, u.ContactLastName };
        }
    }

    public class ApplicationUser_ByTenancy : AbstractIndexCreationTask<ApplicationUser>
    {
        public ApplicationUser_ByTenancy()
        {
            Map = users => from u in users select new { u.Tenancy };
        }
    }
}