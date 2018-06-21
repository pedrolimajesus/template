using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace Lok.AwareLive.Clients.Move.Model
{
    public class HotspotEventsRec
    {
        public HotspotEventsRec()
        {
            HotspotEvents = new List<HotspotEvent>();
        }

        public List<HotspotEvent> HotspotEvents { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{{ HotspotEventsRec [count={0}] : [\n", HotspotEvents.Count);
            foreach (var item in HotspotEvents)
            {
                sb.Append(item);
                sb.Append(",\n");
            }
            sb.Append("}}\n");

            return sb.ToString();
        }
    }

    public class HotspotEvent
    {
        public IdType Hotspot { get; set; }
        public IdType Object { get; set; }
        public DateTime Time { get; set; }
        public HotspotEventType Type { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("  {{\n");
            sb.AppendFormat("     Hotspot : {0}\n", Hotspot);
            sb.AppendFormat("     Object  : {0}\n", Object);
            sb.AppendFormat("     Time    : {0}\n", Time);
            sb.AppendFormat("     Type    : {0}\n", Image(Type));
            sb.AppendFormat("  }}\n");
            return sb.ToString();
        }

        private string Image(HotspotEventType hsType)
        {
            switch (hsType)
            {
                case (HotspotEventType.Enter) : return "Enter";
                case (HotspotEventType.Exit) : return "Exit";
                default:
                    return "Unknown";
            }
        }
    }

    public class IdType
    {
        public int Id { get; set; }
        public override string ToString()
        {
            return string.Format("ID={0}", Id);
        }
    }

    public enum HotspotEventType { Enter, Exit, Unknown }
}
