using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using Lok.Unik.ModelCommon.Client;
using Lok.Unik.ModelCommon.Command;
using Lok.Unik.ModelCommon.Reporting;

namespace Lok.Unik.ModelCommon.Events
{
    public class DeviceInteractionEvent : ITaggeableEvent
    {
        [DocumentIdentifier]
        public int EventId { get; set; }
       

        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; }
        public DateTime InteractionTime { get; set; }
        
        public IList<Tag> DeviceTags { get; set; }
        public CommandTypes CommandType { get; set; }
        public string CommandDescr { get; set; }
        public string CommandMessage { get; set; }
        public Guid InitiatingUserId { get; set; }
        public string InitiatingUserName { get; set; }
        public string Comment { get; set; }
        public bool Success { get; set; }

        public string ReportTagValue { get; set; }

     
    }
}
