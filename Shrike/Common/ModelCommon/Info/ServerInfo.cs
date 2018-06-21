using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Info
{
    public class ServerInfo
    {
        public string HostName { get; set; }
        public string HostOS { get; set; }
        public string HostVersion { get; set; }
        public string IPAddress { get; set; }
        public IList<ProviderInfo> Providers { get; set; }
    }
}
