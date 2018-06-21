using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// Basic info about a look device
    /// </summary>
    public class LookDeviceInfo
    {
        /// <summary>
        /// Human readable name
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// IP Address
        /// </summary>
        public string DeviceIP { get; set; }

        /// <summary>
        /// Where located
        /// </summary>
        public string Location { get; set; }
    }
}
