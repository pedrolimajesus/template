using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// Move hotspot trigger
    /// </summary>
    public class MoveHotspotInfo
    {
        /// <summary>
        /// Identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Color mask identifying the trigger area
        /// </summary>
        public MaskColor MaskColor { get; set; }

        /// <summary>
        /// Area name
        /// </summary>
        public string Name { get; set; }
    }
}
