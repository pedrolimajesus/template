using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using AppComponents.Extensions.EnumEx;
using AppComponents.Extensions.ExceptionEx;
using AppComponents.Raven;
using Raven.Abstractions.Exceptions;
using Raven.Imports.Newtonsoft.Json;
using log4net;

namespace AppComponents.Topology
{

    public class RavenApplicationNodeRunner: IApplicationNodeRunner
    {
        private ApplicationNodeRegistry _reg;
        private IRecurrence<object> _recurrence;
        private LogConfigConsumer _logConfig;
        private IApplicationNodeGatherer _gatherer;
        private TimeSpan _updateCycle;
        private ApplicationNodeStates _state;
        private ILog _log;
        private DebugOnlyLogger _dbLog;
        private IApplicationAlert _alert;
        private bool _running = false;
        private string _logFilePath;
        
        public RavenApplicationNodeRunner()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var companyKey = cf[ApplicationTopologyLocalConfig.CompanyKey];
            var appKey = cf[ApplicationTopologyLocalConfig.ApplicationKey];
            _reg = new ApplicationNodeRegistry(companyKey, appKey);

            var logPath = cf[ApplicationTopologyLocalConfig.LogFilePath];
            var compType = cf[ApplicationTopologyLocalConfig.ComponentType];
            var logName = compType + ".log";
            
            _recurrence = Catalog.Factory.Resolve<IRecurrence<object>>();
            _logFilePath = Path.Combine(logPath, logName);
            _logConfig = new LogConfigConsumer(_logFilePath);
            _gatherer = Catalog.Factory.Resolve<IApplicationNodeGatherer>();

            _alert = Catalog.Factory.Resolve<IApplicationAlert>();

            var rnd = new Random();
            _updateCycle = TimeSpan.FromSeconds(60.0 + (rnd.NextDouble()*30.0));

        }

        private void BeginLogging()
        {
            _log = ClassLogger.Create(GetType());
            _dbLog = DebugOnlyLogger.Create(_log);
            
        }

        public void Initialize(TimeSpan updateCycle)
        {
            BeginLogging();
            _updateCycle = updateCycle;

            _dbLog.InfoFormat("Initializing application node to update every {0}", updateCycle);

        }

        private IPAddress GetLocalIP()
        {
            var address =
                Dns.GetHostEntry(Dns.GetHostName())
                   .AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
           

            return address;
        }
        public void DoTransactNodeInfo(
            IEnumerable<NodeMetric> currentMetrics, 
            DateTime metricsCollectionTime, 
            IEnumerable<NodeAlert> newAlerts, 
            int activityLevel, 
            out ApplicationNodeStates desiredState, 
            out LoggingConfiguration desiredLogging)
        {
            desiredState = ApplicationNodeStates.Running;
            desiredLogging = null;

            try
            {
                using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                {
                    // try to load existing node
                    var appNode = dc.Load<ApplicationNode>(_reg.Id);


                    // create new if necessary
                    bool created = false;
                    if (null == appNode)
                    {
                        _log.Warn("Creating application node record");

                        created = true;
                        appNode = new ApplicationNode
                            {
                                Id = _reg.Id,
                                ComponentType = _reg.ComponentType,
                                MachineName = Environment.MachineName,
                                State = ApplicationNodeStates.Running,
                                Version = _reg.Version,
                                IPAddress = GetLocalIP(),
                                LoggingConfiguration = new LoggingConfiguration
                                    {
                                        ClassFilter = "none",
                                        File = _logFilePath,
                                        LogLevel = 0
                                    }
                            };
                    }

                    // update node data
                    desiredLogging = appNode.LoggingConfiguration;
                    desiredState = appNode.State;


                    appNode.LastPing = DateTime.UtcNow;
                    appNode.MetricCollectionTime = metricsCollectionTime;
                    appNode.Metrics = currentMetrics.ToList();
                    appNode.Alerts.AddRange(newAlerts);
                    appNode.ActivityLevel = activityLevel;

                    if (created)
                    {
                        dc.Store(appNode);
                    }

                    dc.SaveChanges();
                }
            }
            catch (ConcurrencyException)
            {
                // just get it next time
            }
            catch (Exception ex)
            {
                _log.Error(ex.TraceInformation());
                _alert.RaiseAlert(ApplicationAlertKind.Unknown, ex.TraceInformation());
            }
        }

        private void DoRecurringTransact(object _)
        {
            BeginLogging();


            try
            {
                var metrics = _gatherer.GatherMetrics();

                _dbLog.InfoFormat("Application node has {0} metrics", metrics.Count());

                var alerts = _gatherer.NewAlerts();

                _log.InfoFormat("Application node sending {0} alerts", alerts.Count());
                
                var newState = ApplicationNodeStates.Running;
                LoggingConfiguration newLogConfig = null;
                DoTransactNodeInfo(metrics, DateTime.UtcNow, alerts, _gatherer.ActivityLevel,
                    out newState, out newLogConfig);

                if (newState != _state && null != StateChanged)
                {
                    _log.WarnFormat("Application node state: {0}", newState.EnumName());
                    _state = newState;
                    StateChanged(this, new ApplicationNodeStateEvent { State = newState });
                }

                _logConfig.MaybeReconfigureLogging(newLogConfig);


                if (alerts.Any())
                {
                    var appLog = new System.Diagnostics.EventLog();
                    foreach (var alert in alerts)
                    {

                        appLog.Source = Process.GetCurrentProcess().ProcessName;
                        var jsonDetail = JsonConvert.SerializeObject(alert.Detail);

                        var strEv = string.Format("Operational Event {0}:\n{1}", alert.Kind, jsonDetail);
                        appLog.WriteEntry(strEv);
                    }
                }
                
            }
            catch (Exception ex)
            {
                _log.Error(ex.TraceInformation());
                _alert.RaiseAlert(ApplicationAlertKind.Unknown, ex.TraceInformation());
                
            }

            
        }

        
        public void Run()
        {
            BeginLogging();

            _log.InfoFormat("Starting application node at {0} UTC-0", DateTime.UtcNow);
            if (_reg.Id == "none")
            {
                _log.Fatal("The application node is not installed!");
                throw new ApplicationNodeNotInstalledException();
            }

            _recurrence.Recur(_updateCycle, DoRecurringTransact, null);
            _running = true;
        }

        public void Stop()
        {
            BeginLogging();

            _log.InfoFormat("Stopping application node at {0} UTC-0", DateTime.UtcNow);

            if(_running)
                _recurrence.Stop();
        }

        public event EventHandler<ApplicationNodeStateEvent> StateChanged;
    }
}
