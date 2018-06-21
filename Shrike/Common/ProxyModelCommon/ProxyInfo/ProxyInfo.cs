using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// Implementation of IProxyInfo
    /// </summary>
    public class ProxyInfo : Lok.Control.Common.ProxyCommon.IProxyInfo
    {
        /// <summary>
        /// Human readable name of the proxy agent
        /// </summary>
        public string ProxyName { get; set; }

        /// <summary>
        /// Identifier of the Proxy Agent
        /// </summary>
        public string ProxyId { get; set; }

        /// <summary>
        /// Location
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// All devices managed by the proxy agent
        /// </summary>
        public ManagedDeviceInfo ManagedDevices { get; set; }

        /// <summary>
        /// Settings of the proxy agent
        /// </summary>
        public ProxySettings ProxySettings { get; set; }
    }

    public class ProxyRegResponse
    {
    }
}
