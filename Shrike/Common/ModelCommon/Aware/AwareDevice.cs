using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Control.Common.ProxyCommon;
using Lok.Unik.ModelCommon.Client;
using Lok.Unik.ModelCommon.Interfaces;

namespace Lok.Unik.ModelCommon.Aware
{

    public enum SensorCommands
    {
        Start,
        Stop,
        Restart,
        Reboot,
        Shutdown,
        Update
    }

    public enum SensorCommandStatuses
    {
        Pending,
        Sent,
        OK,
        Unreachable,
        Error
    }

    public class SensorCommand
    {
        public Guid CommandId { get; set; }
        public string DeviceIPAddress { get; set; }
        public Guid DeviceId { get; set; }
        public SensorCommands Command { get; set; }
        public DateTime Issued { get; set; }
        public SensorCommandStatuses Status { get; set; }
    }


    /// <summary>
    /// Aware device data
    /// </summary>
    public class AwareDevice : Device, ITimeFilter
    {
        public AwareDevice(): base()
        {
            Id = Guid.NewGuid();
            FacilityId = Guid.Empty;
            FacilityName = string.Empty;
            DeviceTriggers = new List<DeviceTrigger>();
            SensorCommand = null;
            this.TimeRegistered = DateTime.UtcNow;
        }

        public SensorCommand SensorCommand { get; set; }

        /// <summary>
        /// Type of device
        /// </summary>
        public Lok.Control.Common.ProxyCommon.Interfaces.DeviceType SensorType { get; set; }
        
        /// <summary>
        /// Health status of the device
        /// </summary>
        public DeviceHealth Health { get; set; }

        /// <summary>
        /// Count of alerts from the device
        /// </summary>
        public int AlertsCount { get; set; }

        /// <summary>
        /// Triggers defined for the device
        /// </summary>
        public IList<DeviceTrigger> DeviceTriggers { get; set; }

        /// <summary>
        /// Url of image
        /// </summary>
        public string ImageUrl { get; set; }
        
        /// <summary>
        /// Facility identifier
        /// </summary>
        public Guid FacilityId { get; set; }

        /// <summary>
        /// Name of the facility
        /// </summary>
        public string FacilityName { get; set; }

        public string ProxyAgentId { get; set; }

        public DateTime DataSynchronization { get; set; }

        public string IpAddress { get; set; }

        public string LogonName { get; set; }
        public string Password { get; set; }

        public DateTime TimeUpdated { get; set; }

    }
}
