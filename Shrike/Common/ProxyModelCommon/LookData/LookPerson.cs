using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon.LookData
{
    /// <summary>
    /// Describes a person who was tracked or is being tracked
    /// by a Look sensor.
    /// </summary>
    public class LookPerson
    {
        /// <summary>
        /// Identifier
        /// </summary>
        public int Id { get; set; }


        /// <summary>
        /// Look session Id
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// Time the person was first seen
        /// </summary>
        public DateTime EnterTime { get; set; }

        /// <summary>
        /// Time the person was last seen
        /// </summary>
        public DateTime ExitTime { get; set; }

        /// <summary>
        /// Did we see them leave or lose track?
        /// </summary>
        public bool CleanExit { get; set; }

        /// <summary>
        /// Times that we saw them during the session
        /// </summary>
        public List<TimePeriod> TimePeriods { get; set; }

        /// <summary>
        /// Estimated age
        /// </summary>
        public double Age { get; set; }

        /// <summary>
        /// How confident Look is about the age assessment.
        /// </summary>
        public double AgeConfidence { get; set; }

        /// <summary>
        /// Estimated gender.
        /// </summary>
        public Gender Gender { get; set; }
        
        /// <summary>
        /// How confident Look is about the gender assessment.
        /// </summary>
        public double GenderConfidence { get; set; }

        /// <summary>
        /// How long we saw the person.
        /// </summary>
       
        public TimeSpan Dwell
        {
            get
            {
                if (null == TimePeriods)
                    return TimeSpan.Zero;

                var ms = (from timePeriod in TimePeriods
                          where null != timePeriod.EnterTime && null != timePeriod.ExitTime
                          select (timePeriod.ExitTime - timePeriod.EnterTime).TotalMilliseconds)
                          .Sum();

                return TimeSpan.FromMilliseconds(ms);

                
            }

            set
            {
                // ignore; raven db json serialization of this class becomes problematic without this
            }
        }

        public override string ToString()
        {
            return string.Format("Id {3}, Session Id {4}, Gender: {0} | Age: {1} | Dwell Time: {2}", 
                Gender, Age, Dwell, Id, SessionId);
        }
    }

    /// <summary>
    /// Describes the assessed gender of a look person
    /// </summary>
    public enum Gender
    {
        /// <summary>
        /// Assessed as male
        /// </summary>
        Male, 
        
        /// <summary>
        /// Assessed as female
        /// </summary>
        Female,

        /// <summary>
        /// Unknown gender
        /// </summary>
        Unknown
    }

    
}
