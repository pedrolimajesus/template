///////////////////////////////////////////////////////////
//  IDevice.cs
//  Implementation of the Interface IDevice
//  Generated by Enterprise Architect
//  Created on:      14-Sep-2012 14:41:50
///////////////////////////////////////////////////////////

namespace Lok.Unik.ModelCommon.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Lok.Unik.ModelCommon.Info;
    using Lok.Unik.ModelCommon.Events;
    using Lok.Unik.ModelCommon.ItemRegistration;

    public enum DeviceType
    {
        Mobile,

        Kiosk, 

        ControlProxyAgent,

        LookSensor,

        MoveSensor
    }

    public enum DeviceState
    {
        Created,

        Registered,

        Deleted,

        Enabled,

        Disabled
    }

    public interface IDevice : IRegistrableItem
    {
        string IdString { get; set; }

        WindowsInventory Inventory { get; set; }

        ServerInfo ServerInfo { get; set; }

        ProviderInfo ProviderInfo { get; set; }

        IList<Lok.Unik.ModelCommon.Command.Command> Commands { get; set; }

        DeviceType Type { get; set; }

        string Name { get; set; }

        string Description { get; set; }

        string Model { get; set; }

        DeviceState CurrentState { get; set; }

        HealthStatus CurrentStatus { get; set; }

        Guid ScheduleId { get; set; }
    }

    //end IDevice
}

//end namespace System