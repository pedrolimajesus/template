using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Control.Common.ProxyCommon.Interfaces;

namespace Lok.Unik.ModelCommon.Proxy
{
    public class ProxySensorDataRequest : ManifestItem
    {
        public ProxySensorDataRequest()
        {
            DeviceRequestSummaries = new List<DeviceRequestSummary>();

        }
        
        public bool UseMockData { get; set; }
        public IList<DeviceRequestSummary> DeviceRequestSummaries { get; set; }
    }


    public class DeviceRequestSummary
    {
        public string Id { get; set; }
        public string IpAddress { get; set; }
        public DateTime DataSynchronization { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public DeviceType DeviceType { get; set; }
    }
}
