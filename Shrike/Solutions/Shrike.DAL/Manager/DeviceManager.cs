using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using AppComponents.ControlFlow;
using AppComponents.Raven;
using AppComponents.Web;
using Lok.Unik.ModelCommon;
using Lok.Unik.ModelCommon.Client;
using Lok.Unik.ModelCommon.Events;
using Lok.Unik.ModelCommon.Interfaces;
using ModelCommon.RavenDB;
using Raven.Client;

namespace Shrike.DAL.Manager
{
    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class DeviceManager
    {
        public static void CreateIndexes()
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(typeof(DeviceManager)))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var config = Catalog.Factory.Resolve<IConfig>();
                    var dbName = config[CommonConfiguration.DefaultDataDatabase];

                    var tenancyUris = ContextRegistry.ContextsOf("Tenancy");
                    if (tenancyUris.Any())
                    {
                        var tenancyUri = tenancyUris.First();
                        var tenancy = tenancyUri.Segments.Count() > 1 ? tenancyUri.Segments[1] : string.Empty;

                        if (!string.IsNullOrEmpty(tenancy)
                            &&
                            !tenancy.Equals(Tenants.SystemOwner, StringComparison.InvariantCultureIgnoreCase))
                        {
                            dbName = tenancy;
                        }
                    }

