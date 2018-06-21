using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using Lok.Unik.ModelCommon.Client;

namespace Lok.Unik.ModelCommon.Events
{
    public class UserAuthorizationEvent
    {


        [DocumentIdentifier]
        public string EventId { get; set; }

        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public DateTime TimeRegistered { get; set; }
        public string PasscodeUsed { get; set; }
        public IList<Tag> InitialTags { get; set; }
        public string IpAddress { get; set; }

    }
}
