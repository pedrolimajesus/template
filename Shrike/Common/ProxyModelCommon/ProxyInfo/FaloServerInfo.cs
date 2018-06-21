using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// 
    /// </summary>
    public class FaloServerInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string DeviceIP { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public FaloServerTypes DeviceType { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public string Location { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum FaloServerTypes
    {
        /// <summary>
        /// 
        /// </summary>
        FaloIdentityServer, 
        
        /// <summary>
        /// 
        /// </summary>
        FaloProfileServer
    }
}
