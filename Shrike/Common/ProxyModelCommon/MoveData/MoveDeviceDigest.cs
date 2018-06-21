using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;

namespace Lok.Control.Common.ProxyCommon
{
    /// <summary>
    /// Snapshot of a Move sensor
    /// </summary>
    public class MoveDeviceDigest
    {
        

        [DocumentIdentifier]
        public string DigestId { get; set; }

        /// <summary>
        /// Device identifier.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Name of the device.
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Operational health of the device
        /// </summary>
        public DeviceHealth DeviceHealth { get; set; }

        /// <summary>
        /// List of "in-camera" events
        /// </summary>
        public List<DwellEvent> DwellEvents { get; set; }        

        /// <summary>
        /// List of when people cross a border
        /// </summary>
        public List<BorderEvent> BorderEvents { get; set; }      

        /// <summary>
        /// List of when people enter and exit hotspots
        /// </summary>
        public List<HotspotEvent> HotspotEvents { get; set; }


        public DateTime CollectionTime { get; set; }
        public DateTime TimeMin { get; set; }
        public DateTime TimeMax { get; set; }

        public TimeZoneInfo DeviceTimeZone { get; set; }

        /// <summary>
        /// List of peoples tracks
        /// </summary>
        public List<PersonTrack> Tracks { get; set; }

        public MoveDeviceDigest()
        {
            DigestId = Guid.NewGuid().ToString();
            Tracks = new List<PersonTrack>();
            HotspotEvents = new List<HotspotEvent>();
            BorderEvents = new List<BorderEvent>();
            DwellEvents = new List<DwellEvent>();
        }

        public override string ToString()
        {
            return string.Format(
                "{0} {1}: {2} with {3} Dwell events, {4} Border events, {5} Hotspot events, {6} tracks", 
                DeviceId,DeviceName ?? "No Name",DeviceHealth, DwellEvents.Count,BorderEvents.Count,
                HotspotEvents.Count, Tracks.Count);
        }
 
    }
}
