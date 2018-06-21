using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppComponents.Web
{
    using System.Xml.Serialization;

    using global::Raven.Imports.Newtonsoft.Json;

    public enum InvitationStatus
    {
        Deleted,

        Accepted,

        Sent,

        ReSent,

        Rejected,

        New,

        Active
    }

    public class Invitation
    {
        public Guid Id { get; set; }

        public string Tenancy { get; set; }

        public AuthorizationCode AuthorizationCode { get; set; }

        [DocumentIdentifier]
        public string SentTo { get; set; }

        public DateTime DateSent { get; set; }

        private ApplicationUser acceptingUser;

        [JsonIgnore]
        [XmlIgnore]
        public ApplicationUser AcceptingUser
        {
            get { return this.acceptingUser; }
            set
            {
                this.acceptingUser = value;
                if (this.acceptingUser == null)
                {
                    return;
                }
                this.AcceptingAppUserId = acceptingUser.PrincipalId;
                this.AcceptingUserId = this.acceptingUser.ContainerId;
            }
        }

        public string AcceptingAppUserId { get; private set; }
        public string AcceptingUserId { get; private set; }
        //in days
        public int ExpirationTime { get; set; }

        //used or not
        public InvitationStatus Status { get; set; }

        public int ResentTimes { get; set; }

        public DateTime LastAcceptedDate { get; set; }

        public string Role { get; set; }

        public string InvitingTenant { get; set; }

        public string InvitingUserId { get; set; }
    }
}