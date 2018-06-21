using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.AwareLive.Clients.Look.Model
{
    public class StateRec
    {
        public State DataService { get; set; }
        public State Engine { get; set; }
    }

    public enum State { stopped, starting, running, stopping, unknown}

}