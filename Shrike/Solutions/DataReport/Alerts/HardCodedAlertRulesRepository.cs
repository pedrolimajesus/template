using System;
using System.Collections.Generic;
using System.Linq;
using AppComponents.Extensions.EnumerableEx;
using Lok.Unik.ModelCommon.Events;
using Lok.Unik.ModelCommon.Inventory;

namespace DataReport.Alerts
{
    using Shrike.Data.Reports.Alerts;

    public class HardCodedAlertRulesRepository : IAlertRulesRepository
    {
        private FuncAlertRuleSpecification<EventData> _eventDataConversion;
        private FuncAlertRuleSpecification<ManagedAppProblemEvent> _maProblemDataConversion;

        public HardCodedAlertRulesRepository()
        {
            _eventDataConversion = new FuncAlertRuleSpecification<EventData>();
            _eventDataConversion.AppliesToType = typeof(EventData);
            _eventDataConversion.Rule = (data, dev) =>
            {
                Alert retval = null;
                if (data.StatusLevel == HealthStatus.Yellow || data.StatusLevel == HealthStatus.Red)
                {

                    retval = new Alert
                    {
                        Identifier = data.EventId,
                        AlertHealthLevel = data.StatusLevel,
                        AlertTitle = data.Status,
                        Message = data.Message,
                        TimeGenerated = DateTime.UtcNow,
                        Kind = (AlertKinds)data.Stamp,
                        Status = AlertStatus.Unassigned,
                        TimeStatusChanged = DateTime.UtcNow,
                        RelatedDevice = new Guid(data.DeviceId),
                        RelatedDeviceName = dev.Name,
                        RelatedDeviceModel = dev.Model,
                        Tags = dev.Tags
                    };

                    retval.MatchHash = retval.CreateMatchHash();
                }

                return retval;
            };


            _maProblemDataConversion = new FuncAlertRuleSpecification<ManagedAppProblemEvent>();
            _maProblemDataConversion.AppliesToType = typeof(ManagedAppProblemEvent);
            _maProblemDataConversion.Rule = (data, dev) =>
            {
                Alert retval = null;

                if (data.ProblemInfo.Context == ManagedAppProblemContexts.LoadMedia)
                {

                    new Alert
                    {
                        Identifier = data.Identifier,
                        AlertHealthLevel = HealthStatus.Red,
                        AlertTitle = data.ProblemInfo.Problem,
                        Kind = AlertKinds.ContentLoadProblem,
                        Status = AlertStatus.Unassigned,
                        TimeStatusChanged = DateTime.UtcNow,
                        RelatedDevice = dev.Id,
                        RelatedDeviceModel = dev.Model,
                        Tags = dev.Tags

                    };
                }
                return retval;
            };

        }

        public IEnumerable<IAlertRuleSpecification> LoadRulesForType(Type type, string tenancy)
        {
            if (type == typeof(EventData))
            {
                return EnumerableEx.OfOne((IAlertRuleSpecification)_eventDataConversion);
            }

            if (type == typeof(ManagedAppProblemEvent))
                return EnumerableEx.OfOne((IAlertRuleSpecification)_maProblemDataConversion);

            return Enumerable.Empty<IAlertRuleSpecification>();
        }
    }
}
