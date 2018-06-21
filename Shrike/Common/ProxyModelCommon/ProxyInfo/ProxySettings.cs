using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// Proxy operation settings
    /// </summary>
    public class ProxySettings
    {
        /// <summary>
        /// How often to upload sensor data
        /// </summary>
        int DigestUploadIntervalInMinutes { get; set; }

        /// <summary>
        /// Server to send data to
        /// </summary>
        string ControlServer { get; set; }
    }
}
