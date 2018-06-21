namespace Shrike.Data.Reports.Alerts
{
    using System;
    using System.Collections.Generic;

    using Lok.Unik.ModelCommon.Events;
    using Lok.Unik.ModelCommon.Interfaces;

    public interface IAlertRuleSpecification
    {
        Type AppliesToType { get; set; }
        IEnumerable<Alert> Apply(IDevice d, IEnumerable<object> data);
    }
}
