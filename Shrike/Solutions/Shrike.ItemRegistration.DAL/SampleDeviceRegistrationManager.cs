using System;
using System.Drawing;
using System.Linq;

namespace Shrike.ItemRegistration.DAL
{
    using System.Web;

    using AppComponents;
    using AppComponents.Raven;

    using Lok.Unik.ModelCommon;
    using Lok.Unik.ModelCommon.Client;
    using Lok.Unik.ModelCommon.Events;
    using Lok.Unik.ModelCommon.Interfaces;
    using Lok.Unik.ModelCommon.ItemRegistration;

    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class SampleDeviceRegistrationManager : IItemRegistrationManager<IDevice>
    {
        #region Implementation of IItemRegistrationManager<out IDevice>

        public IDevice RegisterItem(ItemRegistrationResult itemRegistrationResult, ItemRegistration itemRegistration)
        {
            var net = new NetworkConfig
                {
                    IpAddress = "128.0.0.1",
                    MacAddress = "macAddress",
                    MachineName = "name",
                    DefaultGateway = "198.0.0.2",
                    SubnetMask = "200.200.200.1"
                };

            var os = new OSInfo
                { OsName = "nameOS", OsType = "Win", OsVersion = "10", OsBits = 64, OsDate = "34-34-34" };

            var dev = new DeviceInfo
                {
                    Processor = "proc",
                    MemoryInBytes = "123",
                    PrinterName = "canon",
                    NumberOfMonitors = 3,
                    PrimaryMonitorName = "primary mon"
                };

            var path = string.Format(
                "{0}{1}/{2}",
                DocumentStoreLocator.SchemeRavenRoute,
                DocumentStoreLocator.GetStoreType(),
                itemRegistration.TenancyId);

            IDevice device;
            using (var session = DocumentStoreLocator.Resolve(path))
            {
                var tagsDevRegistration =
                    itemRegistration.Tags.Where(tag => tag.Category.Color != KnownColor.Transparent).ToList();
                var name = string.Format("Device {0}", DateTime.UtcNow.ToShortTimeString());
                tagsDevRegistration.Add(
                    new Tag
                        {
                            Id = Guid.NewGuid(),
                            Type = TagType.Device,
                            Attribute = name,
                            Value = itemRegistration.Id.ToString(),
                            CreateDate = DateTime.UtcNow,
                            Category =
                                new TagCategory { Name = name, Color = KnownColor.Transparent }
                        });

                device = new Kiosk
                    {
                        Id = itemRegistrationResult.ItemId,
                        Name = name,
                        Description = string.Format("Kiosk Device Created  {0}", DateTime.UtcNow.ToShortDateString()),
                        Model = DeviceType.Kiosk.ToString(),
                        TimeRegistered = DateTime.UtcNow,
                        CurrentStatus = HealthStatus.Green,
                        Tags = tagsDevRegistration,
                        Inventory = new WindowsInventory { NetworkConfig = net, OSInfo = os, DeviceInfo = dev },
                    };
                session.Store(device);

                var ev = new DeviceAuthorizationEvent
                    {
                        DeviceId = itemRegistrationResult.ItemId,
                        InitialTags = tagsDevRegistration,
                        IpAddress =
                            HttpContext.Current == null ? string.Empty : HttpContext.Current.Request.UserHostAddress,
                        PasscodeUsed = itemRegistrationResult.PassCodeName,
                        TimeRegistered = DateTime.UtcNow
                    };
                session.Store(ev);

                session.SaveChanges();
            }
            return device;
        }

        #endregion
    }
}