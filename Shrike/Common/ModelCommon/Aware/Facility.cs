using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Lok.Control.Common.ProxyCommon;
using Newtonsoft.Json;


namespace Lok.Unik.ModelCommon.Aware
{
    using Lok.Unik.ModelCommon.Client;
    using Lok.Unik.ModelCommon.Interfaces;

    public class DeviceCount
    {
        public int Count { get; set; }
        public DeviceType Type { get; set; }
    }



    public class HealthCount
    {
        public int Count { get; set; }
        public DeviceStatus DeviceHealth { get; set; }
    }

    public class Facility : ITaggableEntity, ITimeFilter
    {
        

        public Facility()
        {
            Id = Guid.Empty;
            Tags = new List<Tag>();
            DeviceCounts = new List<DeviceCount>();
            Health = new List<HealthCount>();
            TimeRegistered = DateTime.UtcNow;
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Area { get; set; }

        public string Store { get; set; }

        public IList<DeviceCount> DeviceCounts { get; set; }

        public IList<HealthCount> Health { get; set; }

        public string PictureUrl { get; set; }

        public DateTime TimeRegistered { get; set; }

        public DateTime TimeUpdated { get; set; }

        #region Implementation of ITaggableEntity

        public IList<Tag> Tags { get; set; }

        #endregion

        public int RecordHighArrival { get; set; }
        public DateTime RecordHighArrivalHour { get; set; }
        public int PreviousRecordHighArrival { get; set; }
        public DateTime PreviousRecordHighArrivalHour { get; set; }

        public int RecordHighCount { get; set; }
        public DateTime RecordHighCountHour { get; set; }
        public int PreviousRecordHighCount { get; set; }
        public DateTime PreviousRecordHighCountHour { get; set; }

        #region AwareDevices
        [JsonIgnore]
        [Raven.Imports.Newtonsoft.Json.JsonIgnore]
        [XmlIgnore]
        private IList<AwareDevice> _devices;

        [JsonIgnore]
        [Raven.Imports.Newtonsoft.Json.JsonIgnore]
        [XmlIgnore]
        public IList<AwareDevice> Devices
        {
            get { return _devices ?? (_devices = new List<AwareDevice>()); }
            set
            {
                _devices = value;

                AwareDevicesIds.Clear();

                if (value == null || !value.Any())
                {   
                    return;
                }

                foreach (var device in _devices)
                {
                    AwareDevicesIds.Add(device.Id);
                }
            }
        }

        private IList<Guid> _awareDevicesIds;

        public IList<Guid> AwareDevicesIds
        {
            get
            {
                return _awareDevicesIds ?? (_awareDevicesIds = new List<Guid>());
            }
            set { _awareDevicesIds = value; }
        }

        #endregion

        #region OuterDoors Cash Register
        private IList<int> _outerDoorsIds;

        public IList<int> OuterDoorsIds
        {
            get
            {
                return _outerDoorsIds ?? (_outerDoorsIds = new List<int>());
            }
            set
            {
                if (value == null)
                {
                    _outerDoorsIds = null; 
                    return;
                }

                _outerDoorsIds = value;
            }
        }

        private IList<int> _cashRegistersIds;

        public IList<int> CashRegistersIds
        {
            get
            {
                return _cashRegistersIds ?? (_cashRegistersIds = new List<int>());
            }
            set
            {
                if (value == null)
                {
                    _cashRegistersIds = null;
                    return;
                }

                _cashRegistersIds = value;
            }
        }

        [JsonIgnore]
        [Raven.Imports.Newtonsoft.Json.JsonIgnore]
        [XmlIgnore]
        private IList<DeviceTrigger> _outerDoors;

        [JsonIgnore]
        [Raven.Imports.Newtonsoft.Json.JsonIgnore]
        [XmlIgnore]
        public IList<DeviceTrigger> OuterDoors
        {
            get { return _outerDoors ?? (_outerDoors = new List<DeviceTrigger>()); }
            set
            {
                if (value == null || !value.Any())
                {
                    _outerDoors = value;
                    OuterDoorsIds.Clear();
                    return;
                }

                var ids = new List<int>();
                foreach (var trigger in value)
                {
                    if (trigger.Disabled)
                    {
                        throw new ApplicationException("Disabled sensors cannot be used as outer doors");
                    }

                    if (trigger.Type != TriggerType.MoveBorder)
                    {
                        throw new ApplicationException("Only area sensors can be used as outer doors");
                    }

                    ids.Add(trigger.Id);
                }

                _outerDoors = value;
                _outerDoorsIds = ids;
            }
        }

        [JsonIgnore]
        [Raven.Imports.Newtonsoft.Json.JsonIgnore]
        [XmlIgnore]
        private IList<DeviceTrigger> _cashRegisters;

        [JsonIgnore]
        [Raven.Imports.Newtonsoft.Json.JsonIgnore]
        [XmlIgnore]
        public IList<DeviceTrigger> CashRegisters
        {
            get { return _cashRegisters ?? (_cashRegisters = new List<DeviceTrigger>()); }
            set
            {
                if (value == null || !value.Any())
                {
                    _cashRegisters = value;
                    CashRegistersIds.Clear();
                    return;
                }

                var ids = new List<int>();
                foreach (var trigger in value)
                {
                    if (trigger.Disabled)
                    {
                        throw new ApplicationException("Disabled sensors cannot be used as cash registers");
                    }

                    if (trigger.Type != TriggerType.MoveHotspot)
                    {
                        throw new ApplicationException("Only area sensors can be used as cash registers");
                    }
                    
                    ids.Add(trigger.Id);
                }
                
                _cashRegisters = value;
                _cashRegistersIds = ids;
            }
        }
        #endregion
    }
}