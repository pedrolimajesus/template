using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lok.AwareLive.Clients.Move.Model
{
    public class HotspotDefinitionList
    {
        public HotspotDefinitionList()
        {
            HotspotDefinitions = new List<HotspotDefinition>();
        }

        public List<HotspotDefinition> HotspotDefinitions { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{{ HotspotDefinitions [count={0}] : [\n", HotspotDefinitions.Count);
            foreach (var item in HotspotDefinitions)
            {
                sb.Append(item);
                sb.Append(",\n");
            }
            sb.Append("}\n");

            return sb.ToString();
        }       
    }

    public class HotspotDefinition
    {
        public bool Active { get; set; }
        public int Id { get; set; }
        public MaskColor MaskColor { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("  {{\n");
            sb.AppendFormat("     Name      : {0}\n", Name);
            sb.AppendFormat("     Id        : {0}\n", Id);
            sb.AppendFormat("     Active    : {0}\n", Active);
            sb.AppendFormat("     MaskColor  : {0}\n", MaskColor);
            sb.AppendFormat("  }}\n");
            return sb.ToString();
        }
    }

    public class MaskColor
    {
        public int Blue { get; set; }
        public int Green { get; set; }
        public int Red { get; set; }

        public override string ToString()
        {
            return string.Format("{{ B:{0}, G:{1}, R:{2} }}", Blue, Green, Red);
        }
    }

    /*
    {
        "active": "true",
        "id": 0,
        "mask_color": {
            "B": 255,
            "G": 0,
            "R": 140
        },
        "name": "Express Kiosk"
    },
     * */
}
