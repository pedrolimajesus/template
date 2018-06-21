using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shrike.DAL.Manager;
using Shrike.UserManagement.BusinessLogic.Models;

namespace Shrike.UserManagement.BusinessLogic.Business
{
    using AppComponents;

    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class DeviceBusinessLogic
    {
        private const int ZERO = 0;
        private readonly DeviceManager _deviceManager = new DeviceManager();

        public IEnumerable<Device> GetAllDevices(string criteria = null)
        {
            var list = new DeviceManager().GetAll();
            var deviceList = list.ToArray().Select(device => new Device
            {
                Id = device.Id,
                Name = device.Name,
                Description = device.Description,
                State = device.CurrentState.ToString(),
                TimeRegistered = device.TimeRegistered,
                Model = device.Model,
                Tags = Tags.UI.TagUILogic.ToModelTags(device.Tags).ToList(),

                Status = device.CurrentStatus.ToString()
            }).ToList();

            if (!string.IsNullOrEmpty(criteria))
            {
                var criteriaDeviceList = new List<Device>();
                foreach (var device in deviceList)
                {
                    criteriaDeviceList
                        .AddRange(from tag in device.Tags
                                  where tag.Id.ToString().Equals(criteria)
                                  select device);
                }
                deviceList = criteriaDeviceList;
            }

            return deviceList;
        }

        public ICollection<Device> GetAllDevices(string criteria, string time)
        {
            var devices = GetAllDevices(criteria);
            ICollection<Device> deviceTime = new List<Device>();
            switch (time)
            {
                case "Today":
                    foreach (var device in devices)
                    {
                        DateTime timeRegistered = device.TimeRegistered;
                        if (DateTime.Today.ToString("MM/dd/yyyy").Equals(timeRegistered.ToString("MM/dd/yyyy")))
                        {
                            deviceTime.Add(device);
                        }
                    }
                    break;

                case "Yesterday":
                    foreach (var device in devices)
                    {
                        DateTime timeRegistered = device.TimeRegistered;
                        if (DateTime.Today.AddDays(-1).ToString("MM/dd/yyyy").Equals(timeRegistered.ToString("MM/dd/yyyy")))
                        {
                            deviceTime.Add(device);
                        }
                    }
                    break;

                case "Last Week":
                    foreach (var device in devices)
                    {
                        DateTime timeRegistered = device.TimeRegistered;
                        if (DateTime.Compare(timeRegistered, DateTime.Today.AddDays(-7)) > 0 && DateTime.Compare(timeRegistered, DateTime.Today.AddDays(-1)) < 0)
                        {
                            deviceTime.Add(device);
                        }
                    }
                    break;

                case "Last Month":
                    foreach (var device in devices)
                    {
                        DateTime timeRegistered = device.TimeRegistered;
                        if (timeRegistered.Month == DateTime.Today.AddMonths(-1).Month)
                        {
                            deviceTime.Add(device);
                        }
                    }
                    break;

                case "Old":
                    foreach (var device in devices)
                    {
                        DateTime timeRegistered = device.TimeRegistered;
                        if (DateTime.Compare(timeRegistered, DateTime.Today.AddMonths(-1)) < 0)
                        {
                            deviceTime.Add(device);
                        }
                    }
                    break;
            }

            return deviceTime;
        }
    }
}
