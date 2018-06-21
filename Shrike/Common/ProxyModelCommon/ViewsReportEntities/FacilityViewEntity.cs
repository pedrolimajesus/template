using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Control.Common.ProxyCommon.LookData;

namespace Lok.Control.Common.ProxyCommon.ViewsReportEntities
{
    public class FacilityViewTally
    {
        /// <summary>
        /// Equal to aware device id this record corresponds to
        /// </summary>
        public string Id { get; set; }

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

        public int Current { get; set; }

    }
}
