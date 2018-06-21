using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// Move movement direction types
    /// </summary>
    public enum EventType
    {
        /// <summary>
        /// Tracking object left to right
        /// over trigger
        /// </summary>
        left_to_right, 
        
        /// <summary>
        /// Tracking object right to left over trigger
        /// </summary>
        right_to_left
    }


    public enum Interpretation
    {
        None,
        Entrance,
        Egress,
        Internal
    }
}
