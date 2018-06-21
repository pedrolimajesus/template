using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon
{
    /// <summary>
    /// Describes the path of a person tracked by move through
    /// the sensor space.
    /// </summary>
    public class PersonTrack
    {
        /// <summary>
        /// Id of the person tracked.
        /// </summary>
        public int ObjectId { get; set; }

        /// <summary>
        /// The path
        /// </summary>
        public List<Position> Positions { get; set; }

        public PersonTrack()
        {
            Positions = new List<Position>();
        }

        public override string ToString()
        {
            return string.Format("Tracking person {0} through: {1}...",
                                 ObjectId,
                                 (null == Positions)
                                     ? "no data"
                                     : string.Join(",", 
                                                Positions.Take(5).Select(p=>p.ToString()))
                );
        }
    }
}
