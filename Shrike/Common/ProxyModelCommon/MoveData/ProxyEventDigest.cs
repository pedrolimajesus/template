using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lok.Control.Common.ProxyCommon
{
    /// <summary>
    /// Digest representing sensor snapshots from 
    /// look, move and falo devices
    /// </summary>
    
    public class ProxyEventDigest
    {
        /// <summary>
        /// Look sensor snapshots
        /// </summary>
        public List<LookDeviceDigest> LookDevices { get; set; }

        /// <summary>
        /// Move sensor snapshots
        /// </summary>
        public List<MoveDeviceDigest> MoveDevices { get; set; }

        /// <summary>
        /// Falo data
        /// </summary>
        public List<FaloServerDigest> FaloServers { get; set; }


        public ProxyEventDigest()
        {
            LookDevices = new List<LookDeviceDigest>();
            MoveDevices = new List<MoveDeviceDigest>();
            FaloServers = new List<FaloServerDigest>();
        }

        public override string ToString()
        {
            return string.Format("{0} look sensors, {1} move sensors, {2} falo servers",
                                 LookDevices.Count, MoveDevices.Count, FaloServers.Count);
        }
    }
}
