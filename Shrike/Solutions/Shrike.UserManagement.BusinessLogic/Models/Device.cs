using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon;
using Lok.Unik.ModelCommon.Command;
using Lok.Unik.ModelCommon.Info;
using Lok.Unik.ModelCommon.Interfaces;

namespace Shrike.UserManagement.BusinessLogic.Models
{
    using Tags.UI.Models;

    public class Device
    {
        public Device()
        {
            PackagesAssociates = new List<ContentPackage>();
            ApplicationsAssociates = new List<ApplicationModel>();
        }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\s]*$", ErrorMessage = "Not a Valid Name")]
        [DisplayName("Name:")]
        public string Name { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\s]*$", ErrorMessage = "Not a Valid Description")]
        [DisplayName("Description:")]
        public string Description { get; set; }

        [Required]
        [DisplayName("Model:")]
        public string Model { get; set; }

        [Required]
        [DisplayName("Status:")]
        public string Status { get; set; }

        [DisplayName("Current State:")]
        public string State { get; set; }

        [DisplayName("Time Registered:")]
        public DateTime TimeRegistered { get; set; }

        [DisplayName("Last Contact Time:")]
        public string LastContactTime { get; set; }

        [DisplayName("Tags:")]
        public virtual IList<Tag> Tags { get; set; }

        public Guid Id { get; set; }

        public virtual WindowsInventory Inventory { get; set; }

        public virtual ServerInfo ServerInfo { get; set; }

        public virtual ProviderInfo ProviderInfo { get; set; }

        public virtual IList<Command> Commands { get; set; }

        public virtual DeviceType Type { get; set; }

        public virtual DeviceState CurrentState { get; set; }

        public IList<ContentPackage> PackagesAssociates { get; set; }

        public IList<ApplicationModel> ApplicationsAssociates { get; set; }
    }

    /// <summary>
    /// Set the Request Host OS
    /// </summary>
    public enum HostOs
    {
        WindowsXp, WindowsVista, Windows7
    }

    public class DeviceDetail
    {
        public Device Device { get; set; }
        public User User { get; set; }
        public SchedulePlan SchedulePlan { get; set; }

        public DeviceDetail()
        {
            Device = new Device();
            User = new User();
            SchedulePlan = new SchedulePlan();
        }
    }
}
