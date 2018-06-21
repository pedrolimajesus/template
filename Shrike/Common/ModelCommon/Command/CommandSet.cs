using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Command
{
    using System;

    public class CommandSet
    {
        int ChangeRequestId { get; set; }
        DateTime RequestTime { get; set; }
        IList<ICommand> Commands { get; set; }
    }
}
