using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Control.Common.ProxyCommon.LookData;

namespace Lok.Control.Common.ProxyCommon.ViewsReportEntities
{


    public class DeviceViewTally
    {
        public DeviceViewTally()
        {
            RecentLookEvents = new List<LookPerson>();
            RecentBorderEvents = new List<BorderEvent>();
            RecentDwellEvents = new List<DwellEvent>();
            RecentHotspotEvents = new List<HotspotEvent>();
            TriggerTallies = new List<TriggerTally>();
            
            Tally15Minutes = 0;
            Tally60Minutes = 0;
            TallyToday = 0;
        }

        /// <summary>
        /// Equal to aware device id this record corresponds to
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Used by the proxy sensor provider to calculate
        /// tallies over the past day
        /// </summary>
        public IList<LookPerson> RecentLookEvents { get; set; }

        /// <summary>
        /// Used by the proxy sensor provider to calculate
        /// tallies over the past day
        /// </summary>
        public IList<DwellEvent> RecentDwellEvents { get; set; }

        /// <summary>
        /// Used by the proxy sensor provider to calculate
        /// tallies over the past day
        /// </summary>
        public IList<BorderEvent> RecentBorderEvents { get; set; }
        /// <summary>
        /// Used by the proxy sensor provider to calculate
        /// tallies over the past day
        /// </summary>
        public IList<HotspotEvent> RecentHotspotEvents { get; set; }

        /// <summary>
        /// Total unique people last 15
        /// </summary>
        public int Tally15Minutes { get; set; }

        /// <summary>
        /// Total unique people last hour
        /// </summary>
        public int Tally60Minutes { get; set; }

        /// <summary>
        /// Total unique people today
        /// </summary>
        public int TallyToday { get; set; }

        /// <summary>
        /// If the sensor has triggers, the tallies for each trigger
        /// </summary>
        public IList<TriggerTally> TriggerTallies { get; set; }

    }

    public enum TriggerType
    {
        Border, 
        Hotspot,
        Zone
    }

    public class TriggerTally
    {
        public string TriggerName { get; set; }

        public int TriggerId { get; set; }

        public TriggerType Type { get; set; }

        public int Tally15Minutes { get; set; }
        public int Tally60Minutes { get; set; }
        public int TallyToday { get; set; }
    }
}
