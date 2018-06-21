// // 
// //  Copyright 2012 David Gressett
// // 
// //    Licensed under the Apache License, Version 2.0 (the "License");
// //    you may not use this file except in compliance with the License.
// //    You may obtain a copy of the License at
// // 
// //        http://www.apache.org/licenses/LICENSE-2.0
// // 
// //    Unless required by applicable law or agreed to in writing, software
// //    distributed under the License is distributed on an "AS IS" BASIS,
// //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //    See the License for the specific language governing permissions and
// //    limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppComponents.Data;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.RandomNumbers;
using PluginContracts;
using log4net;
using AppComponents.Extensions.ExceptionEx;

namespace AppComponents.Workflow
{
    [Export(typeof (IWorkflowPluginHost))]
    public class WorkflowHost : IWorkerEntryPoint, IWorkflowPluginHost
    {
        private const string WFHostContainerName = "wftemplates";
        private ConcurrentDictionary<string, WorkflowAgent> _agents = new ConcurrentDictionary<string, WorkflowAgent>();

        private string _queueConnection;
        private readonly IMessagePublisher _sender;
  
        private CompositionContainer _container;

        private CancellationToken _ct;

        private DebugOnlyLogger _dblog;
        
        private ILog _log;
        
        private Random _rndGroomTime = GoodSeedRandom.Create();
        private StringResourcesCache _stringResources;
        private readonly Encoding _templateEncoding = new ASCIIEncoding();
        private IFilesContainer _wfTemplates;
        private string _hostId;

        [ImportMany] private IEnumerable<Lazy<IWorkflowPlugin, IWorkflowPluginMetadata>> _workerFactories;

        private IMessagePublisher _broadcaster;
        private IMessagePublisher _publisher;
        private IMessageListener _broadcastListener;
        private IMessageListener _listener;

        private string _workflowDataRepositoryKey;
        private string _workflowMessagingKey;
        private string _workflowWorkspaceKey;

        private IDataRepositoryService<WorkflowMachineState,
                                        WorkflowMachineState,
                                        DataEnvelope<WorkflowMachineState, NoMetadata>,
                                        NoMetadata,
                                        DatumEnvelope<WorkflowMachineState, NoMetadata>,
                                        NoMetadata> _machineStateData;

        private IDataRepositoryService<WorkflowInstanceInfo,
                                        WorkflowInstanceInfo,
                                        DataEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                        NoMetadata,
                                        DatumEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                        NoMetadata> _instanceData;

        private IDataRepositoryService<WorkflowTrigger,
                                        WorkflowTrigger,
                                        DataEnvelope<WorkflowTrigger, NoMetadata>,
                                        NoMetadata,
                                        DatumEnvelope<WorkflowTrigger, NoMetadata>,
                                        NoMetadata> _triggers;
        

        #region IWorkerEntryPoint Members


        public WorkflowHost()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _queueConnection = WorkflowShared.GetMessageQueueHost(cf);

            

            _wfTemplates = Catalog.Preconfigure()
                .Add(BlobContainerLocalConfig.ContainerName, WFHostContainerName)
                .Add(BlobContainerLocalConfig.OptionalAccess, EntityAccess.Private)
                .Add(BlobContainerLocalConfig.OptionalContentType, "application/json")
                .ConfiguredResolve<IFilesContainer>();

            _stringResources = Catalog.Factory.Resolve<StringResourcesCache>();

            var hostEnvironment = Catalog.Factory.Resolve<IHostEnvironment>();
            var _hostId = hostEnvironment.GetCurrentHostIdentifier(HostEnvironmentConstants.DefaultHostScope);

            WorkflowShared.DeclareWorkflowExchanges(_queueConnection, cf[WorkflowConfiguration.WorkflowMessagingKey]);
            WorkflowShared.AttachQueueToWorkflowExchange(_queueConnection, _hostId, cf[WorkflowConfiguration.WorkflowMessagingKey]);
            WorkflowShared.AttachedQueueToWorkflowBroadcast(_queueConnection, _hostId, cf[WorkflowConfiguration.WorkflowMessagingKey]);

            _publisher = Catalog.Preconfigure()
                .Add(MessagePublisherLocalConfig.HostConnectionString, _queueConnection)
                .Add(MessagePublisherLocalConfig.ExchangeName, WorkflowShared.WorkflowExchange)
                .ConfiguredResolve<IMessagePublisher>(cf[WorkflowConfiguration.WorkflowMessagingKey]);

