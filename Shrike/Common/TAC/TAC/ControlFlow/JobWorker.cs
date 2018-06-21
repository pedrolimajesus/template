using System;
using System.Linq;

using Newtonsoft.Json;
using log4net;

namespace AppComponents.ControlFlow
{
    public class ScheduledJobWorker : SleepyWorkerEntryPoint
    {
        public const string ExchangeName = "scheduledjobexchange";
        private readonly DebugOnlyLogger _dlog;

        private readonly IHostEnvironment _hostEnv;
        private readonly ILog _logger;
        private readonly IMessagePublisher _sender;
        private IDistributedMutex _jobsMutex;
        private IJobScheduler _jobScheduler;

        private bool _paused = false;

        public ScheduledJobWorker()
        {
            _hostEnv = Catalog.Factory.Resolve<IHostEnvironment>();

            _logger = ClassLogger.Create(typeof(ScheduledJobWorker));
            _dlog = DebugOnlyLogger.Create(_logger);

            _jobScheduler = Catalog.Factory.Resolve<IJobScheduler>();

            var config = Catalog.Factory.Resolve<IConfig>();
            _sender = Catalog.Preconfigure()
                .Add(MessagePublisherLocalConfig.HostConnectionString, config[CommonConfiguration.DefaultBusConnection])
                .Add(MessagePublisherLocalConfig.ExchangeName, ExchangeName)
                .ConfiguredResolve<IMessagePublisher>(ScopeFactoryContexts.Distributed);
        }

        public void TogglePause()
        {
            _paused = !_paused;
        }

        public override int ProcessItems()
        {

            if (_paused)
                return 0;

            _jobsMutex = Catalog.Preconfigure()
                .Add(DistributedMutexLocalConfig.Name, ScheduledItem.MutexName)
                .ConfiguredResolve<IDistributedMutex>();

            try
            {

                if (_jobsMutex.Wait(TimeSpan.FromMinutes(4)))
                {
                    var dueCount = 0;

                    using (_jobsMutex)
                    {
                        if (_jobsMutex.Open())
                        {
                            _dblog.InfoFormat("Worker {0} processing scheduled jobs",
                                              _hostEnv.GetCurrentHostIdentifier(
                                                  HostEnvironmentConstants.DefaultHostScope));


                            var due = _jobScheduler.GetDue();
                            dueCount = due.Count();

                            _log.InfoFormat("Processing {0} due jobs", due.Count());

                            foreach (var item in due)
                            {
                                _token.ThrowIfCancellationRequested();

                                _dblog.InfoFormat("Job due, starting {1} {2} : {0}", item.Message, item.Type, item.Route);
                                _jobScheduler.Reschedule(item);

                                var message = JsonConvert.DeserializeObject(item.Message, item.Type);
                                _sender.Send(message, item.Route);

                            }

                        
                        }
                    }

                    return dueCount;
                }

            }
            catch (Exception ex)
            {
                _log.ErrorFormat(string.Format("An Exception was Caught in {0} ERROR: {1}", this.GetType().Name ,ex.Message));

            }

            return 0;
        }
    }
}
