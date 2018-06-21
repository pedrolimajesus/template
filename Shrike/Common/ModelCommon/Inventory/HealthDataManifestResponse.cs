using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Events;

namespace Lok.Unik.ModelCommon.Inventory
{
    public class EventData
    {
        public string DeviceId { get; set; }
        public Guid EventId { get; set; }
        public DateTime EventTime { get; set; }
        public int SourceId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }

        public int Stamp { get; set; }
        public HealthStatus StatusLevel { get; set; }
    }

    public class CounterData
    {
        public string DeviceId { get; set; }
        public Guid EventId { get; set; }
        public DateTime SampleTime { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
    }

    public class HealthDataManifestResponse: ManifestItem
    {
        public string DeviceId { get; set; }
        public List<EventData> Events { get; set; }
        public List<CounterData> CounterSamples { get; set; }
    }
}
