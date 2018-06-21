using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Control.Common.ProxyCommon.Interfaces
{
    // This class is used to make device registration requests from the Proxy to the Server
    public class DeviceRegistrationRequest
    {
        public DeviceRegistrationRequest()
        {
            Name = "";
            Description = "";
            IpAddress = "";
            Username = "";
            Password = "";
            DeviceType = DeviceType.Unknown_Device;
            AutoRegisterToProxy = false;
        }

        public string Name { get; set;  }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DeviceType DeviceType { get; set; }
        public bool AutoRegisterToProxy { get; set; }
    }
}
