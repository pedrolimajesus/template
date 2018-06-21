using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Control.Common.ProxyCommon;
using Lok.Control.Common.ProxyCommon.Interfaces;
using Lok.Unik.ModelCommon.Aware;

namespace Lok.Unik.ModelCommon.Proxy
{

    

    public class ProxySensorDataResponse : ManifestItem
    {
        public int SyncNumber { get; set; }

        public List<LookDeviceDigest> LookDevices { get; set; }
        public List<MoveDeviceDigest> MoveDevices { get; set; }

        public FacilitySimulationTest SimulationSetup { get; set; }
    }

    public class FacilitySimulationTest
    {
        public SimFacilityDefinition TestFacility { get; set; }
        public List<SimDeviceDefinition> Devices { get; set; }
        
    }


    public class SimFacilityDefinition
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Area { get; set; }

        public string Store { get; set; }

    }


    public class SimDeviceDefinition
    {
        public string Id { get; set; }

        public int Type { get; set; }

        public string Name { get; set; }

        public DeviceTrigger[] TriggerDefs { get; set; }

    }

}
