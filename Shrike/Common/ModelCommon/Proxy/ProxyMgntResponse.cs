using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Control.Common.ProxyCommon;

namespace Lok.Unik.ModelCommon.Proxy
{
    public class ProxyMgntResponse : ManifestItem
    {
        // Proxy Settings Info
        public int SyncNumber { get; set; }
        public string ProxyName { get; set; }
        public string ProxyId { get; set; }
        public string Location { get; set; }
    }
}
