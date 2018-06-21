using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lok.AwareLive.Clients.Move.Model
{
    public class BorderEventsRec
    {
        public BorderEventsRec()
        {
            BorderEvents = new List<BorderEvent>();
        }

        public List<BorderEvent> BorderEvents { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{{ BorderEventsRec [count={0}] : [\n", BorderEvents.Count);
            foreach (var item in BorderEvents)
            {
                sb.Append(item);
                sb.Append(",\n");
            }
            sb.Append("}}\n");

            return sb.ToString();
        }
    }

    public class BorderEvent
    {
        public IdType Border { get; set; }
        public DirectionType Direction { get; set; }
        public IdType Object { get; set; }
        public DateTime Time { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("  {{\n");
            sb.AppendFormat("     Border    : {0}\n", Border);
            sb.AppendFormat("     Direction : {0}\n", Image(Direction));
            sb.AppendFormat("     Object    : {0}\n", Object);
            sb.AppendFormat("     Time      : {0}\n", Time);
            sb.AppendFormat("  }}\n");
            return sb.ToString();
        }

        private string Image(DirectionType direction)
        {
            switch (direction)
            {
                case (DirectionType.left_to_right) : return "left_to_right";
                case (DirectionType.right_to_left): return "right_to_left";
                default :
                    return "Unknown";
            }
        }
    }

    public enum DirectionType { right_to_left, left_to_right, unknown_direction}
}
