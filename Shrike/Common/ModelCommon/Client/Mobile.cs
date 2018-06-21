using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Client
{
    using Lok.Unik.ModelCommon.Interfaces;

    public class Mobile : Device
    {
        public override sealed DeviceType Type { get; set; }

        public Mobile()
        {
            Type = DeviceType.Mobile;
        }
    }
}
