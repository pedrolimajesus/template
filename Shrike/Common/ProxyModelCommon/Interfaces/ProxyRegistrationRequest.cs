using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon.Interfaces
{
    /// <summary>
    /// Info used to register a proxy agent with Aware
    /// </summary>
    public class ProxyRegistrationRequest
    {
        /// <summary>
        /// Passphrase created by the admin for device registration
        /// </summary>
        public string Passphrase { get; set; }

        /// <summary>
        /// Human readable name of the device
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Description of device
        /// </summary>
        public string DeviceDescription { get; set; }
    }
}
