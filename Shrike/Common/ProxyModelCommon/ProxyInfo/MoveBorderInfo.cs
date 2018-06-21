using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{


    /// <summary>
    /// A move border trigger.
    /// Defines a line in terms of a vector with
    /// a head and tail. 
    /// Left of the line is defined by 
    /// imagining standing at the tail facing the head
    /// </summary>
    public class MoveBorderInfo
    {
        /// <summary>
        /// Identifier for the border
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Tail of the vector, where it begins
        /// </summary>
        public Point InitalPoint { get; set; }

        /// <summary>
        /// Head of the vector, where it ends
        /// </summary>
        public Point TerminalPoint { get; set; }

        /// <summary>
        /// Name of area to the left of the line
        /// </summary>
        public string LeftName { get; set; }

        /// <summary>
        /// Name of the area to the right of the line
        /// </summary>
        public string RightName { get; set; }


        public override string ToString()
        {
            return string.Format("Border {0}, {1} to {2}; left:{3}, right:{4}", Id, InitalPoint, TerminalPoint, LeftName,
                                 RightName);
        }
    }
}
