namespace Lok.Unik.ModelCommon.Client
{
    using System;
    using System.Collections.Generic;

    using Lok.Unik.ModelCommon.Info;
    using Lok.Unik.ModelCommon.Interfaces;
    using Lok.Unik.ModelCommon.Events;

    public abstract class Device : IDevice
    {
        public Device()
        {
            Commands = new List<Lok.Unik.ModelCommon.Command.Command>();
            Tags = new List<Tag>();
        }

        //RavenDB
        public virtual Guid Id { get; set; }

        public string IdString
        {
            get
            {
                return Id.ToString();
            }
            set
            {
                Guid guid;
                if (!string.IsNullOrEmpty(value) && Guid.TryParse(value, out guid))
                {
                    Id = guid;
                }
            }
        }

        public virtual WindowsInventory Inventory { get; set; }

        public virtual ServerInfo ServerInfo { get; set; }

        public virtual ProviderInfo ProviderInfo { get; set; }

        public virtual IList<Lok.Unik.ModelCommon.Command.Command> Commands { get; set; }

        public virtual DeviceType Type { get; set; }

        public virtual string Name { get; set; }

        public virtual string Description { get; set; }

        public virtual string Model { get; set; }

        public virtual DeviceState CurrentState { get; set; }

        public virtual IList<Tag> Tags { get; set; }

        public virtual DateTime TimeRegistered { get; set; }

        public virtual HealthStatus CurrentStatus { get; set; }

        public virtual Guid ScheduleId { get; set; }
    }
}