using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lok.AwareLive.Clients.Move.Model
{
    public class LinesSubscription
    {
        public LinesSubscription()
        {
            urlTemplate = "";
            method = "GET";
            borderIds = new List<int>();
        }
        public string urlTemplate { get; set; }
        public string method { get; set; }
        public List<int> borderIds { get; set; }

        public void LoadIdsFromLineDefinitions(LineDefinitionList lines)
        {
            foreach (var line in lines.LineDefinitions)
            {
                if (line.Active)
                {
                    borderIds.Add(line.Id);
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

            sb.AppendFormat("    \"borders\" : [ ", urlTemplate);
            foreach (var id in borderIds)
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

            sb.AppendFormat("    \"borders\" : [ ", urlTemplate);
            foreach (var id in borderIds)
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

            return sb.ToString();
        }
    }
}
