using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using Lok.Unik.ModelCommon.Client;

namespace Lok.Unik.ModelCommon.Events
{
    public class InfluenceData
    {
        public string Provider { get; set; }
        public string Value { get; set; }
    }
 
    public class InteractionElement
    {
        public string Type { get; set; }
        public bool Touched { get; set; }
        public string Identifier { get; set; }
        public string Title { get; set; }

        public string Group { get; set; }
        public string PartOfTitle { get; set; }
        public string PartOfId { get; set; }

        public string Target { get; set; }
        public string Action { get; set; }
        public string Data { get; set; }
    }

    

    public class InteractionEvent
    {
        public InteractionEvent()
        {
            InteractionTime = DateTime.UtcNow;
            InteractionElements = new List<InteractionElement>();
            Influences = new List<InfluenceData>();
        }

        public DateTime InteractionTime { get; set; }
        public int SecondsExposed { get; set; }
        public bool Active { get; set; }
        public List<InfluenceData> Influences { get; set; }
        public List<InteractionElement> InteractionElements { get; set; }

        public string PlacementSummary { get; set; }

        public string Application { get; set; }
        public string ApplicationId { get; set; }
        public string PlacementId { get; set; }
    }

    public class InteractionMessage
    {
        public InteractionMessage()
        {
            InteractionEvents = new List<InteractionEvent>();
        }

        public List<InteractionEvent> InteractionEvents { get; set; } 
    }

    public class ManagedAppInteractionEvent : ITaggeableEvent
    {
        public ManagedAppInteractionEvent()
        {
            DeviceTags = new List<Tag>();
            Identifier = Guid.NewGuid();
            InteractionInfo = new InteractionMessage();

        }

        [DocumentIdentifier]
        public Guid Identifier { get; set; }

        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public IList<Tag> DeviceTags { get; set; }

        public DateTime SampleTime { get; set; }

        public InteractionMessage InteractionInfo { get; set; }

        public string ReportTagValue { get; set; }
    }

    


}
