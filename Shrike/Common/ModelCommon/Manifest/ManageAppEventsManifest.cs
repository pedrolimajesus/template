using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Events;

namespace Lok.Unik.ModelCommon.Manifest
{
    public class ManagedAppEventManifestItem : ManifestItem
    {

    }

    public class ManagedAppEventManifestResponse : ManifestItem
    {
        public ManagedAppEventManifestResponse()
        {
            ProblemEvents = new List<ManagedAppProblemEvent>();
            UserInteractionEvents = new List<ManagedAppInteractionEvent>();
            ScheduleRunEvents = new List<ScheduleRunEvent>();
        }

        public string DeviceId { get; set; }

        public IList<ManagedAppProblemEvent> ProblemEvents { get; set; }
        public IList<ManagedAppInteractionEvent> UserInteractionEvents { get; set; }
        public IList<ScheduleRunEvent> ScheduleRunEvents { get; set; } 
    }
}
