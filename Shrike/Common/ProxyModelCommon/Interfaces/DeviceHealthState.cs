using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon.Interfaces
{
    /// <summary>
    /// Logs a snapshot of a sensor device's operational status
    /// </summary>
    public class DeviceHealthState
    {
        /// <summary>
        /// Status at the snapshot time
        /// </summary>
        public DeviceStatus Status { get; set; }

        /// <summary>
        /// Time of the snapshot
        /// </summary>
        public DateTime Time { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Time, Status);
        }
    }
}
