namespace Shrike.Data.Reports.Alerts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DataReport.Alerts;

    using Lok.Unik.ModelCommon.Events;
    using Lok.Unik.ModelCommon.Interfaces;

    public class FuncAlertRuleSpecification<T> : IAlertRuleSpecification
    {
        public Func<T, IDevice, Alert> Rule { get; set; }

        public Type AppliesToType
        {
            get { return typeof(T); }
            set { }
        }

        public IEnumerable<Alert> Apply(IDevice dev, IEnumerable<object> data)
        {
            if (null == this.Rule)
                return Enumerable.Empty<Alert>();

            return data.Where(it => it.GetType() == typeof(T))
                .Select(it => (T)it)
                .Select(datum => this.Rule(datum, dev))
                .Where(alert => null != alert)
                .ToList<Alert>();
        }
    }

}
