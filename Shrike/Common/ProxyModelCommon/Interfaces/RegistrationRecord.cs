using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon.Interfaces
{
    /// <summary>
    /// Description of registered device
    /// </summary>
    public class RegistrationRecord
    {
        /// <summary>
        /// Human readable name of the device
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Description of device function, placement, etc
        /// </summary>
        public string DeviceDescription { get; set; }

        /// <summary>
        /// Identifer for the device; 
        /// by convention for aware, this will be the IP Address
        /// </summary>
        public string DeviceId { get; set; }

        public override string ToString()
        {
            return string.Format("{0}| {1} ({2})", DeviceId, DeviceName ?? "No Name", DeviceDescription ?? "");
        }
    }
}
