using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lok.AwareLive.Clients.Move.Model
{
    public class AreasSubscriptionList
    {
        public AreasSubscriptionList()
        {
            Subscriptions = new List<AreasSubscription>();
        }

        public List<AreasSubscription> Subscriptions { get; set; }

        public string ToJson(bool raw = true)
        {
            bool firstIteration = true;
            string eol = (raw) ? "" : "\n";
            var sb = new StringBuilder();
            sb.AppendFormat("[{0}", eol);

            foreach (var subscription in Subscriptions)
            {
                if (firstIteration)
                {
                    firstIteration = false;
                }
                else
                {
                    sb.Append(" ,");
                }
                sb.Append(subscription.ToJsonRecord(raw));
            }

            sb.Append(eol);
            sb.Append("]");

            return sb.ToString();
        }
    }

    
    public class AreasSubscription
    {
        public AreasSubscription()
        {
            urlTemplate = "";
            method = "GET";
            areaIds = new List<int>();
        }
        public string urlTemplate { get; set; }
        public string method { get; set; }
        public List<int> areaIds { get; set; }

        public void LoadIdsFromAreaDefinitions(AreaDefinitionList areas)
        {
            foreach (var area in areas.AreaDefinitions)
            {
                if (area.Active)
                {
                    areaIds.Add(area.Id);
                }
            }
        }

        public string ToJsonAsList(bool raw = true)
        {
            bool firstIteration = true;
            string eol = (raw) ? "" : "\n";
            var sb = new StringBuilder();
            sb.AppendFormat("[{0}", eol);
            sb.Append("{");
            sb.Append(eol);

            sb.AppendFormat("    \"url_template\" : \"{0}\",{1}", urlTemplate, eol);
            sb.AppendFormat("    \"method\" : \"{0}\",{1}", method, eol);

            sb.AppendFormat("    \"hotspots\" : [ ", urlTemplate);
            foreach (var id in areaIds)
            {
                if (firstIteration)
                {
                    firstIteration = false;
                }
                else
                {
                    sb.Append(" ,");
                }
                sb.Append(id);
            }
            sb.AppendFormat("]{0}", eol);
            sb.Append("}");
            sb.Append(eol);
            sb.Append("]");

            return sb.ToString();
        }

        public string ToJsonAsItem(bool raw = true)
        {
            bool firstIteration = true;
            string eol = (raw) ? "" : "\n";
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append(eol);

            sb.AppendFormat("    \"url_template\" : \"{0}\",{1}", urlTemplate, eol);
            sb.AppendFormat("    \"method\" : \"{0}\",{1}", method, eol);

            sb.AppendFormat("    \"hotspots\" : [ ", urlTemplate);
            foreach (var id in areaIds)
            {
                if (firstIteration)
                {
                    firstIteration = false;
                }
                else
                {
                    sb.Append(" ,");
                }
                sb.Append(id);
            }
            sb.AppendFormat("]{0}", eol);
            sb.Append("}");
            sb.Append(eol);

            return sb.ToString();
        }

        public string ToJsonRecord(bool raw = true)
        {
            bool firstIteration = true;
            string eol = (raw) ? "" : "\n";
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append(eol);

            sb.AppendFormat("    \"url_template\" : \"{0}\",{1}", urlTemplate, eol);
            sb.AppendFormat("    \"method\" : \"{0}\",{1}", method, eol);

            sb.AppendFormat("    \"hotspots\" : [ ", urlTemplate);
            foreach (var id in areaIds)
            {
                if (firstIteration)
                {
                    firstIteration = false;
                }
                else
                {
                    sb.Append(" ,");
                }
                sb.Append(id);
            }
            sb.AppendFormat("]{0}", eol);
            sb.Append("}");
            sb.Append(eol);

            return sb.ToString();
        }

        /*
[
    {
        "url_template": "http://10.1.90.108:3000/Srv/ExtNotifications/MOVE1/HotspotEvent?hotspot=%hotspot&type=%type&object=%object",
        "method": "POST",
        "hotspots": [ 3,5]
    },
    {
        "url_template": "http://10.2.20.109:8181/statemachine/event.php?userId=%object&event=%type&hotspotId=%hotspot",
        "method": "GET",
        "hotspots": [ 3,5 ]
    }
]
         */
    }
}
