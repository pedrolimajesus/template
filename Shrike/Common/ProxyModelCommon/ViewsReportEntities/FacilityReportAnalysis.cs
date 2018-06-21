using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;

namespace Lok.Control.Common.ProxyCommon.ViewsReportEntities
{
    /// <summary>
    /// A one hour time slice of sensor data for a specific facility
    /// analyzed into useful statistics
    /// </summary>
    public class FacilityReportAnalysis
    {
        public FacilityReportAnalysis()
        {
            LookAnalysis = new List<FacilityLookAnalysis>();
            MoveAnalysis = new List<FacilityMoveAnalysis>();
        }

        [DocumentIdentifier]
        public string Id { get; set; }

        /// <summary>
        /// Which facility this data relates to
        /// </summary>
        public string FacilityId { get; set; }

        /// <summary>
        /// The specific hour on the calendar this data relates to
        /// minute and second are set to zero
        /// </summary>
        public DateTime DayAndHour { get; set; }

        /// <summary>
        /// For each look sensor, stats
        /// </summary>
        public IList<FacilityLookAnalysis> LookAnalysis { get; set; }

        /// <summary>
        /// For each move sensor, stats
        /// </summary>
        public IList<FacilityMoveAnalysis> MoveAnalysis { get; set; }
    }

    /// <summary>
    /// A one hour time slice analysis of move data,
    /// for one sensor,
    /// crunched into useful stats.
    /// Rollups of this data type are used to produce
    /// facility reports.
    /// </summary>
    public class FacilityMoveAnalysis
    {

        public FacilityMoveAnalysis()
        {
            BorderAnalysis = new List<MoveSensorAnalysis>();
            HotspotAnalysis = new List<MoveSensorAnalysis>();
        }

        /// <summary>
        /// Id of the device
        /// </summary>
        public string DeviceId { get; set; }
        
        /// <summary>
        /// Name of the device
        /// </summary>
        public string DeviceName { get; set; }


        /// <summary>
        /// Hotspot and border counts
        /// </summary>
        public IList<MoveSensorAnalysis> BorderAnalysis { get; set; }
        public IList<MoveSensorAnalysis> HotspotAnalysis { get; set; } 



        public const int SampleCount = 240;
        

    }

    /// <summary>
    /// Represents how many people were seen
    /// crossing a border or inside a hotspot.
    /// </summary>
    public class MoveSensorAnalysis
    {
        public MoveSensorAnalysis()
        {
            SampleCounts = new List<int>();
            for (int each = 0; each != FacilityMoveAnalysis.SampleCount; each++)
                SampleCounts.Add(0);

            DwellTracking = new Dictionary<int, DateTime>();
        }

        /// <summary>
        /// id of the move hotspot / border
        /// </summary>
        public int SensorId { get; set; }



        /// <summary>
        /// During this time, how many entrances
        /// </summary>
        public int Entrances { get; set; }

        /// <summary>
        /// During this time, how many exits
        /// </summary>
        public int Exits { get; set; }

        /// <summary>
        /// Entrances - Exits
        /// </summary>
        public int Delta { get; set; }

        /// <summary>
        /// Previous Cumulative + Delta -- represents people left
        /// at the end of this time period
        /// </summary>
        public int Cumulative { get; set; }

        /// <summary>
        /// Minute by minute count of people in the look 
        /// sensor at each time
        /// </summary>
        public IList<int> SampleCounts { get; set; }


        public IDictionary<int, DateTime> DwellTracking { get; set; }

        
        /// <summary>
        /// Maximum number of simultaneous people detected in the zone
        /// </summary>
        public int ObjectMax { get; set; }

        /// <summary>
        /// Sampling each minute of the hour, sum of people observed during that minute.
        /// Can be used with PersonSampleCount to
        /// compute averages over a list of facility look analysis
        /// </summary>
        public int ObjectCountMinuteSamplesTotal { get; set; }

        /// <summary>
        /// How many person count samples were accumulated
        /// (minutes with 0 ppl don't count)
        /// </summary>
        public int ObjectCountSampleCount { get; set; }

        /// <summary>
        /// Longest dwell time by a person
        /// </summary>
        public TimeSpan LongestDwell { get; set; }

        /// <summary>
        /// Sum of dwell times for each unique person during the hour
        /// </summary>
        public int DwellTimesTotal { get; set; }

