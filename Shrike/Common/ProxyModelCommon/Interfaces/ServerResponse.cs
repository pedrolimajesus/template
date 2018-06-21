using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon.Interfaces
{

    /// <summary>
    /// Represents a command from Aware to the proxy
    /// </summary>
    public class ServerResponse
    {
        /// <summary>
        /// Type of command
        /// </summary>
        public ProxyCommand ProxyCommand { get; set; }

        /// <summary>
        /// Context sensitive (depending on command type)
        /// arguments for the command
        /// </summary>
        public string Data { get; set; }
    }


    /// <summary>
    /// Type of commands from aware to the proxy agent
    /// </summary>
    public enum ProxyCommand
    {
        /// <summary>
        /// No work for the proxy to do
        /// </summary>
        NoCurrentRequest,     

        /// <summary>
        /// Update your policy with this data
        /// </summary>
        PolicyUpdate,         

        /// <summary>
        /// Send this file directory to the data warehouse
        /// </summary>
        SendDirectory,        

        /// <summary>
        /// Send pictures from devices.  If Data is null... for all devices.  
        /// Otherwise from devices named in Data.
        /// </summary>
        SendDevicePict        
    }
}