            _broadcaster = Catalog.Preconfigure()
                .Add(MessagePublisherLocalConfig.HostConnectionString, _queueConnection)
                .Add(MessagePublisherLocalConfig.ExchangeName, WorkflowShared.WorkflowFanoutExchange)
                .ConfiguredResolve<IMessagePublisher>(cf[WorkflowConfiguration.WorkflowMessagingKey]);

            _listener = Catalog.Preconfigure()
                .Add(MessageListenerLocalConfig.HostConnectionString, _queueConnection)
                .Add(MessageListenerLocalConfig.ExchangeName, WorkflowShared.WorkflowExchange)
                .Add(MessageListenerLocalConfig.QueueName, _hostId)
                .ConfiguredResolve<IMessageListener>(cf[WorkflowConfiguration.WorkflowMessagingKey]);

            _broadcastListener = Catalog.Preconfigure()
                .Add(MessageListenerLocalConfig.HostConnectionString, _queueConnection)
                .Add(MessageListenerLocalConfig.ExchangeName, WorkflowShared.WorkflowFanoutExchange)
                .Add(MessageListenerLocalConfig.QueueName, _hostId)
                .ConfiguredResolve<IMessageListener>(cf[WorkflowConfiguration.WorkflowMessagingKey]);

            _machineStateData = Catalog.Preconfigure().ConfigureWorkflowDataRepository<WorkflowMachineState>()
                        .ConfiguredResolve<IDataRepositoryService<WorkflowMachineState,
                                                WorkflowMachineState,
                                                DataEnvelope<WorkflowMachineState, NoMetadata>,
                                                NoMetadata,
                                                DatumEnvelope<WorkflowMachineState, NoMetadata>,
                                                NoMetadata>>
                                                (cf[WorkflowConfiguration.WorkflowDataRepositoryKey]);

            _instanceData = Catalog.Preconfigure().ConfigureWorkflowDataRepository<WorkflowInstanceInfo>()
                        .ConfiguredResolve<IDataRepositoryService<WorkflowInstanceInfo,
                                                WorkflowInstanceInfo,
                                                DataEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                                NoMetadata,
                                                DatumEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                                NoMetadata>>
                                                (cf[WorkflowConfiguration.WorkflowDataRepositoryKey]);

            _triggers = Catalog.Preconfigure().ConfigureWorkflowDataRepository<WorkflowTrigger>()
                            .ConfiguredResolve<IDataRepositoryService<WorkflowTrigger,
                                                WorkflowTrigger,
                                                DataEnvelope<WorkflowTrigger, NoMetadata>,
                                                NoMetadata,
                                                DatumEnvelope<WorkflowTrigger, NoMetadata>,
                                                NoMetadata>>
                                                (cf[WorkflowConfiguration.WorkflowDataRepositoryKey]);


            _workflowDataRepositoryKey = cf[WorkflowConfiguration.WorkflowDataRepositoryKey];
            _workflowMessagingKey = cf[WorkflowConfiguration.WorkflowMessagingKey];
            _workflowWorkspaceKey = cf[WorkflowConfiguration.WorkflowWorkspaceKey];
        }

