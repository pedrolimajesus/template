using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// Aware proxy managed devices
    /// </summary>
    public class ManagedDeviceInfo
    {
        /// <summary>
        /// How many look sensors
        /// </summary>
        public int NumberOfLookDevices { get; set; }
        
        /// <summary>
        /// How many move sensors
        /// </summary>
        public int NumberOfMoveDevices { get; set; }
        
        /// <summary>
        /// How many falo sensors
        /// </summary>
        public int NumberOfFaloServers { get; set; }

        /// <summary>
        /// All the look devices
        /// </summary>
        public List<LookDeviceInfo> LookList { get; set; }
        
        /// <summary>
        /// All the move devices
        /// </summary>
        public List<MoveDeviceInfo> MoveList { get; set; }
        
        /// <summary>
        /// All the falo devices
        /// </summary>
        public List<FaloServerInfo> FaloServersList { get; set; }
    }
}
