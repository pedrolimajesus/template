using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Interfaces;

namespace Lok.Unik.ModelCommon.Client
{
    public sealed class LookSensor: Device
    {

        public LookSensor()
        {
            this.Type = DeviceType.LookSensor;
            
        }

      

        public DateTime LastContactTimeUTC { get; set; }


    }
}
