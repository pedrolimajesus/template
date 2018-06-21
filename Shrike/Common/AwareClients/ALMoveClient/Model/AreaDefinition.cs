using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Lok.AwareLive.Clients.Move.Model
{
    public class AreaDefinitionList
    {
        public AreaDefinitionList()
        {
            AreaDefinitions = new List<AreaDefinition>();
        }

        public List<AreaDefinition> AreaDefinitions { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{{ AreaDefinitions [count={0}] : [\n", AreaDefinitions.Count);
            foreach (var item in AreaDefinitions)
            {
                sb.Append(item);
                sb.Append(",\n");
            }
            sb.Append("}\n");

            return sb.ToString();
        }       
    }

    public class AreaDefinition
    {
        public bool Active { get; set; }
        public int Id { get; set; }
        public string Color { get; set; }
        public string Name { get; set; }
        public bool Used { get; set; }

        //public override int GetHashCode()
        //{
        //    return Hash.GetCombinedHashCodeForHashes(Active.GetHashCode(), 
        //                                             Id, 
        //                                             (null == Color) ? 1: Color.GetHashCode(), 
        //                                             (null == Name) ? 1: Name.GetHashCode(),
        //                                             Used.GetHashCode());
        //}


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("  {{\n");
            sb.AppendFormat("     Name      : {0}\n", Name);
            sb.AppendFormat("     Id        : {0}\n", Id);
            sb.AppendFormat("     Active    : {0}\n", Active);
            sb.AppendFormat("     Color     : {0}\n", Color);
            sb.AppendFormat("  }}\n");
            return sb.ToString();
        }
    }
}
