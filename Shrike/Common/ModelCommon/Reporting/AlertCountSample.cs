using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Client;
using Lok.Unik.ModelCommon.Events;

namespace Lok.Unik.ModelCommon.Reporting
{
    public class AlertCountSample : ITaggeableEvent
    {
        public Guid RelatedDeviceIdentifier { get; set; }
        public string RelatedDeviceName { get; set; }
        public int RedCount { get; set; }
        public int YellowCount { get; set; }
        public int Total { get; set; }
        public AlertKinds MostCommonKind { get; set; }
        public IList<Tag> DeviceTags { get; set; }

        public string ReportTagValue { get; set; }
    }
}
