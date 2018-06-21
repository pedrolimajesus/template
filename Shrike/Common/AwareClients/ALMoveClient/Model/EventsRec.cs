using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Lok.AwareLive.Clients.Move.Model
{
    public class EventsRec
    {
        public EventsRec()
        {
            HotspotEvents = new List<HotspotEvent>();
            BorderEvents = new List<BorderEvent>();
        }

        public List<HotspotEvent> HotspotEvents { get; set; }
        public List<BorderEvent>  BorderEvents { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{{ EventsRec [count={0}] : [\n", BorderEvents.Count);
            foreach (var item in BorderEvents)
            {
                sb.Append(item);
                sb.Append(",\n");
            }
            sb.Append("}}\n");

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
}
