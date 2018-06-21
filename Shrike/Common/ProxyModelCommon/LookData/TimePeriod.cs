using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon.LookData
{
    /// <summary>
    /// Describes how long a person detected by look 
    /// was tracked by the sensor, and when.
    /// </summary>
    public class TimePeriod
    {
        /// <summary>
        /// When the person was first seen.
        /// </summary>
        public DateTime EnterTime { get; set; }

        /// <summary>
        /// When the person was last seen.
        /// </summary>
        public DateTime ExitTime { get; set; }

        public override string ToString()
        {
            return string.Format("First seen: {0}; Last seen {1}", EnterTime, ExitTime);
        }
    }
}
