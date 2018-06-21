using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon.Interfaces
{

    /// <summary>
    /// Type of proxy agent request to Aware
    /// </summary>
    public class ProxyRequest
    {
        public RequestType RequestType { get; set; }
    }

    

    /// <summary>
    /// Note: RequestTypes should only ever be added to for upward compatibility
    /// </summary>
    public enum RequestType
    {
        /// <summary>
        /// Anything for me to do+
        /// </summary>
        RequestForCommand,  

        /// <summary>
        /// Notify aware this proxy agent is shutting down
        /// </summary>
        ShuttingDown,      

        /// <summary>
        /// Notify aware this proxy agent is starting up
        /// </summary>
        StartingUp,         

        /// <summary>
        /// Request for the policy for calling ProxyDevice
        /// </summary>
        SendPolicyConfig,   

        /// <summary>
        /// Notify aware this proxy agent is being decommissioned
        /// </summary>
        DecommissionProxy    
    }
}
