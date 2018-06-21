using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Client;
using Lok.Unik.ModelCommon.Interfaces;

namespace Lok.Unik.ModelCommon
{
    public class ControlProxyAgent: Device, ITimeFilter
    {
        public ControlProxyAgent()
        {
            Type = DeviceType.ControlProxyAgent;
        }

        public DateTime TimeUpdated { get; set; }
    }
}
