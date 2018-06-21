using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Control.Common.ProxyCommon;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// Move Device info
    /// </summary>
    public class MoveDeviceInfo
    {
        /// <summary>
        /// Human readable name of the device
        /// </summary>
        public string DeviceName { get; set; }
        
        /// <summary>
        /// IP Address of the device
        /// </summary>
        public string DeviceIP { get; set; }
        
        /// <summary>
        /// Location of the device
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// All border triggers defined for the device
        /// </summary>
        public List<MoveBorderInfo> MoveBorders { get; set; }
        
        
        /// <summary>
        /// All hotspot triggers defined for the device
        /// </summary>
        public List<MoveHotspotInfo> MoveHotspots { get; set; }


        public override string ToString()
        {
            return string.Format("Device {0} {1}", DeviceName, DeviceIP);
        }
    }
}
