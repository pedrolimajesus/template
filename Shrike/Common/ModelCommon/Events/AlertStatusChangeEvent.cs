using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using Lok.Unik.ModelCommon.Client;

namespace Lok.Unik.ModelCommon.Events
{
    public class AlertStatusChangeEvent : ITaggeableEvent
    {
        [DocumentIdentifier]
        public Guid Identifier { get; set; }

        public DateTime TimeGenerated { get; set; }
        public DateTime StaleAlertTime { get; set; }
        public HealthStatus AlertHealthLevel { get; set; }

        public string AlertTitle { get; set; }
        public string Message { get; set; }

        public Guid LongestAssignedUserId { get; set; }
        public string LongestAssignedUserName { get; set; }
        public TimeSpan LongestAssignmentTime { get; set; }

        public bool Resolved { get; set; }

        public Guid RelatedDevice { get; set; }
        public string RelatedDeviceName { get; set; }

        public IList<Tag> DeviceTags { get; set; }

        public string KindDesc { get; set; }
        public AlertKinds Kind { get; set; }

        public string ReportTagValue { get; set; }
    }
}
