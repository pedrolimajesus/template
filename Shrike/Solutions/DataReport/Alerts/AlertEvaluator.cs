namespace Shrike.Data.Reports.Alerts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AppComponents;
    using AppComponents.ControlFlow;
    using AppComponents.Extensions.EnumEx;
    using AppComponents.Extensions.EnumerableEx;
    using AppComponents.Extensions.StringEx;

    using DataReport.Alerts;

    using Lok.Unik.ModelCommon.Events;
    using Lok.Unik.ModelCommon.Interfaces;

    using ModelCommon.RavenDB;

    using AppComponents.Data;

    using Shrike.Data.Reports.Repository;

    public class AlertEvaluator : IAlertEvaluator
    {
        private IAlertRulesRepository _rulesRepository;

        public AlertEvaluator()
        {
            this._rulesRepository = (Catalog.Factory.CanResolve<IAlertRulesRepository>()) ? Catalog.Factory.Resolve<IAlertRulesRepository>() : new HardCodedAlertRulesRepository();

        }


        public HealthStatus EvaluateDevice(IDevice device, IEnumerable<object> data)
        {


            var tenancyUris = ContextRegistry.ContextsOf("Tenancy");
            if (!tenancyUris.Any())
            {
                return HealthStatus.Unknown;
            }

            var tenancyUri = tenancyUris.First();
            var tenancy = tenancyUri.Segments.Count() > 1 ? tenancyUri.Segments[1] : string.Empty;



            var typeGrouped = from d in data
                              group d by d.GetType()
                                  into items
                                  select new { Key = items.Key, Items = items };

            List<Alert> alerts = new List<Alert>();

            foreach (var tg in typeGrouped)
            {
                var rules = this._rulesRepository.LoadRulesForType(tg.Key, tenancy);

                if (rules.Any())
                {
                    foreach (var r in rules)
                    {
                        var newAlerts = r.Apply(device, tg.Items);
                        if (newAlerts.Any())
                        {
                            alerts.AddRange(newAlerts);
                        }
                    }
                }

            }

            var retval = this.ProcessAlerts(device, alerts);

            return retval;

        }

        public HealthStatus ProcessAlerts(IDevice device, IEnumerable<Alert> alerts)
        {
            HealthStatus formerStatus = device.CurrentStatus;
            HealthStatus retval = device.CurrentStatus;

            var alertStatusEvents = new List<AlertStatusChangeEvent>();

            if (alerts.Any())
            {
                using ( var ctx = ContextRegistry.NamedContextsFor(
                        ContextRegistry.CreateNamed(ContextRegistry.Kind,
                        UnikContextTypes.UnikTenantContextResourceKind)))
                {
                    var batches = alerts.InBatchesOf(50);
                    foreach (var batch in batches)
                    {
                        //using (var dc = DocumentStoreLocator.ContextualResolve())
                        //{
                            foreach (var it in batch)
                            {
                                int matchHash = it.CreateMatchHash();
                                /* var matches = dc.Query<Alert>().Where(a => a.MatchHash == matchHash && a.Status != AlertStatus.Closed);
                                */

                                var querySpecification = new QuerySpecification()
                                {
                                    Where = new Filter
                                    {
                                        Rules = new Comparison[]     {
                                                    new Comparison { Data = matchHash,
                                                            Field = "MatchHash", Test = Test.Equal },
                                                     new Comparison { Data = AlertStatus.Closed,
                                                            Field = "Status", Test = Test.NotEqual }
                                                    }
                                    }
                                };

                                var matches = ReportDataProvider.GetFromRepository<Alert>(querySpecification);

                                bool isDuplicate = false;
                                foreach (var candidate in matches)
                                {
                                    if (candidate.RelatedDevice == it.RelatedDevice && it.Kind == candidate.Kind &&
                                        it.AlertHealthLevel == candidate.AlertHealthLevel)
                                    {
                                        isDuplicate = true;
                                        break;
                                    }
                                }

                                if (!isDuplicate)
                                {
                                    it.MatchHash = matchHash;
                                    if (it.AlertHealthLevel > retval)
                                    {

                                        retval = it.AlertHealthLevel;
                                        device.CurrentStatus = retval;

                                        alertStatusEvents.Add(new AlertStatusChangeEvent
                                        {
                                            Identifier = it.Identifier,
                                            AlertHealthLevel = it.AlertHealthLevel,
                                            AlertTitle = it.AlertTitle,
                                            Kind = it.Kind,
                                            KindDesc = it.Kind.EnumName().SpacePascalCase(),
                                            DeviceTags = it.Tags,
                                            Message = it.Message,
                                            RelatedDevice = it.RelatedDevice,
                                            RelatedDeviceName = it.RelatedDeviceName,
                                            Resolved = false,
                                            TimeGenerated = it.TimeGenerated,
                                            StaleAlertTime = (it.AlertHealthLevel == HealthStatus.Red) ? it.TimeGenerated + TimeSpan.FromHours(6.0) : it.TimeGenerated + TimeSpan.FromHours(24.0),
                                            LongestAssignmentTime = TimeSpan.FromSeconds(0.0)
                                        });
                                    }

                                    ReportDataProvider.StoreInRepository<Alert>(it);
                                    //dc.Store(it);
                                }
                            }

                            //dc.SaveChanges();
                        //}
                    }
                }
            }


            if (formerStatus != retval)
            {
                using (
                    var ctx =
                        ContextRegistry.NamedContextsFor(ContextRegistry.CreateNamed(ContextRegistry.Kind,
                                                                                     UnikContextTypes.
                                                                                         UnikWarehouseContextResourceKind)))
                {
                    //using (var dc = DocumentStoreLocator.ContextualResolve())
                    //{
                        var deviceHealthChangeEvt = new DeviceHealthStatusEvent
                        {
                            DeviceId = device.Id,
                            DeviceName = device.Name,
                            From = formerStatus,
                            To = retval,

                            DeviceTags = device.Tags,
                            DeviceTagHashes = DeviceHealthStatusEvent.ConvertTagsToHashs(device.Tags),
                            TimeChanged = DateTime.UtcNow
                        };

                        //dc.Store(deviceHealthChangeEvt);
                        ReportDataProvider.StoreInRepository<DeviceHealthStatusEvent>(deviceHealthChangeEvt);

                        var batches = alertStatusEvents.InBatchesOf(50);
                        foreach (var batch in batches)
                        {
                            foreach (var se in batch) 
                            {
                                //dc.Store(se);
                                ReportDataProvider.StoreInRepository<AlertStatusChangeEvent>(se);
                            }
                            //dc.SaveChanges();
                        }
                    //}
                }
            }
            return retval;
        }
    }



}
