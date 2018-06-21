using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AppComponents.ControlFlow
{
    public class AppEventLogApplicationAlert : IApplicationAlert
    {
        public void RaiseAlert(ApplicationAlertKind kind, params object[] details)
        {
            var appLog = new System.Diagnostics.EventLog { Source = Process.GetCurrentProcess().ProcessName };
            var jsonDetail = JsonConvert.SerializeObject(details);

            var strEv = string.Format("Operational Event {0}:\n{1}", kind, jsonDetail);
            appLog.WriteEntry(strEv);
        }
    }
}
