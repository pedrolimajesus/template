namespace Shrike.Data.Reports.Alerts
{
    using System.Collections.Generic;

    using Lok.Unik.ModelCommon.Events;
    using Lok.Unik.ModelCommon.Interfaces;

    public interface IAlertEvaluator
    {
        HealthStatus EvaluateDevice(IDevice d, IEnumerable<object> data);
        HealthStatus ProcessAlerts(IDevice device, IEnumerable<Alert> alerts);
    }
}
