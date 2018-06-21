using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.ManagedAppServices
{
    public enum AnnouncementRoute
    {
        Email,
        Alert
    }

    public class Announcement
    {
        public Announcement()
        {
            Recipients = new List<string>();

        }

        public AnnouncementRoute Route { get; set; }
        public IList<string> Recipients { get; set; }
        public string Content { get; set; }
    }
}
