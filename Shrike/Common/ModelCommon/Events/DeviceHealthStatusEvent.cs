using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using Lok.Unik.ModelCommon.Client;

namespace Lok.Unik.ModelCommon.Events
{
    public enum HealthStatus
    {
        Unknown = 1000,

        Green = 1001,

        Yellow = 1002,

        Red = 1003
        
        
    }


    public class DeviceHealthStatusEvent
    {
        [DocumentIdentifier]
        public string EventId { get; set; }

        public Guid DeviceId { get; set; }
        public DateTime TimeChanged { get; set; }
        public HealthStatus From { get; set; }
        public HealthStatus To { get; set; }

        public IList<Tag> DeviceTags { get; set; }
        public IList<int> DeviceTagHashes { get; set; } 
        public static IList<int> ConvertTagsToHashs(IList<Tag> tags)
        {
            
            return tags.Select(it => it.GetHashCode()).ToList();
        }

        public string DeviceName { get; set; }
    }
}