        public void Initialize(CancellationToken token)
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);

            _ct = token;

            Compose();
           
        }



        public bool OnStart()
        {
            _listener.Listen(
                new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(typeof(WakeWorkflowJob), WakeWorkflow),
                new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(typeof(WorkflowCreate), NewWorkflowInstance),
                new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(typeof(WorkflowStateMachineRetry), FireRetryMsg),
                new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(typeof(FireWorkflowTriggerJob), FireTriggerJobMsg));

            _broadcastListener.Listen(new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(typeof(WorkflowTrigger), DistributeWorkflowTriggers));


            var hostEnvironment = Catalog.Factory.Resolve<IHostEnvironment>();
            var hostId = hostEnvironment.GetCurrentHostIdentifier(HostEnvironmentConstants.DefaultHostScope);

            var instancesQuery = new QuerySpecification
            {
                BookMark = new GenericPageBookmark { PageSize = 100 },
                Where = new Filter
                {
                    Rules = new Comparison[]
                    {
                        new Comparison
                            {
                                Data = WorkflowStatus.Complete.ToString(),
                                Field = "Status",
                                Test = Test.NotEqual
                            }
                    }
                }
            };

            Task.Factory.StartNew(() =>
                                      {
                                          // get this host's chunk of workflow instances to run.
                                          Random rnd = GoodSeedRandom.Create();

                                          bool done = false;
                                          bool first = true;
                                          DateTime lastException = DateTime.MinValue;
                                          while (!done)
                                          {
                                              try
                                              {
                                                  int acquired = 0;
                                                  IEnumerable<WorkflowInstanceInfo> instances = Enumerable.Empty<WorkflowInstanceInfo>();
                                                  instancesQuery.BookMark = new GenericPageBookmark { PageSize = 100 };


                                                  while (instancesQuery.BookMark.More)
                                                  {
                                                      var qr = _instanceData.Query(instancesQuery);
                                                      if (qr.Items.Any())
                                                          instances = instances.Concat(qr.Items);
                                                  }

                                                  instances = instances.ToArray();



                                                  IEnumerable<WorkflowInstanceInfo> tryInstances;

                                                  // first pass, pick out existing workflows in a pattern less 
                                                  // likely to interfere with other workflow hosts trying to acquire
                                                  if (first)
                                                  {
                                                      int startAt;
                                                      startAt = rnd.Next(3) * 3;
                                                      if (startAt > instances.Count())
                                                          startAt = rnd.Next(instances.Count() / 3);

                                                      tryInstances = instances.TakeEvery(startAt, 3);
                                                      first = false;
                                                  }
                                                  else
                                                  {
                                                      // subsequent passes, be greedy
                                                      tryInstances = instances;
                                                  }


                                                  int waitDur;


                                                  foreach (var inst in tryInstances)
                                                  {
                                                      if (TryAcquireWorkflow(inst.Id) != null)
                                                      {
                                                          acquired++;
                                                          _log.InfoFormat("Host {0} acquired workflow instance {1} ...",
                                                                          hostId, inst.Id);

                                                          // give other hosts a little space to take some.
                                                          waitDur = rnd.Next(160);
                                                          Thread.Sleep(waitDur);
                                                      }
                                                      else
                                                      {
                                                          _log.InfoFormat(
                                                              "Host {0} could not acquire workflow instance {1} ...", hostId,
                                                              inst.Id);
                                                      }
                                                  }

                                                  if (!first && acquired == 0)
                                                      done = true;

                                                  waitDur = rnd.Next(7654);
                                                  _log.InfoFormat(
                                                      "Host {0} acquired {1} pre-existing workflows, waiting {2} milliseconds to try to attain more.",
                                                      hostId, acquired, waitDur);
                                                  Thread.Sleep(waitDur);
                                              }
                                              catch (Exception ex)
                                              {
                                                  var es = string.Format("exception running workflow host {0}",
                                                                         ex.TraceInformation());
                                                  _dblog.Error(es);
                                                  
                                                  if (DateTime.UtcNow - lastException > TimeSpan.FromMinutes(1.0))
                                                  {
                                                      _log.TraceException(ex);
                                                      var alert = Catalog.Factory.Resolve<IApplicationAlert>();
                                                      alert.RaiseAlert(ApplicationAlertKind.System, es);
                                                  }
                                              }

                                          }
                                      });

            return true;
        }

        public void OnStop()
        {
            try
            {
                _agents.Values.ForEach(a => a.Stop());
                _listener.Dispose();
                _broadcastListener.Dispose();
                WorkflowShared.RemoveQueueFromWorkflowExchange(_queueConnection, _hostId, _workflowMessagingKey);
                WorkflowShared.RemoveQueueFromWorkflowBroadcast(_queueConnection, WorkflowShared.WorkflowFanoutExchange, _workflowMessagingKey);
            }
            catch (Exception ex)
            {
                _dblog.WarnFormat("Exception while stopping workflow host: {0}", ex.ToString());
            }
        }

        public void Run()
        {
        }

        public void ProtectedRun()
        {
            DateTime lastException = DateTime.MinValue;
            while (!_ct.IsCancellationRequested)
            {
                try
                {
                    // any wfs fall through the cracks and are asleep but have triggers?
                    IEnumerable<WorkflowInstanceInfo> sleeping = Enumerable.Empty<WorkflowInstanceInfo>();

                    var instancesQuery = new QuerySpecification
                    {
                        BookMark = new GenericPageBookmark { PageSize = 100 },
                        Where = new Filter
                        {
                            Rules = new Comparison[]
                    {
                        new Comparison
                            {
                                Data = WorkflowStatus.Sleeping.ToString(),
                                Field = "Status",
                                Test = Test.Equal
                            }
                    }
                        }
                    };

                    instancesQuery.BookMark = new GenericPageBookmark { PageSize = 100 };


                    while (instancesQuery.BookMark.More)
                    {
                        var qr = _instanceData.Query(instancesQuery);
                        if (qr.Items.Any())
                            sleeping = sleeping.Concat(qr.Items);
                    }

                    sleeping = sleeping.ToArray();




                    Parallel.ForEach(sleeping,
                                     nc =>
                                     {
                                         try
                                         {
                                             // check for triggers
                                             IEnumerable<WorkflowMachineState> hasTriggers = Enumerable.Empty<WorkflowMachineState>();

                                             var hasTriggersQuery = new QuerySpecification
                                                                        {
                                                                            BookMark = new GenericPageBookmark { PageSize = 100 },
                                                                            Where = new Filter
                                                                                        {
                                                                                            Rules = new Comparison[]
                                                                                                        {
                                                                                                            new Comparison
                                                                                                                {
                                                                                                                    Field = "Parent",
                                                                                                                    Data = nc.Id,
                                                                                                                    Test = Test.Equal
                                                                                                                }
                                                                                                        }
                                                                                        }
                                                                        };

                                             while (hasTriggersQuery.BookMark.More)
                                             {
                                                 var qr = _machineStateData.Query(hasTriggersQuery);
                                                 if (qr.Items.Any())
                                                 {
                                                     hasTriggers = hasTriggers.Concat(qr.Items);
                                                 }
                                             }




                                             // it has some, try to acquire it
                                             if (hasTriggers.Any())
                                             {
                                                 _ct.ThrowIfCancellationRequested();
                                                 _log.InfoFormat(
                                                     "Sleeping workflow {0} has {1} waiting triggers. Waking.", nc.Id,
                                                     hasTriggers.Count());

                                                 TryAcquireWorkflow(nc.Id);
                                             }
                                         }
                                         catch (Exception ex)
                                         {
                                             _dblog.ErrorFormat("Problem while waking workflow {0}:{2}", nc.Id,
                                                                ex.ToString());
                                         }
                                     });


                    // groom completed and data
                    IEnumerable<WorkflowInstanceInfo> completedInstances = Enumerable.Empty<WorkflowInstanceInfo>();

                    var completedQuery = new QuerySpecification
                    {
                        BookMark = new GenericPageBookmark { PageSize = 100 },
                        Where = new Filter
                        {
                            Rules = new Comparison[]
                    {
                        new Comparison
                            {
                                Data = WorkflowStatus.Complete.ToString(),
                                Field = "Status",
                                Test = Test.Equal
                            }
                    }
                        }
                    };

                    completedQuery.BookMark = new GenericPageBookmark { PageSize = 100 };


                    while (completedQuery.BookMark.More)
                    {
                        var qr = _instanceData.Query(completedQuery);
                        if (qr.Items.Any())
                            completedInstances = completedInstances.Concat(qr.Items);
                    }

                    completedInstances = completedInstances.ToArray();




                    Parallel.ForEach(completedInstances,
                                     ci =>
                                     {
                                         try
                                         {
                                             _log.InfoFormat("Grooming completed workflow instance {0}", ci.Id);
                                             _ct.ThrowIfCancellationRequested();

                                             // delete workspaces
                                             var workspace = Catalog.Preconfigure()
                                                 .Add(WorkspaceLocalConfig.WorkspaceName,
                                                      WorkflowShared.WorkflowInstanceWorkspaceName(ci.Id))
                                                 .ConfiguredResolve<IWorkspace>(_workflowWorkspaceKey);


                                             workspace.DeleteWorkspace();


                                             _ct.ThrowIfCancellationRequested();

                                             IEnumerable<WorkflowMachineState> states =
                                                 Enumerable.Empty<WorkflowMachineState>();

                                             var msQuery = new QuerySpecification
                                             {
                                                 BookMark = new GenericPageBookmark { PageSize = 100 },
                                                 Where = new Filter
                                                 {
                                                     Rules = new Comparison[]
                                                    {
                                                        new Comparison
                                                            {
                                                                Data = ci.Id,
                                                                Field = "Parent",
                                                                Test = Test.Equal
                                                            }
                                                    }
                                                 }
                                             };

                                             msQuery.BookMark = new GenericPageBookmark { PageSize = 100 };


                                             while (msQuery.BookMark.More)
                                             {
                                                 var qr = _machineStateData.Query(msQuery);
                                                 if (qr.Items.Any())
                                                     states = states.Concat(qr.Items);
                                             }



                                             _machineStateData.DeleteBatch(states.ToList());
                                             _instanceData.Delete(ci);


                                         }
                                         catch (Exception ex)
                                         {
                                             var es = string.Format(
                                                 "Problem while grooming completed workflow {0}:{1}", ci.Id,
                                                 ex.ToString());
                                             _dblog.Error(es);
                                             var on = Catalog.Factory.Resolve<IApplicationAlert>();
                                             on.RaiseAlert(ApplicationAlertKind.Unknown, es);
                                         }
                                     });

                    var chill = TimeSpan.FromMinutes((30.0 * _rndGroomTime.NextDouble()) + 1.0);
                    _log.InfoFormat("Waiting {0} minutes until next sweep cycle.", chill.TotalMinutes);
                    _ct.WaitHandle.WaitOne(chill);
                }
                catch (Exception ex)
                {
                    var es = string.Format("exception running workflow host {0}",
                                                                         ex.TraceInformation());
                    _dblog.Error(es);

                    if (DateTime.UtcNow - lastException > TimeSpan.FromMinutes(1.0))
                    {
                        _log.TraceException(ex);
                        var alert = Catalog.Factory.Resolve<IApplicationAlert>();
                        alert.RaiseAlert(ApplicationAlertKind.System, es);
                    }
                }

            }
        }

        #endregion

        #region IWorkflowPluginHost Members

        /// <summary>
        ///   PluginTemplate wants to fire a trigger.
        /// </summary>
        /// <param name="agentId"> </param>
        /// <param name="machine"> </param>
        /// <param name="trigger"> </param>
        /// <param name="msg"> </param>
        public void FireOnHostedWorkflow(string agentId, string machine, string trigger)
        {
            _dblog.InfoFormat("PluginTemplate host firing on instance {0}: machine {1}: {2}.", agentId, machine, trigger);


            var fire = new WorkflowTrigger
                           {
                               TriggerName = trigger,
                               InstanceTarget = agentId,
                               MachineContext = machine,
                               Route = TriggerRoutes.FireRoute,
                               Id = Guid.NewGuid().ToString()
                           };

            InternalFireOnHostedWorkflow(agentId, fire);
        }

        public void EndWorkflow(string agentId)
        {
            _dblog.InfoFormat("PluginTemplate host end workflow on instance {0}: machine {1}: {2}.", agentId);


            var fire = new WorkflowTrigger
                           {
                               Id = Guid.NewGuid().ToString(),
                               Route = TriggerRoutes.EndRoute,
                               InstanceTarget = agentId,
                           };

            InternalFireOnHostedWorkflow(agentId, fire);
        }


        /// <summary>
        ///   PluginTemplate wants to know a workflow state
        /// </summary>
        /// <param name="agentId"> </param>
        /// <param name="machine"> </param>
        /// <returns> </returns>
        public string GetHostedWorkflowState(string agentId, string machine)
        {
            if (!_agents.ContainsKey(agentId))
            {
                _log.WarnFormat("PluginTemplate requested state on instance {0} which is not hosted.", agentId);
                return null;
            }

            return _agents[agentId].StateMachines[machine].State;
        }

        public void WriteWorkspaceDataStrings(string agentId, IEnumerable<KeyValuePair<string, string>> workspaceData)
        {
            if (!_agents.ContainsKey(agentId))
            {
                var es = string.Format("PluginTemplate requested state on instance {0} which is not hosted.", agentId);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);
                return;
            }

            _agents[agentId].StoreWorkflowData(workspaceData);
        }

        public string ReadWorkspaceDataString(string agentId, string key)
        {
            if (!_agents.ContainsKey(agentId))
            {
                var es = string.Format("PluginTemplate requested state on instance {0} which is not hosted.", agentId);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);
                return null;
            }

            return _agents[agentId].ReadWorkspaceData(key);
        }

        #endregion


        public string ConnectionHost 
        {
            get { return _queueConnection; }
        }

        /// <summary>
        ///   load workflow plugins and bind them to this host.
        /// </summary>
        private void Compose()
        {
            var catalog = new AggregateCatalog();
            var assemblies =
                AppDomain.CurrentDomain.GetAssemblies().Where(
                    a =>
                    !a.IsDynamic && !a.FullName.StartsWith("System.") && !a.FullName.StartsWith("Microsoft.")
                    && !a.FullName.StartsWith("DotNet"));
            

            assemblies.ForEach(entry =>
                             {
                                 _log.InfoFormat("Workflow host adding plugins from assembly {0}", entry.FullName);
                                 catalog.Catalogs.Add(new AssemblyCatalog(entry));
                             });

            var lfm = Catalog.Factory.Resolve<ILocalFileMirror>();
            var di = new DirectoryInfo(Path.Combine(lfm.TargetPath, lfm.TargetFolder));
            if(!di.Exists)
                di.Create();
            var dirCat = new DirectoryCatalog(lfm.TargetPath, "*wf.dll");
            _log.InfoFormat("Workflow host adding plugins from local file mirror: {0}",
                            string.Join(",", dirCat.LoadedFiles.ToArray()));

            if(dirCat.LoadedFiles.Any())
                catalog.Catalogs.Add(dirCat);

            _container = new CompositionContainer(catalog);

            try
            {
                _container.ComposeParts(this);
            }
            catch (CompositionException compositionException)
            {
                var es = string.Format("Exception loading workflow plugins: {0}", compositionException.Message);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.Unknown, es);
                _log.Error(compositionException.Message, compositionException);
                
            }
        }

        /// <summary>
        ///   Give workflow agents instances of plugins to use
        /// </summary>
        /// <param name="id"> </param>
        /// <param name="route"> </param>
        /// <returns> </returns>
        public IWorkflowWorker Resolve(string id, string route)
        {
            _log.InfoFormat("Resolving worker for route {0}", route);

            var routePaths = route.Split('/');
            var routeRoot = routePaths.First();
            var factories = _workerFactories.ToArray();
            var factory = (from l in factories
                           where l.Metadata.Route == id
                           select l.Value).SingleOrDefault();

            if (null != factory)
            {
                return factory.CreateWorkerInstance(route, id);
            }

            var msg = string.Format("Could not resolve workflow plugin route {0}", route);
            var on = Catalog.Factory.Resolve<IApplicationAlert>();
            on.RaiseAlert(ApplicationAlertKind.Unknown, msg);
            _log.Error(msg);
#if DEBUG
            var routes = string.Join("\n",
                                     _workerFactories.Select(
                                         wf =>
                                         string.Format("{0}/t/t/t{1}", wf.Metadata.Route,
                                                       wf.Value.GetType().FullName)));
            _dblog.InfoFormat("Routes available:\n{0}", routes);
#endif
            throw new WorkflowPluginNotFoundException(msg);
        }

        private void InternalFireOnHostedWorkflow(string agentId, WorkflowTrigger fire)
        {

            _triggers.Store(fire);

            


            if (_agents.ContainsKey(agentId))
            {
                _agents[agentId].Bump();
            }
            else
            {
                _log.WarnFormat(
                    "Attempt to fire on hosted workflow {0} that isn't hosted here, redirecting to broadcast.", agentId);

                _broadcaster.Send(fire, "_");
                
            }
        }


        private void InternalDistributeTrigger(CancellationToken ct, WorkflowTrigger wft)
        {
            if (_agents.ContainsKey(wft.InstanceTarget))
            {
                DistributeWorkflowTriggers(wft, ct, null);
            }
            else
            {
                _broadcaster.Send(wft, "_");
                
            }
        }

        private void FireTriggerJobMsg(object msg, CancellationToken ct, IMessageAcknowledge ack)
        {
            var wftj = (FireWorkflowTriggerJob) msg;
            try
            {
                FireTriggerJob(ct, wftj);
                ack.MessageAcknowledged();
            }
            catch (Exception)
            {
                ack.MessageRejected();
                throw;
            }
        }

        private void FireTriggerJob(CancellationToken ct, FireWorkflowTriggerJob wftj)
        {
            var wft = new WorkflowTrigger
                          {
                              InstanceTarget = wftj.Instance,
                              MachineContext = wftj.Context,
                              TriggerName = wftj.Trigger,
                              TriggerId = Guid.NewGuid().ToString()
                          };

            InternalDistributeTrigger(ct, wft);
        }

        private void FireRetryMsg(object msg, CancellationToken ct, IMessageAcknowledge ack)
        {
            var wfsmr = (WorkflowStateMachineRetry)msg;
            try
            {
                FireRetry(ct, wfsmr);
                ack.MessageAcknowledged();
            }
            catch (Exception)
            {
                
                ack.MessageRejected();
                throw;
            }
        }

        private void FireRetry(CancellationToken ct, WorkflowStateMachineRetry wfsmr)
        {
            var wft = new WorkflowTrigger
                          {
                              InstanceTarget = wfsmr.Id,
                              TriggerId = Guid.NewGuid().ToString(),
                              TriggerName = CommonTriggers.StateRetryTrigger,
                              MachineContext = wfsmr.Machine,
                              Route = string.Empty,
                          };

            InternalDistributeTrigger(ct, wft);
        }

        /// <summary>
        ///   Job scheduler says it's time to wake a workflow.
        /// </summary>
        private void WakeWorkflow(object msg, CancellationToken ct, IMessageAcknowledge ack)
        {
            try
            {
                var wwfj = (WakeWorkflowJob)msg;
                _log.InfoFormat("Scheduled wake of sleeping workflow {0}", wwfj.Id);
                TryAcquireWorkflow(wwfj.Id);
                ack.MessageAcknowledged();
            }
            catch(Exception)
            {
                ack.MessageRejected();
                throw;
            }
            
            
        }


        /// <summary>
        ///   Message came in saying to trigger a workflow. If we don't host it now, assume it's sleeping.
        /// </summary>
        private void DistributeWorkflowTriggers(object msg, CancellationToken ct, IMessageAcknowledge ack)
        {

            try
            {
                var trigger = (WorkflowTrigger)msg;

                if (_agents.ContainsKey(trigger.InstanceTarget))
                {
                    InvokeTrigger(_agents[trigger.InstanceTarget], trigger);
                }
                else
                {
                    // try to get a handle on possibly sleeping workflow.

                    var newAgent = TryAcquireWorkflow(trigger.InstanceTarget);
                    if (null != newAgent)
                    {
                        _log.InfoFormat("Waking sleeping workflow {0} with trigger {1}", trigger.InstanceTarget,
                                        trigger.TriggerName ?? "none");
                        InvokeTrigger(_agents[trigger.InstanceTarget], trigger);
                    }
                    else
                    {
                        _dblog.InfoFormat("Ignoring trigger on instance {0} because it is active on another host.",
                                          trigger.InstanceTarget);
                    }
                }

                if(null != ack)
                    ack.MessageAcknowledged();
            }
            catch (Exception)
            {
                if(null != ack) ack.MessageRejected();
                throw;
            }

        }

        /// <summary>
        ///   Message came in to create a new workflow.
        /// </summary>
        private void NewWorkflowInstance(object msg, CancellationToken ct, IMessageAcknowledge ack)
        {
            
                var wfc = (WorkflowCreate)msg;


                Debug.Assert(!string.IsNullOrEmpty(wfc.TemplateName) || !string.IsNullOrEmpty(wfc.TemplateContent));

                try
                {
                    _log.InfoFormat("Hosting new workflow instance of type {0}", wfc.TemplateName);

                    string templateData = !string.IsNullOrEmpty(wfc.TemplateContent)
                                              ? wfc.TemplateContent
                                              : LoadWorkflowTemplate(wfc.TemplateName);

                    var agent = new WorkflowAgent(this, wfc.Id, _ct, wfc.InitialData, templateData, _workflowDataRepositoryKey, _workflowMessagingKey, _workflowWorkspaceKey);
                    _agents.TryAdd(wfc.Id, agent);
                    agent.Start();
                    ack.MessageAcknowledged();
                }
                catch (Exception ex)
                {
                    var es = string.Format("Problem while trying to host new workflow instance for type {0}: {1}",
                                           wfc.TemplateName, ex);
                    _log.Error(es);
                    var on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.Unknown, es);
                    if (_agents.ContainsKey(wfc.Id))
                    {
                        WorkflowAgent _;
                        _agents.TryRemove(wfc.Id, out _);
                    }
                    ack.MessageRejected();
                }
           

        }


        private void InvokeTrigger(WorkflowAgent agent, WorkflowTrigger trigger)
        {
            _log.InfoFormat("Received trigger invocation on hosted workflow {0}: {1}.{2}", agent.Id,
                            trigger.MachineContext, trigger.TriggerName ?? "none");

            // trigger is already in the trigger table.
            agent.Bump();
        }

        /// <summary>
        ///   Try to lock and host a pre-existing workflow.
        /// </summary>
        /// <param name="workflowId"> </param>
        /// <returns> </returns>
        private WorkflowAgent TryAcquireWorkflow(string workflowId)
        {
            WorkflowAgent acquired = null;
            var wfMutex = Catalog.Preconfigure()
                .Add(DistributedMutexLocalConfig.Name, WorkflowShared.WorkflowInstanceLockName(workflowId))
                .ConfiguredResolve<IDistributedMutex>();

            if (wfMutex.Open())
            {
                try
                {
                    var wftemp = GetWorkflowTemplate(workflowId);
                    if (!string.IsNullOrWhiteSpace(wftemp))
                    {
                        acquired = WorkflowAgent.AcquireSleepingOnLocked(wfMutex, this, workflowId, _ct, wftemp, _workflowDataRepositoryKey, _workflowMessagingKey, _workflowWorkspaceKey);
                        if (null != acquired)
                        {
                            if (_agents.TryAdd(workflowId, acquired))
                            {
                                acquired.Start();
                            }
                            else
                            {
                                _dblog.WarnFormat("Strange: locked workflow {0} but couldn't add it", workflowId);
                            }
                        }
                    }
                }
                catch (SynchronizationLockException)
                {
                    _dblog.InfoFormat("Synchronization lock exception while trying to acquire {0}", workflowId);
                    wfMutex.Release();
                }
                catch (WorkflowTemplateException wfex)
                {
                    var es = string.Format("Error loading template for workflow! {0}: {1}", workflowId, wfex);
                    _log.Error(es);
                    var on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.Unknown, es);
                    wfMutex.Release();
                }
                catch (Exception ex)
                {
                    var es = string.Format("Error while acquiring workflow, {0}", ex);
                    _log.Error(es);
                    var on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.Unknown, es);
                    wfMutex.Release();
                }
            }
            else
            {
                _dblog.InfoFormat("Could not lock {0}", workflowId);
            }

            return acquired;
        }

        private string GetWorkflowTemplate(string workflowId)
        {
            string template = null;


            WorkflowInstanceInfo inst;
            inst = _instanceData.Load(workflowId).Item;
           


            if (null != inst)
            {
                _dblog.InfoFormat("Workflow {0} loading template {1}", workflowId, inst.TemplateName);
                template = LoadWorkflowTemplate(inst.TemplateName);
            }
            else
            {
                var es = string.Format("Cannot load template for non-existent workflow instance {0}", workflowId);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.Unknown, es);
            }

            return template;
        }

        private string LoadWorkflowTemplate(string templateName)
        {
            var jsonName = templateName + ".json";
            if (_stringResources.ResourceNames.Contains(jsonName))
            {
                return _stringResources[jsonName];
            }

            string template = null;
            var rawTemplate = _wfTemplates.Get(templateName);

            if (null != rawTemplate && rawTemplate.Length > 0)
            {
                template = _templateEncoding.GetString(rawTemplate);
            }
            else
            {
                var es = string.Format("Could not load workflow template named {0}", templateName);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.Unknown, es);
            }
            return template;
        }


        /// <summary>
        ///   Workflow agent is done, says: get me out of here!
        /// </summary>
        /// <param name="agentId"> </param>
        public void DeHost(string agentId)
        {
            _dblog.InfoFormat("Dehosting {0}", agentId);
            WorkflowAgent _;
            _agents.TryRemove(agentId, out _);
        }


        
    }


    /// <summary>
    ///   constants for kinds of triggers
    /// </summary>
    public static class TriggerRoutes
    {
        public const string FireRoute = "fire";
        public const string NapRoute = "nap";
        public const string EndRoute = "end";
        public const string WakeRoute = "wake";
    }


    /// <summary>
    ///   Workflow instance creation message.
    /// </summary>
    public class WorkflowCreate
    {
        public string InitialData { get; set; }
        public string TemplateName { get; set; }
        public string TemplateContent { get; set; }
        public string Id { get; set; }
    }


    internal class BumpWorkflowMessage
    {
        public string InstanceTarget { get; set; }
    }

    public class WorkflowStateMachineRetry
    {
        public string Id { get; set; }
        public string Machine { get; set; }
    }


    public class WorkflowPluginNotFoundException : Exception
    {
        public WorkflowPluginNotFoundException()
        {
        }

        public WorkflowPluginNotFoundException(string msg)
            : base(msg)
        {
        }
    }
}