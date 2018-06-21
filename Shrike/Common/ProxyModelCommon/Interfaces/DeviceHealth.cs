using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Control.Common.ProxyCommon.Interfaces;

namespace Lok.Control.Common.ProxyCommon
{
    /// <summary>
    /// Operational status of a sensor device
    /// </summary>
    public class DeviceHealth
    {
        /// <summary>
        /// Current status
        /// </summary>
        public DeviceStatus CurrentStatus { get; set; }

        /// <summary>
        /// History of device health status
        /// </summary>
        public List<DeviceHealthState> HealthLog { get; set; }


        public override string ToString()
        {
            return string.Format("Status: {0} ", CurrentStatus);
        }

    }

    /// <summary>
    /// Status of sensor devices
    /// </summary>
    public enum DeviceStatus
    {
        /// <summary>
        /// Normal operation
        /// </summary>
        Running, 
        
        /// <summary>
        /// Standby mode
        /// </summary>
        Standby, 
        
        /// <summary>
        /// Device is shutdown
        /// </summary>
        Shutdown, 
        
        /// <summary>
        /// No communication with device,
        /// status unknown
        /// </summary>
        Unreachable
    }
}


