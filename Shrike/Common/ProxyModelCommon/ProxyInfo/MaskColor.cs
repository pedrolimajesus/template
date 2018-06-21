using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{

    /// <summary>
    /// For a move sensor, the color mask used
    /// to describe an area to watch
    /// </summary>
    public class MaskColor
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }


        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", R, G, B);
        }
    }
}
