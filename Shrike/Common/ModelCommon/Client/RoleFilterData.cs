using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Client
{
    public class RoleFilterData
    {
        public RoleFilter RoleFilter { get; set; } //default page
        public IEnumerable<DataSettingUI> FooterData { get; set; }
    }

    public class DataSettingUI
    {
        public RoleFilter RoleFilter { get; set; }
        public string ImageSrc { get; set; }
        public string Title { get; set; }
        //
        public IEnumerable<CommandData> CommandData { get; set; }
        public IEnumerable<FilterData> FilterData { get; set; }
    }

    public class CommandData
    {
        public IEnumerable<string> RoleEnable { get; set; }
        public string EventOnclick { get; set; }
        public string AltImage { get; set; }
        public string SrcImage { get; set; }
        public string Title { get; set; }
    }

    public class FilterData
    {

    }
}
