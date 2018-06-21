using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Control.Common.ProxyCommon;
using Lok.Control.Common.ProxyCommon.Interfaces;
using Lok.Unik.ModelCommon.Aware;

namespace Lok.Unik.ModelCommon.Proxy
{
    

    public class ProxySensorMgntResponse : ManifestItem
    {
        public ProxySensorMgntResponse()
        {
            ManagedDevices = new ManagedDeviceInfo();
            DeviceCommands = new List<SensorCommand>();
            MoveDeviceDefinitions = new List<AwareMoveDefinition>();
            DeviceRegistrationRequests = new List<DeviceRegistrationRequest>();
        }

        public int SyncNumber { get; set; }

        public ManagedDeviceInfo ManagedDevices { get; set; }
        public IList<SensorCommand> DeviceCommands { get; set; }
        public IList<AwareMoveDefinition> MoveDeviceDefinitions { get; set; }
        public IList<DeviceRegistrationRequest> DeviceRegistrationRequests { get; set; } 
    }
}
