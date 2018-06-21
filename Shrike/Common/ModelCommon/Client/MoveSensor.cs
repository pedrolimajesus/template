using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Interfaces;

namespace Lok.Unik.ModelCommon.Client
{
    public sealed class MoveSensor: Device
    {
        public MoveSensor()
        {
            this.Type = DeviceType.MoveSensor;
        }

        public IEnumerable<string> Borders { get; set; }
        public IEnumerable<string> Hotspots { get; set; }

        public DateTime LastContactTimeUTC { get; set; }
    }
}
