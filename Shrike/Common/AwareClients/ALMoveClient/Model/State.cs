using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lok.AwareLive.Clients.Move.Model
{
    public class StateRec
    {
        public State DataService { get; set; }
        public State Engine { get; set; }
    }

    public enum State { stopped, starting, running, stopping, unknown}
}
