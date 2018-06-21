using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;

namespace Lok.Control.Common.ProxyCommon.Interfaces
{


    /// <summary>
    /// A batch of data that provides snapshots for look and move sensors
    /// reporting to this proxy agent.
    /// </summary>
    public class ProxyDevicesDigest
    {
        /// <summary>
        /// Id of the proxy agent sensor data collector.
        /// </summary>
        [DocumentIdentifier]
        public int ProxyId { get; set; }

        /// <summary>
        /// Name of the proxy agent sensor data collector.
        /// </summary>
        public string ProxyName { get; set; }

        /// <summary>
        /// Human readable description of the proxy agent 
        /// </summary>
        public string ProxyDescription { get; set; }

        /// <summary>
        /// Operational health
        /// </summary>
        public DeviceHealth ProxyHealth { get; set; }


        /// <summary>
        /// Beginning of the snapshot time
        /// </summary>
        public DateTime DigestStart { get; set; }

        /// <summary>
        /// End of the snapshot time
        /// </summary>
        public DateTime DigestEnd { get; set; }

        /// <summary>
        /// Snapshot report from each look device.
        /// </summary>
        public List<LookDeviceDigest> LookDevices { get; set; }

        /// <summary>
        /// Snapshot report from each move device.
        /// </summary>
        public List<MoveDeviceDigest> MoveDevices { get; set; }

        public ProxyDevicesDigest()
        {
            LookDevices= new List<LookDeviceDigest>();
            MoveDevices = new List<MoveDeviceDigest>();
        }


        public override string ToString()
        {
            return string.Format("{0} {1}: {3}. Digest from {4} to {5} of {6} look sensors and {7} move sensors",
                                 ProxyId, ProxyName ?? "No Name", ProxyHealth, DigestStart, DigestEnd,
                                 (null == LookDevices) ? 0 : LookDevices.Count,
                                 (null == MoveDevices) ? 0 : MoveDevices.Count);
        }
    }
}
