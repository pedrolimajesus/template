using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace Lok.AwareLive.Clients.Move.Model
{
    public class LineDefinitionList
    {
        public LineDefinitionList()
        {
            LineDefinitions = new List<LineDefinition>();
        }

        public List<LineDefinition>  LineDefinitions { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{{ LineDefinitions [count={0}] : [\n", LineDefinitions.Count);
            foreach (var item in LineDefinitions)
            {
                sb.Append(item);
                sb.Append(",\n");
            }
            sb.Append("}\n");

            return sb.ToString();
        }
    }

    public class LineDefinition
    {
        public LineDefinition()
        {
            Initial = new CordinateType();
            Terminal = new CordinateType();
        }

        public bool Active { get; set; }
        public int Id { get; set; }
        public CordinateType Initial { get; set; }
        public string LeftName { get; set; }
        public string Name { get; set; }
        public string RightName { get; set; }
        public CordinateType Terminal { get; set; }
        public string Color { get; set; }
        public bool Used { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("  {{\n");
            sb.AppendFormat("     Name      : {0}\n", Name);
            sb.AppendFormat("     Id        : {0}\n", Id);
            sb.AppendFormat("     Active    : {0}\n", Active);
            sb.AppendFormat("     LeftName  : {0}\n", LeftName);
            sb.AppendFormat("     RightName : {0}\n", RightName);
            sb.AppendFormat("     Initial   : {0}\n", Initial);
            sb.AppendFormat("     Terminal  : {0}\n", Terminal);
            sb.AppendFormat("  }}\n");
            return sb.ToString();
        }
    }
}


/*
    {
        "active": "true",
        "id": 2,
        "initial": {
            "x": 700,
            "y": 700
        },
        "left_name": "in",
        "name": "DoorLobby",
        "right_name": "out",
        "terminal": {
            "x": 950,
            "y": 950
        }
    }
*/