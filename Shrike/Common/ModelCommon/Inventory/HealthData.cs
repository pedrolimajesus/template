using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Inventory
{
    public class EventData
    {
        public string DeviceId { get; set; }
        public Guid EventId { get; set; }
        public DateTime EventTime { get; set; }
        public int MonitorId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class CounterData
    {
        public string DeviceId { get; set; }
        public Guid EventId { get; set; }
        public DateTime SampleTime { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
    }

    public class HealthData
    {
        public List<EventData> Events { get; set; }
        public List<CounterData> CounterSamples { get; set; }
    }
}
