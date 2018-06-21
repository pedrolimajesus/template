using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Aware;

namespace Lok.Unik.ModelCommon.Proxy
{
    public class ProxySensorMgntRequest : ManifestItem
    {
        public bool UploadSensorMgntData { get; set; }
        public IList<SensorCommand> PendingCommands { get; set; }
        public IList<DeviceRequestSummary> DeviceRequestSummaries { get; set; }
    }
}
