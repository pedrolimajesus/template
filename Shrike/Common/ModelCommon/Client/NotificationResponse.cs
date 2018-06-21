using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Client
{
    public enum NotifyType { No_Request, Sync_Request }
    public class NotificationResponse
    {
        public NotifyType Request { get; set; }
    }
}
