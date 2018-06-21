using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{
    /// <summary>
    /// Describes where and when a person was seen by Move
    /// </summary>
    public class Position
    {
        /// <summary>
        /// x coord.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// y coord.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// When the placement of this person was
        /// observed.
        /// </summary>
        public DateTime Time { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: ({1},{2})", Time, X, Y);
        }

    }
}
