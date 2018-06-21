using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// Move dwell event 
    /// </summary>
    public class DwellEvent
    {
        /// <summary>
        /// person being tracked
        /// </summary>
        public int ObjectId { get; set; }

        /// <summary>
        /// Event type -- detected, lost, etc
        /// </summary>
        public DwellType Type { get; set; }

        /// <summary>
        /// UTC time event occurred
        /// </summary>
        public DateTime Time { get; set; }

        public override string ToString()
        {
            return string.Format("Object: {0} Type:{1} at {2}", ObjectId, Type, Time);
        }
    }
}
