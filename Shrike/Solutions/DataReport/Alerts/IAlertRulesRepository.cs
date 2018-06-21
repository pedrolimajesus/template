using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataReport.Alerts
{
    using Shrike.Data.Reports.Alerts;

    public interface IAlertRulesRepository
    {
        IEnumerable<IAlertRuleSpecification> LoadRulesForType(Type type, string tenancy);
    }

}
