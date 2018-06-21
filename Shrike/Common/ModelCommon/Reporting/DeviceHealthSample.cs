using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Client;

namespace Lok.Unik.ModelCommon.Reporting
{
    public class DeviceHealthSample : ITaggeable
    {
        public DateTime SampleTime { get; set; }
        public int Green{ get; set; }
        public int Yellow { get; set; }
        public int Red { get; set; }

        public Tag ForTag { get; set; }

        public string ReportTagValue { get; set; }
        
    }
}
