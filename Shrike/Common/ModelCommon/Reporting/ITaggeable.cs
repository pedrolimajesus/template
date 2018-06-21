using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Client;

namespace Lok.Unik.ModelCommon.Reporting
{
    public interface ITaggeable
    {
        Tag ForTag { get; set; }
        string ReportTagValue { get; set; }
    }


}