        /// <summary>
        /// How many people dwelt in this zone during the hour
        /// </summary>
        public int DwellTimesSampleCount { get; set; }
    }

   

    /// <summary>
    /// A one hour time slice analysis of look data
    /// for one sensor,
    /// crunched into useful stats.
    /// Rollups of this data type are used to produce
    /// facility reports.
    /// </summary>
    public class FacilityLookAnalysis
    {
        public const int SampleCount = 240;

        public FacilityLookAnalysis()
        {
            SampleCounts = new List<int>();
            for (int each = 0; each != SampleCount; each++)
                SampleCounts.Add(0);

            DwellTracking = new Dictionary<string, DateTime>();


        }

        public static string CreateSessionPersonKey(int session, int person)
        {
            return string.Format("{0}|{1}", session, person);
        }

        /// <summary>
        /// Minute by minute count of people in the look 
        /// sensor at each time
        /// </summary>
        public IList<int> SampleCounts { get; set; }

        public IDictionary<string, DateTime> DwellTracking { get; set; }

        /// <summary>
        /// Id of the device
        /// </summary>
        public string DeviceId { get; set; }
        
        /// <summary>
        /// Name of the device
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// During this time, how many entrances
        /// </summary>
        public int TotalEntrances { get; set; }
        
        /// <summary>
        /// During this time, how many exits
        /// </summary>
        public int TotalExits { get; set; }
        
        /// <summary>
        /// Entrances - Exits
        /// </summary>
        public int TotalDelta { get; set; }
        
        /// <summary>
        /// Previous Cumulative + Delta -- represents people left
        /// at the end of this time period
        /// </summary>
        public int TotalCumulative { get; set; }
        
        /// <summary>
        /// Maximum number of simultaneous people detected in the zone
        /// </summary>
        public int PersonMax { get; set; }

        /// <summary>
        /// Sampling each minute of the hour, sum of people observed during that minute.
        /// Can be used with PersonSampleCount to
        /// compute averages over a list of facility look analysis
        /// </summary>
        public int PersonCountMinuteSamplesTotal { get; set; }
        
        /// <summary>
        /// How many person count samples were accumulated
        /// (minutes with 0 ppl don't count)
        /// </summary>
        public int PersonCountSampleCount { get; set; }
        
        /// <summary>
        /// Longest dwell time by a person
        /// </summary>
        public TimeSpan LongestDwell { get; set; }
        
        /// <summary>
        /// Sum of dwell times for each unique person during the hour
        /// </summary>
        public int DwellTimesTotal { get; set; }
        
        /// <summary>
        /// How many people dwelt in this zone during the hour
        /// </summary>
        public int DwellTimesSampleCount { get; set; }
        
        /// <summary>
        /// Fuck all if I know
        /// </summary>
        public int Active { get; set; }
        
        /// <summary>
        /// Fuck all if I know
        /// </summary>
        public int Passive { get; set; }
        
        /// <summary>
        /// Total unique males in this hour
        /// </summary>
        public int Male { get; set; }
        
        /// <summary>
        /// Total unique females in this hour
        /// </summary>
        public int Female { get; set; }
        
        /// <summary>
        /// Total unique children in this hour
        /// </summary>
        public int Child { get; set; }
        
        
        /// <summary>
        /// Total unique teen in this hour
        /// </summary>
        public int Teen { get; set; }
        
        /// <summary>
        /// Total unique YAs in this hour
        /// </summary>
        public int YoungAdult { get; set; }
        
        /// <summary>
        /// Total unique middle aged in this hour
        /// </summary>
        public int MiddleAged { get; set; }
        
        /// <summary>
        /// Total unique seniors in this hour
        /// </summary>
        public int Senior { get; set; }
        
        /// <summary>
        /// Total unique gen z in this hour
        /// </summary>
        public int GenZ { get; set; }
        
        
        /// <summary>
        /// Total unique gen y in this hour
        /// </summary>
        public int GenY { get; set; }
        
        /// <summary>
        /// Total unique gen x in this hour
        /// </summary>
        public int GenX { get; set; }
        
        /// <summary>
        /// Total unique baby boomers in this hour
        /// </summary>
        public int GenBoomer { get; set; }
        
        
        /// <summary>
        /// Total unique greatest gen in this hour
        /// </summary>
        public int GenGreatest { get; set; }



    }


}
