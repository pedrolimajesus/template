using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using LoK.ManagedApp;
using Lok.Unik.ModelCommon.Client;

//using Lok.Unik.ModelCommon.Client;

namespace Shrike.UserManagement.BusinessLogic.Models
{
    public class SchedulePlan
    {
        public SchedulePlan()
        {
            DevicesAssociates = new List<Device>();
        }

        public Guid Id { get; set; }

        [Required]
        [DisplayName("Icon")]
        public string Icon { get; set; }

        [Required]
        [DisplayName("Name")]
        public string Name { get; set; }

        [Required]
        [DisplayName("Schedule Time /wk")]
        public string ScheduleTime { get; set; }

        [Required]
        [DisplayName("Device Count")]
        public string DeviceCount { get; set; }

        [Required]
        [DisplayName("Next Schedule")]
        public string NextSchedule { get; set; }

        [Required]
        [DisplayName("TimeLineSchedule")]
        public ICollection<TimeLineSchedule> TimeLineScheduleDatas { get; set; }

        public ICollection<Tags.UI.Models.Tag> Tags { get; set; }

        public SchedulePlanStatus Status { get; set; }

        public IList<Device> DevicesAssociates { get; set; }
    }


    public class LayoutTemplate
    {
        public LayoutTemplate()
        {
            StandardViewPorts = new List<StandardViewPorts>();
        }

        public IEnumerable<StandardViewPorts> StandardViewPorts { get; set; }
        public string SrcImage { get; set; }
        public Guid Id { get; set; }
        //public 
    }
}
