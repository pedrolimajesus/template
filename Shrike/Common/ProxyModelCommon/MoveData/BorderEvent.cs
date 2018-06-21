using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// Describes the event of a person
    /// crossing a move border.
    /// </summary>
    public class BorderEvent
    {
        /// <summary>
        /// Id of the border.
        /// </summary>
        public int BorderId { get; set; }

        /// <summary>
        /// Crossing direction over the border
        /// </summary>
        public EventType Direction { get; set; }

        /// <summary>
        /// Coming or going?
        /// </summary>
        public Interpretation Interpretation { get; set; }

        /// <summary>
        /// Considered an outside door / border?
        /// </summary>
        public bool OuterBorder { get; set; }

        /// <summary>
        /// Id of the person recognized by the sensor
        /// </summary>
        public int ObjectId { get; set; }

        /// <summary>
        /// Time that this event occurred.
        /// </summary>
        public DateTime Time { get; set; }

        public override string ToString()
        {
            return string.Format("Id {0} crossed by {1} {2} at {3}", BorderId,
                                 ObjectId, Direction, Time);
        }
    }
}
