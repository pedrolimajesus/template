using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Control.Common.ProxyCommon;

namespace Lok.Unik.ModelCommon.Aware
{
    public enum TriggerType
    {
        None,
        Look,
        MoveBorder,
        MoveHotspot
    }

    public class DeviceTrigger
    {
        public DeviceTrigger()
        {
            Id = 0;
            Disabled = false;
        }

        /// <summary>
        /// Identifier
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Trigger events for the day
        /// </summary>
        public int DayCount { get; set; }

        /// <summary>
        /// Trigger events for the hour
        /// </summary>
        public int HourCount { get; set; }

        /// <summary>
        /// Health
        /// </summary>
        public int Health { get; set; }

        /// <summary>
        /// Trigger events for the last ten minutes
        /// </summary>
        public int TenMinuteCount { get; set; }

        /// <summary>
        /// Device name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// What type of device trigger is this?
        /// </summary>
        public TriggerType Type { get; set; }

        /// <summary>
        /// Hash of the latest update about this trigger
        /// </summary>
        public int SpecHash { get; set; }

        /// <summary>
        /// object whose properties specify the information
        /// about the trigger
        /// </summary>
        public object Specification { get; set; }

        /// <summary>
        /// Type of the specification object
        /// </summary>
        public Type SpecificationType { get; set; }

        /// <summary>
        /// If disabled, this trigger is no longer
        /// active on the sensor and should not be 
        /// picked for facilities.
        /// </summary>
        public bool Disabled { get; set; }
    }
}
