using System.ComponentModel.DataAnnotations;
using AppComponents;
using LoK.ManagedApp;
using System;
using System.Collections.Generic;
using Lok.Unik.ModelCommon.Interfaces;

namespace Lok.Unik.ModelCommon.Client
{
    using Raven.Imports.Newtonsoft.Json;
    using Raven.Imports.Newtonsoft.Json.Converters;

    public class SchedulePlan : ITaggableEntity
    {
        public SchedulePlan()
        {
            Id = Guid.NewGuid();

            Tags = new List<Tag>();
            TimeLineSchedules = new List<TimeLineSchedule>();
            
            ApplyTo = new List<Tag>();
            DoNotApplyTo = new List<Tag>();
            Placements = new List<PortalPlacement>();

        }


        #region Implementation of ITaggableEntity

        public IList<Tag> Tags { get; set; }

        [DocumentIdentifier]
        public Guid Id { get; set; }

        #endregion

        public string Name { get; set; }
        
        public IList<Tag> ApplyTo { get; set; }
        public IList<Tag> DoNotApplyTo { get; set; }

        public IList<PortalPlacement> Placements { get; set; }

        public IList<TimeLineSchedule> TimeLineSchedules { get; set; }

        public IList<TimeLine> DefaultTimeLines { get; set; }

        public SchedulePlanStatus Status { get; set; }
    }

    public enum SchedulePlanStatus
    {
        Active,
        InActive,
        Disable
    }

    public class TimeLineSchedule
    {
        public TimeLineSchedule()
        {
            Id = Guid.NewGuid();
            TimeLines = new List<TimeLine>();
            DaysOfWeek = new List<Day>();
            Period = new AppointmentTime();
        }

        [DocumentIdentifier]
        public Guid Id { get; set; }

        public string Title { get; set; }
        
        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\s]*$", ErrorMessage = "Not a Valid Description for Schedule")]
        public string Description { get; set; }


        public ICollection<TimeLine> TimeLines { get; set; }


        public ICollection<Day> DaysOfWeek { get; set; }


        public AppointmentTime Period { get; set; }

    }

    public class Day
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public DayOfWeek DayOfWeek { get; set; }
        public bool Scheduled { get; set; }
    }

    public class TimeLine
    {
        public Guid Application { get; set; }

        public Guid ContentPackage { get; set; }

        public string Placement { get; set; }
    }

    
    //Appointment Recurrence
    public class AppointmentTime
    {
        public TimeSpan Start { get; set; }

        public TimeSpan Duration { get; set; }

        public TimeSpan End { get; set; }
    }

    
}