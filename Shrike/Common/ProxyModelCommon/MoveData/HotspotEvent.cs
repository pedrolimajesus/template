using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{
    /// <summary>
    /// Move hotspot event tracking entries / exits in
    /// an area
    /// </summary>
    public class HotspotEvent
    {
        /// <summary>
        /// hot spot id tracked
        /// </summary>
        public int HotspotId { get; set; }

        /// <summary>
        /// entering or exiting
        /// </summary>
        public DirectionType Type { get; set; }

        /// <summary>
        /// object / person tracked
        /// </summary>
        public int ObjectId { get; set; }

        /// <summary>
        /// when this happened.
        /// </summary>
        public DateTime Time { get; set; }

        public override string ToString()
        {
            return string.Format("{0}| Direction:{1} Object {2} at {3}", HotspotId, Type, ObjectId, Time);
        }
    }
}
