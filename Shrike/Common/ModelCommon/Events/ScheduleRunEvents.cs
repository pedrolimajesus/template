using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using Lok.Unik.ModelCommon.Client;

namespace Lok.Unik.ModelCommon.Events
{
    public class ScheduleRunMessage
    {
        public string Application { get; set; }
        public string ApplicationId { get; set; }
        public string ContentPackage { get; set; }
        public string ContentPackageId { get; set; }
        public string PlacementId { get; set; }
        public string PlacementSummary { get; set; }
        public DateTime LaunchTime { get; set; }
        public TimeSpan RunDuration { get; set; }
        
    }

    public class ScheduleRunEvent : ITaggeableEvent
    {
        public ScheduleRunEvent()
        {
            Info = new ScheduleRunMessage();
            Identifier = Guid.NewGuid();
            DeviceTags = new List<Tag>();
        }



        [DocumentIdentifier]
        public Guid Identifier { get; set; }

        public string DeviceId { get; set; }

        public string DeviceName { get; set; }
        public IList<Tag> DeviceTags { get; set; }

        public ScheduleRunMessage Info { get; set; }

        public string ReportTagValue { get; set; }
    }
}