                    var store = session.Advanced.DocumentStore;
                    IndexesManager.CreateIndexes(store, dbName, typeof(AllDeviceIndex));
                }
            }
        }

        public void Touch(Guid deviceId)
        {

            try
            {
                Kiosk dev = null;
                bool initialHealthStatus = false;
                using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
                {
                    using (var session = DocumentStoreLocator.ContextualResolve())
                    {
                        dev = session.Load<Kiosk>(deviceId);
                        if (null != dev)
                        {
                            // successful contact
                            dev.LastContactTimeUTC = DateTime.UtcNow;

                            // initialize health status ok if nothing going on
                            if (dev.CurrentStatus == HealthStatus.Unknown)
                            {
                                dev.CurrentStatus = HealthStatus.Green;
                                initialHealthStatus = true;

                            }

                            session.SaveChanges();
                        }

                    }
                }

                if (null != dev && initialHealthStatus)
                    DeviceHealthStatusInitialize(dev);
            }
            catch // swallow exceptions in this case
            {


            }
        }

        private static void DeviceHealthStatusInitialize(Kiosk dev)
        {
            using (
                var ctx =
                    ContextRegistry.NamedContextsFor(ContextRegistry.CreateNamed(ContextRegistry.Kind,
                                                                                 UnikContextTypes.
                                                                                     UnikWarehouseContextResourceKind)))
            {
                using (var dc = DocumentStoreLocator.ContextualResolve())
                {
                    var deviceHealthChangeEvt = new DeviceHealthStatusEvent
                    {
                        DeviceId = dev.Id,
                        DeviceName = dev.Name,
                        From = HealthStatus.Unknown,
                        To = HealthStatus.Green,
                        DeviceTags = dev.Tags,
                        TimeChanged = DateTime.UtcNow
                    };

                    dc.Store(deviceHealthChangeEvt);
                    dc.SaveChanges();
                }
            }
        }

        public IDevice SaveNewDevice(IDevice deviceDb)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var objId = Guid.NewGuid();
                    deviceDb.Id = objId;

                    deviceDb.Tags.Add(new Tag
                    {
                        Id = Guid.NewGuid(),
                        Type = TagType.Device,
                        Attribute = deviceDb.Name,
                        Value = deviceDb.Name,
                        CreateDate = DateTime.UtcNow,
                        Category = new TagCategory
                        {
                            Name = objId.ToString(),
                            Color = CategoryColor.Default
                        }
                    });

                    session.Store(deviceDb);
                    session.SaveChanges();

                    return deviceDb;
                }
            }
        }

        public IEnumerable<Device> GetDevices()
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q = from entity in session.Query<Device, AllDeviceIndex>() select entity;
                    return q.ToArray();
                }
            }
        }

        public IEnumerable<T> GetDevicesByType<T>() where T : Device
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    return (from entity in session.Query<T>() select entity).ToArray();
                }
            }
        }

        public void Store<T>(T device) where T : Device
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    session.Store(device);
                    session.SaveChanges();
                }
            }
        }

        public IEnumerable<IDevice> GetAll()
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q2 = from entity in session.Query<Device, AllDeviceIndex>() select entity;

                    return q2.ToArray();
                }
            }
        }
        public IDocumentSession Sessiones()
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    return session;
                }
            }
        }

        public void UpdateDeviceById(Guid deviceId, string name, string description)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q2 = from entity in session.Query<Device, AllDeviceIndex>()
                             //         where entity.Id == deviceId
                             select entity;
                    var device = q2.ToArray().FirstOrDefault(a => a.Id == deviceId);
                    if (device != null)
                    {
                        device.Description = description;
                        device.Name = name;
                    }
                    session.SaveChanges();
                    //return q2.FirstOrDefault();
                }
            }
        }

        public Device GetDeviceById(Guid deviceId)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q1 = from entity in session.Query<Device, AllDeviceIndex>()
                             //where entity.IdString == deviceId.ToString()
                             select entity;
                    var device = q1.ToArray().FirstOrDefault(a => a.Id == deviceId);
                    return device;
                }
            }
        }

        public void EnableDisableDevice(Guid deviceId, DeviceState status)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q2 = from entity in session.Query<Device, AllDeviceIndex>()
                             //         where entity.Id == deviceId
                             select entity;
                    var device = q2.ToArray().FirstOrDefault(a => a.Id == deviceId);
                    if (device != null)
                    {
                        device.CurrentState = status;
                    }
                    session.SaveChanges();
                }
            }
        }

        public void DeleteById(Guid deviceId)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q2 = from entity in session.Query<Device, AllDeviceIndex>() select entity;

                    var toDelete = q2.ToArray().FirstOrDefault(a => a.Id == deviceId);
                    session.Delete(toDelete);
                    session.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Gets the OS Information From Device.
        /// </summary>
        /// <returns></returns>
        public OSInfo GetOsInfo()
        {
            var os = new OSInfo { OsName = "Windows", OsType = "Server", OsVersion = "8.0.1.2353" };
            return os;
        }

        /// <summary>
        /// Gets Hardware Info From Device.
        /// </summary>
        /// <returns></returns>
        public DeviceInfo GetDeviceInfo()
        {
            var device = new DeviceInfo
            {
                Processor = "Intel Core i7 2.3 Ghz.",
                MemoryInBytes = "8Gb.",
                PrinterName = "Cannon Pixma",
                NumberOfMonitors = 2,
                PrimaryMonitorName = "LG Flatom"
            };
            return device;
        }

        /// <summary>
        /// Gets Network Configuration From Device.
        /// </summary>
        /// <param name="device">Device name</param>
        /// <returns></returns>
        public NetworkConfig GetNetworkConfig(string device)
        {
            var network = new NetworkConfig
            {
                IpAddress = "192.168.1.20",
                SubnetMask = "255.255.255.0",
                DefaultGateway = "192.168.1.1",
                MacAddress = "05-ED-56-3A-F1-64",
                MachineName = device
            };
            return network;
        }

        /// <summary>
        /// When a Device was created update the Inventory DeviceId
        /// </summary>
        /// <param name="device">IDevice Entity</param>
        /// <returns>GUID Id</returns>
        private static Guid UpdateDeviceInventoryId(IDevice device)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(typeof(DeviceManager)))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var id = device.Type.ToString() + 's' + '/' + device.Id;
                    var update = session.Load<dynamic>(id) as IDevice;
                    update.Inventory.DeviceId = device.Id;
                    session.SaveChanges();
                    return update.Inventory.DeviceId;
                }
            }
        }

        public void UpdateTagForDevice(Guid id, List<Tag> tags)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(typeof(DeviceManager)))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q2 = from entity in session.Query<Device, AllDeviceIndex>()
                             select entity;
                    var device = q2.ToArray().FirstOrDefault(a => a.Id == id);
                    if (device != null)
                    {
                        device.Tags = tags;
                    }

                    session.SaveChanges();
                }
            }
        }
        public void RemoveTag(Guid id)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(typeof(DeviceManager)))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q2 = from entity in session.Query<Device, AllDeviceIndex>() select entity;
                    var device = q2.ToArray().FirstOrDefault(a => a.Id == id);
                    if (device != null)
                    {
                        device.Tags.Clear();
                    }
                    session.SaveChanges();
                }
            }
        }
    }
}
