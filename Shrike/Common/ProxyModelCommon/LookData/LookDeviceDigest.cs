using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using Lok.Control.Common.ProxyCommon.LookData;


namespace Lok.Control.Common.ProxyCommon
{
    /// <summary>
    /// Snapshot of data from a look sensor
    /// </summary>
    public class LookDeviceDigest
    {
        public LookDeviceDigest()
        {
            DigestId = Guid.NewGuid().ToString();
        }

        [DocumentIdentifier]
        public string DigestId { get; set; }

        /// <summary>
        /// Id of the device, by convention, a guid
        /// </summary>
        
        public string DeviceId { get; set; }

        /// <summary>
        /// Human readable name of the device.
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Operational health of the device.
        /// </summary>
        public DeviceHealth DeviceHealth { get; set; }

        /// <summary>
        /// physical time zone of the device
        /// </summary>
        public TimeZoneInfo DeviceTimeZone { get; set; }

        /// <summary>
        /// Description of each person detected by the sensor.
        /// </summary>
        public List<LookPerson> Persons { get; set; }

        /// <summary>
        /// when the data is collected into the data warehouse
        /// </summary>
        public DateTime CollectionTime { get; set; }

        /// <summary>
        /// Minimum time (excluding DateTime.MinValue) 
        /// seen in the look person events exit time.
        /// Calculated during collection.
        /// </summary>
        public DateTime TimeMin { get; set; }

        /// <summary>
        /// Maximum time seen in the look person events
        /// exit time.
        /// Calculated during collection.
        /// </summary>
        public DateTime TimeMax { get; set; }
      


        public override string ToString()
        {
            return string.Format("{0}: {1}| {2}. Seeing {3} people.",
                                 DeviceId, DeviceName ?? "No Name", 
                                 DeviceHealth, 
                                 (null==Persons) ? 0: Persons.Count);
        }
    }
}
