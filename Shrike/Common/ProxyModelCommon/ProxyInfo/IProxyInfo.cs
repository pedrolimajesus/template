using System;
namespace Lok.Control.Common.ProxyCommon
{
    /// <summary>
    /// Device proxy 
    /// Information
    /// </summary>
    public interface IProxyInfo
    {
        /// <summary>
        /// Human readable name 
        /// </summary>
        string ProxyName { get; set; }

        /// <summary>
        /// Identifier
        /// </summary>
        string ProxyId { get; set; }

        /// <summary>
        /// Location
        /// </summary>
        string Location { get; set; }

        /// <summary>
        /// Info about all devices reporting
        /// to this proxy
        /// </summary>
        ManagedDeviceInfo ManagedDevices { get; set; }

        /// <summary>
        /// settings for the proxy operation
        /// </summary>
        ProxySettings ProxySettings { get; set; }
    }
}
