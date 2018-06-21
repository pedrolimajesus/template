using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppComponents;

namespace Lok.Control.Common.ProxyCommon.Interfaces
{
    /// <summary>
    /// Describes a sensor device
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// Identifier for the device
        /// </summary>
        [DocumentIdentifier]
        public string Id { get; set; }


        public string IpAddress { get; set; }

        /// <summary>
        /// Human readable name of the device
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Type of the device
        /// </summary>
        public DeviceType DeviceType { get; set; }

        /// <summary>
        /// Description of the device
        /// </summary>
        public string DeviceDescription { get; set; }

        /// <summary>
        /// Operational health of the device.
        /// </summary>
        public string DeviceHealth { get; set; }

      


        public override string ToString()
        {
            return string.Format("{0} {1} {2}: {3} | {4}",
                                 IpAddress,
                                 DeviceType,
                                 DeviceName ?? "No Name",
                                 DeviceHealth,
                                 DeviceDescription ?? "No description"
                );
        }
    }

    /// <summary>
    /// Type of sensor device
    /// </summary>
    public enum DeviceType
    {
        Unknown_Device = 0,

        /// <summary>
        /// Move sensor
        /// </summary>
        Move_Device = 1, 

        /// <summary>
        /// Look sensor
        /// </summary>
        Look_Device = 2, 

        /// <summary>
        /// Sensor data collection proxy
        /// </summary>
        Proxy_Device = 3, 


        Falo_ID_Srv = 4, 
        Falo_Profile_Srv = 5
    }
}
