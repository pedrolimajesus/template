using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon
{
    public class NetworkConfig
    {
        public string IpAddress { get; set; }
        public string MacAddress { get; set; }
        public string MachineName { get; set; }
        public string DefaultGateway { get; set; }
        public string SubnetMask { get; set; }
    }
}
