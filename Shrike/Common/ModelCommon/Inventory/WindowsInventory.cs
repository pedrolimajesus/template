using System.Collections.Generic;
using System;
using Lok.Unik.ModelCommon.Inventory;

namespace Lok.Unik.ModelCommon
{
    using CounterData = Lok.Unik.ModelCommon.Inventory.CounterData;

    public class WindowsInventory : ManifestItem
    {
        public WindowsInventory()
        {
            NetworkConfig = new NetworkConfig();
            OSInfo = new OSInfo();
            DeviceInfo = new DeviceInfo();
            HealthDataManifestResponse = new HealthDataManifestResponse() { Events = new List<EventData>(), CounterSamples = new List<CounterData>()};
        }

        public HealthDataManifestResponse HealthDataManifestResponse { get; set; }
        
        public Guid DeviceId { get; set; }
        public NetworkConfig NetworkConfig { get; set; }
        public OSInfo OSInfo { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
        public DateTime LastScreenShotTaken { get; set; }
    }
}
