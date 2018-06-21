using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Client;

namespace Lok.Unik.ModelCommon.Events
{
    public interface ITaggeableEvent
    {
        IList<Tag> DeviceTags { get; set; }
        string ReportTagValue { get; set; }
    }
}
