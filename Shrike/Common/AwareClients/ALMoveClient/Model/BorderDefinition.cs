using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;


namespace Lok.AwareLive.Clients.Move.Model
{
    public class BorderDefinitionList
    {
        public BorderDefinitionList()
        {
            BorderDefinitions = new List<BorderDefinition>();
        }

        public List<BorderDefinition>  BorderDefinitions { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{{ BorderDefinitions [count={0}] : [\n", BorderDefinitions.Count);
            foreach (var item in BorderDefinitions)
            {
                sb.Append(item);
                sb.Append(",\n");
            }
            sb.Append("}\n");

            return sb.ToString();
        }
    }


    public class BorderDefinition
    {
        public BorderDefinition()
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

        public const string SideNameIn = "in";
        public const string SideNameOut = "out";

        //public override int GetHashCode()
        //{
        //    return Hash.GetCombinedHashCodeForHashes(Active.GetHashCode(), 
        //                                             Id, 
        //                                             (null == Initial) ? 1 : Initial.GetHashCode(),
        //                                             (null == LeftName) ? 1 : LeftName.GetHashCode(), 
        //                                             (null == Name) ? 1: Name.GetHashCode(), 
        //                                             (null == RightName) ? 1 : RightName.GetHashCode(),
        //                                             (null == Terminal) ? 1 : Terminal.GetHashCode());
        //}

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

    public class CordinateType
    {
        public int X { get; set; }
        public int Y { get; set; }

        public override string ToString()
        {
            return string.Format("{{ (X:{0}), (Y:{1}) }}", X, Y);
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