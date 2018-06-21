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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AppComponents.Data;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.Extensions.ExceptionEx;
using AppComponents.RandomNumbers;
using Newtonsoft.Json;
using PluginContracts;
using log4net;

namespace AppComponents.Workflow
{

    /// <summary>
    /// Worker thread agent that executes the logic for a workflow instance embedded in a workflow host.
    /// </summary>
    public class WorkflowAgent : IDisposable
    {
        private TimeSpan _autoSleepTime = TimeSpan.FromSeconds(600);
        private ManualResetEventSlim _bumpEvent = new ManualResetEventSlim(false);
        private CancellationToken _ct;
        private DebugOnlyLogger _dblog;
        private string _fallThroughMachine;
        private WorkflowHost _host;
        private TimeSpan _initialSleepTimeout = TimeSpan.FromSeconds(15.0);
        private bool _isDisposed;
        private ILog _log;
        private bool _runAgent = true;
        private IDistributedMutex _wfLock;
        private string _workspaceContainerName;
        private string _workflowDataRepositoryKey;
        private string _workflowWorkspaceKey;

        private readonly IDataRepositoryService<WorkflowInstanceInfo,
                                        WorkflowInstanceInfo,
                                        DataEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                        NoMetadata,
                                        DatumEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                        NoMetadata> _instanceData;

        private readonly IDataRepositoryService<WorkflowTrigger,
                                        WorkflowTrigger,
                                        DataEnvelope<WorkflowTrigger, NoMetadata>,
                                        NoMetadata,
                                        DatumEnvelope<WorkflowTrigger, NoMetadata>,
                                        NoMetadata> _triggers;



        private readonly Regex _matchWorkflowState = new Regex(@"{state\(([^\)]*)\)}");
        private readonly Regex _matchWorkspaceKey = new Regex(@"{workspace\(([^\)]*)\)}");
        private readonly Regex _matchConfigKey = new Regex(@"{config\(([^\)]*)\)}");


        public WorkflowAgent(WorkflowHost host, string newId, CancellationToken ct, string initialData,
                             string templateData, string workflowDataRepositoryKey, string workflowMessagingKey, string workflowWorkspaceKey)
        {
            Id = newId;
            _ct = ct;

            _workflowDataRepositoryKey = workflowDataRepositoryKey;
            _workflowWorkspaceKey = workflowWorkspaceKey;

            _instanceData = Catalog.Preconfigure().ConfigureWorkflowDataRepository<WorkflowInstanceInfo>()
                                            .ConfiguredResolve<IDataRepositoryService<WorkflowInstanceInfo,
                                                WorkflowInstanceInfo,
                                                DataEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                                NoMetadata,
                                                DatumEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                                NoMetadata>>
                                                (_workflowDataRepositoryKey);

            _triggers = Catalog.Preconfigure().ConfigureWorkflowDataRepository<WorkflowTrigger>()
                                            .ConfiguredResolve<IDataRepositoryService<WorkflowTrigger,
                                                WorkflowTrigger,
                                                DataEnvelope<WorkflowTrigger, NoMetadata>,
                                                NoMetadata,
                                                DatumEnvelope<WorkflowTrigger, NoMetadata>,
                                                NoMetadata>>
                                                (_workflowDataRepositoryKey);

           
          
            Initialize(host, templateData);

            if (!string.IsNullOrEmpty(initialData))
            {
                var initialDataSet = JsonConvert.DeserializeObject<Dictionary<string, string>>(initialData);

                

                try
                {
                    foreach (var id in _inputDefinitions.EmptyIfNull().Where(def => !def.Optional))
                    {
                        if (!initialDataSet.ContainsKey(id.WorkspaceKey))
                        {
                            throw new InvalidWorkflowInputsException(string.Format("workspace inputs require {0}, not found",id.WorkspaceKey));
                        }
                    }
                    

                    if (!_wfLock.Wait(TimeSpan.FromSeconds(300.0)))
                    {
                        throw new SynchronizationLockException(string.Format("Timeout waiting for workspace data to {0}", Id));
                    }

                    Workspace.Batch(
                        wsd => initialDataSet.ForEach(datum=>wsd.Put(datum.Key,datum.Value))
                        );
                }
                catch(Exception ex)
                {
                    var es = string.Format("Exception initializing workspace inputs, {0}", ex.Message);
                    _log.Error(es);
                    ex.TraceInformation();
                    var on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.System, es);
                    throw;
                }
                finally
                {
                    _wfLock.Release();
                }
            }
        }

        private WorkflowAgent(WorkflowHost host, string existing, CancellationToken ct, IDistributedMutex dm,
                              string templateData, string workflowDataRepositoryKey, string workflowMessagingKey, string workflowWorkspaceKey)
        {
            Id = existing;
            _ct = ct;
            _wfLock = dm;

            _workflowDataRepositoryKey = workflowDataRepositoryKey;
            _workflowWorkspaceKey = workflowWorkspaceKey;

            _instanceData = Catalog.Preconfigure().ConfigureWorkflowDataRepository<WorkflowInstanceInfo>()
                                        .ConfiguredResolve<IDataRepositoryService<WorkflowInstanceInfo,
                                                WorkflowInstanceInfo,
                                                DataEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                                NoMetadata,
                                                DatumEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                                NoMetadata>>
                                                (_workflowDataRepositoryKey);

            _triggers = Catalog.Preconfigure().ConfigureWorkflowDataRepository<WorkflowTrigger>()
                                        .ConfiguredResolve<IDataRepositoryService<WorkflowTrigger,
                                                WorkflowTrigger,
                                                DataEnvelope<WorkflowTrigger, NoMetadata>,
                                                NoMetadata,
                                                DatumEnvelope<WorkflowTrigger, NoMetadata>,
                                                NoMetadata>>
                                                (_workflowDataRepositoryKey);

            Initialize(host, templateData);
        }

        public string Name { get; private set; }
        public string Version { get; private set; }
        public Dictionary<string, IWorkflowWorker> Workers { get; private set; }
        public Dictionary<string, WorkflowStateMachine> StateMachines { get; private set; }
        public string Id { get; private set; }
        public IWorkspace Workspace { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _wfLock.Release();
                _wfLock = null;
                _bumpEvent.Dispose();
                _isDisposed = true;
            }
        }

        #endregion

        private void Initialize(WorkflowHost host, string templateData)
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);

            _host = host;


            _workspaceContainerName = WorkflowShared.WorkflowInstanceWorkspaceName(Id);

            Workspace = Catalog.Preconfigure()
                .Add(WorkspaceLocalConfig.WorkspaceName, _workspaceContainerName)
                .ConfiguredResolve<IWorkspace>(_workflowWorkspaceKey);


            if (null == _wfLock)
                _wfLock = ConstructLock(Id);

           

            SpecifyUsingTemplate(templateData);

            var instance = new WorkflowInstanceInfo
                               {
                                   Id = Id,
                                   TemplateName = Name,
                                   LastActivity = DateTime.UtcNow,
                                   NextActivationTime = DateTime.UtcNow,
                                   Status = WorkflowStatus.Active.ToString(),
                               };

            _instanceData.Store(instance);


          
        }

        

        public void StoreWorkflowData(IEnumerable<KeyValuePair<string, string>> workspaceData)
        {
            try
            {
                if (!_wfLock.Wait(TimeSpan.FromSeconds(60.0)))
                {
                    throw new SynchronizationLockException();
                }
                
                Workspace.Batch(wsd=>
                    workspaceData.ForEach(data => wsd.Put(data.Key, data.Value)));

            }
            catch (SynchronizationLockException)
            {
                var es = string.Format("Timed out waiting to write workspace data to {0}", Id);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);
            }
            finally 
            {
                
                _wfLock.Release();
            }
        }

        public string ReadWorkspaceData(string key)
        {
            return GetWorkspaceValue(key);
        }

        private static IDistributedMutex ConstructLock(string instanceId)
        {
            return Catalog.Preconfigure()
                .Add(DistributedMutexLocalConfig.Name, WorkflowShared.WorkflowInstanceLockName(instanceId))
                .Add(DistributedMutexLocalConfig.UnusedExpirationSeconds, 60)
                .ConfiguredResolve<IDistributedMutex>();
        }


        private string WorkerKey(string instance, string route)
        {
            return instance + @"/" + route.Split('/').First();
        }

        private void RouteAction(string worker, string route)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(worker));
            Debug.Assert(!string.IsNullOrWhiteSpace(route));

            route = RouteSubstitutions(route);

            _dblog.DebugFormat("workflow instance {0} taking action {1}/{2}", Id, worker, route);

            if (Workers.ContainsKey(WorkerKey(worker,route)))
            {
                try
                {
                    Workers[WorkerKey(worker,route)].Invoke(Id, route);
                }
                catch (Exception ex)
                {
                    var es = string.Format("Exception while invoking route {0}: {1},\n{2}", worker, route, ex);
                    _log.Error(es);
                    var on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.System, es);
                }
            }
            else
            {
                var es = string.Format("Cannot invoke action on worker {0}, this worker doesn't exist in the workflow",
                                       worker);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);
            }
        }

        private bool RouteCondition(string worker, string route, string state, string trigger)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(worker));
            Debug.Assert(!string.IsNullOrWhiteSpace(route));

            route = RouteSubstitutions(route);

            if (Workers.ContainsKey(WorkerKey(worker,route)))
            {
                try
                {
                    var check = Workers[WorkerKey(worker,route)].Guard(Id, route, state, trigger);
                    _dblog.DebugFormat("Workflow {0} checking condition {0}/{1} on state {2}, trigger {3}, returns {4}", Id, worker, route, state, trigger, check);
                    return check;
                }
                catch (Exception ex)
                {
                    var es = string.Format("Exception while invoking guard {0}: {1},\n{2}", worker, route, ex);
                    _log.Error(es);
                    var on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.System, es);
                }
            }
            else
            {
                var es = string.Format("Cannot invoke condition on worker {0}, this worker doesn't exist in the workflow",
                                       worker);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);
            }
            return false;
        }

        private string RouteDecision(string worker, string route, string trigger, string state)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(worker));
            Debug.Assert(!string.IsNullOrWhiteSpace(route));

            route = RouteSubstitutions(route);

            if (Workers.ContainsKey(WorkerKey(worker,route)))
            {
                try
                {
                    var nextState = Workers[WorkerKey(worker,route)].DecideTransition(Id, route, trigger, state);
                    _dblog.DebugFormat("Workflow {0} routing decision on {1}/{2} for state {3}, trigger {4} returns {5}",
                        Id, worker, route, state, trigger, nextState);
                    return nextState;
                }
                catch (Exception ex)
                {
                    var es = string.Format("Exception while invoking decision {0}: {1},\n{2}", worker, route, ex);
                    _log.Error(es);
                    var on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.System, es);
                }
            }
            else
            {
                var es = string.Format(
                    "Cannot invoke decision on worker {0}, this worker doesn't exist in the workflow", worker);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);
            }

            return state;
        }

        

        private string RouteSubstitutions(string route)
        {
            var processedRoute = route;

            try
            {
                // current instance Id
                processedRoute = processedRoute.Replace(@"{id}", Id);

                // named workflow state
                processedRoute = MakeSubstitution(processedRoute,
                    _matchWorkflowState.Match(processedRoute), argument => StateMachines[argument].State);

                // workspace value
                processedRoute = MakeSubstitution(processedRoute,
                    _matchWorkspaceKey.Match(processedRoute), GetWorkspaceValue);

                // configuration value
                processedRoute = MakeSubstitution(processedRoute,
                    _matchConfigKey.Match(processedRoute), GetSubstitutionConfigValue);
            }
            catch (Exception ex)
            {
                var es = string.Format(
                    "Substitution failed for {0} on workflow {1}:\n\t{2}", route, Id, ex.Message);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);
               
            }
            
            
            return processedRoute;
        }

        private string GetSubstitutionConfigValue(string argument)
        {
            return SubstitutionConfig[argument];
        }

        private readonly Regex _matchArguments = new Regex("\\((?<TextInsideBrackets>([^\\)]*))\\)");

        private string MakeSubstitution(string processedRoute, Match match, Func<string, string> getValue)
        {
            while (match.Success)
            {
                var substString = match.Value;
                var argument = _matchArguments.Match(substString).Groups["TextInsideBrackets"].Value;
                var val = getValue(argument);
                if (string.IsNullOrEmpty(val))
                    val = "null";
                val = val.Replace('/', '|');
                processedRoute = processedRoute.Replace(match.Value, val);
                match = match.NextMatch();
            }
            return processedRoute;
        }

        private string GetWorkspaceValue(string key)
        {
            string data = string.Empty;
            try
            {
                if (!_wfLock.Wait(TimeSpan.FromSeconds(60.0)))
                {
                    throw new SynchronizationLockException();
                }

                Workspace.Batch(wsd => data = wsd.Get<string>(key));
            }
            catch (SynchronizationLockException)
            {
                var es = string.Format("Timed out waiting to write workspace data to {0}", Id);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);
            }
            finally
            {
                _wfLock.Release();    
            }

            
            return data;
        }

        private IConfig _substitutionsConfig;
        private List<InputDefinition> _inputDefinitions;

        private IConfig SubstitutionConfig
        {
            get
            {
                if (null == _substitutionsConfig)
                {
                    if (Catalog.Factory.CanResolve<IConfig>(WorkflowConfiguration.OptionalWorkflowSubstituteConfigKey))
                        _substitutionsConfig =
                            Catalog.Factory.Resolve<IConfig>(WorkflowConfiguration.OptionalWorkflowSubstituteConfigKey);
                    else
                    {
                        _substitutionsConfig = Catalog.Factory.Resolve<IConfig>();
                    }
                }
                return _substitutionsConfig;
            }
        }

        private void UnhandledTrigger(string stateMachine, string state, string trigger)
        {
            if (
                !string.IsNullOrEmpty(_fallThroughMachine)
                && String.Compare(stateMachine, _fallThroughMachine) != 0)
            {
                _log.WarnFormat("Fall through trigger on machine {0} from state {1} on trigger {2}", stateMachine, state,
                                trigger);
                StateMachines[_fallThroughMachine].Fire(trigger);
            }
            else
            {
                _log.ErrorFormat("Unhandled trigger on machine {0} from state {1} on trigger {2}", stateMachine, state,
                                 trigger);
            }
        }


        private void ValidateWork(string worker, string route)
        {
            string msg = null;
            if (!Workers.ContainsKey(WorkerKey(worker,route)))
            {
                msg = string.Format("Template uses worker {0} without loading a worker plugin for it", worker);
            }

            if (!Workers[WorkerKey(worker,route)].SupportsRoute(route))
            {
                msg = string.Format("Template attempts route {0} on worker {1}, but it doesn't support it.", route,
                                    worker);
            }

            if (!string.IsNullOrEmpty(msg))
            {
                _log.ErrorFormat(msg);
                throw new WorkflowTemplateException(msg);
            }
        }

        // load from json template
        public void SpecifyUsingTemplate(string templateData)
        {
            Workers = new Dictionary<string, IWorkflowWorker>();
            StateMachines = new Dictionary<string, WorkflowStateMachine>();

            var template = WorkflowTemplate.FromJson(templateData);

            Name = template.Name;
            Version = template.Version;

            _fallThroughMachine = template.Fallthrough;

         
            _autoSleepTime = TimeSpan.FromSeconds(template.AutoSleepSeconds);

            var plugins = template.Plugins.EmptyIfNull();
            if (!plugins.Any())
                throw new WorkflowTemplateException("template has no worker plugins");

            var statemachines = template.StateMachines.EmptyIfNull();
            if (!statemachines.Any())
                throw new WorkflowTemplateException("template has no statemachines defined");


            foreach (var plugin in plugins)
            {

                if (string.IsNullOrWhiteSpace(plugin.Instancename))
                    throw new WorkflowTemplateException("plugin has no name specified.");
                if (string.IsNullOrWhiteSpace(plugin.Route))
                    throw new WorkflowTemplateException("plugin has no route specified.");

                var worker = _host.Resolve(plugin.Instancename, plugin.Route);
                if (!Workers.ContainsKey(WorkerKey(plugin.Instancename, plugin.Route)))
                    Workers.Add(WorkerKey(plugin.Instancename, plugin.Route), worker);
            }

           

            foreach (var sm in statemachines)
            {


                if (string.IsNullOrWhiteSpace(sm.Name))
                    throw new WorkflowTemplateException("state machine has no name specified");
                if (string.IsNullOrWhiteSpace(sm.InitialState))
                    throw new WorkflowTemplateException("state machine has no initial state specified");

                var states = sm.States.EmptyIfNull();
                if (!states.Any())
                    throw new WorkflowTemplateException("state machine has no states specified");

                
                var context = new WorkflowContext(Id, sm.Name, sm.InitialState, _workflowDataRepositoryKey);
                var machine = new StateMachine<string, string>(context.AccessState, context.ChangeState);
                

                foreach (var st in states)
                {


                    if (string.IsNullOrWhiteSpace(st.Name))
                        throw new WorkflowTemplateException("state name unspecified");

                    var stateSpecifier = machine.Specify(st.Name);
                    if (!string.IsNullOrEmpty(st.Parent))
                        stateSpecifier.SubstateOf(st.Parent);

                    var entryActions = st.EntryActions.EmptyIfNull();
                    var exitActions = st.ExitActions.EmptyIfNull();
                    var retry = st.RetryTemplate;
                    var transitions = st.Transitions.EmptyIfNull();
                    StateMachineTemplate machineTemplate = sm;


                    foreach (var entryAction in entryActions)
                    {
                        EntryActionTemplate action = entryAction;

                        ValidateWork(entryAction.Worker, entryAction.Route);

                        if (!string.IsNullOrEmpty(entryAction.From))
                        {
                           
                            stateSpecifier.OnEntryFrom(entryAction.From,
                                () =>
                                {
                                    try
                                    {
                                        RouteAction(action.Worker, action.Route);
                                    }
                                    catch (Exception ex)
                                    {
                                        var es = string.Format("exception during statemachine action {0}, {1}", action.Route, ex.Message);
                                        _log.Error(es);
                                        ex.TraceInformation();
                                        var on = Catalog.Factory.Resolve<IApplicationAlert>();
                                        on.RaiseAlert(ApplicationAlertKind.System, es);


                                        if (!string.IsNullOrEmpty(action.ExceptionTrigger))
                                        {
                                            StateMachines[machineTemplate.Name].Fire(action.ExceptionTrigger);
                                        }
                                    }

                                });
                        }
                        else
                        {
                            
                            
                            stateSpecifier.OnEntry(() =>
                                                       {
                                                           try
                                                           {
                                                               RouteAction(action.Worker, action.Route);
                                                           }
                                                           catch (Exception ex)
                                                           {
                                                               var es = string.Format("exception during statemachine action {0}, {1}", action.Route, ex.Message);
                                                               _log.Error(es);
                                                               ex.TraceInformation();
                                                               var on = Catalog.Factory.Resolve<IApplicationAlert>();
                                                               on.RaiseAlert(ApplicationAlertKind.System, es);


                                                               if (!string.IsNullOrEmpty(action.ExceptionTrigger))
                                                               {
                                                                   StateMachines[machineTemplate.Name].Fire(action.ExceptionTrigger);
                                                               }
                                                           }

                                                       });
                        }


                        if (!string.IsNullOrEmpty(entryAction.AutoNextTrigger))
                        {
                            
                      
                            stateSpecifier.OnEntry(() => StateMachines[machineTemplate.Name].Fire(action.AutoNextTrigger));
                        }
                    }

                    foreach (var exitAction in exitActions)
                    {


                        ValidateWork(exitAction.Worker, exitAction.Route);

                        ExitActionTemplate action = exitAction;

                        stateSpecifier.OnExit(() =>
                                                  {
                                                      try
                                                      {
                                                          RouteAction(action.Worker, action.Route);
                                                      }
                                                      catch (Exception ex)
                                                      {
                                                          var es = string.Format("exception during statemachine action {0}, {1}", action.Route, ex.Message);
                                                          _log.Error(es);
                                                          ex.TraceInformation();
                                                          var on = Catalog.Factory.Resolve<IApplicationAlert>();
                                                          on.RaiseAlert(ApplicationAlertKind.System, es);


                                                          if (!string.IsNullOrEmpty(action.ExceptionTrigger))
                                                          {
                                                              StateMachines[machineTemplate.Name].Fire(action.ExceptionTrigger);
                                                          }
                                                      }

                                                  }
                            );

                        if (!string.IsNullOrEmpty(exitAction.AutoNextTrigger))
                        {

                            
                            stateSpecifier.OnExit(() => StateMachines[machineTemplate.Name].Fire(action.AutoNextTrigger));
                        }
                    }

                    foreach (var trans in transitions)
                    {
                        var transition = trans;

                        var dynamicNextWorker = (transition.DynamicNextTemplate == null)
                                                    ? null
                                                    : transition.DynamicNextTemplate.Worker;
                        var dynamicNextRoute = (transition.DynamicNextTemplate == null)
                                                   ? null
                                                   : transition.DynamicNextTemplate.Route;


                        var guardworker = (transition.ConditionTemplate == null)
                                              ? null
                                              : transition.ConditionTemplate.Worker;

                        var guardroute = (transition.ConditionTemplate == null)
                                             ? null
                                             : transition.ConditionTemplate.Route;

                        if (null != guardworker)
                            ValidateWork(guardworker, guardroute);

                        if (null != dynamicNextRoute && null != dynamicNextWorker)
                        {
                            ValidateWork(dynamicNextWorker, dynamicNextRoute);
                            

                            if (null != guardworker)
                            {
                                ValidateWork(guardworker, guardroute);

                                stateSpecifier.PermitIf(
                                    transition.Trigger,
                                    () =>
                                    RouteDecision(dynamicNextWorker, dynamicNextRoute, transition.Trigger,
                                                  StateMachines[machineTemplate.Name].State),
                                    () => RouteCondition(guardworker, guardroute, StateMachines[machineTemplate.Name].State, transition.Trigger));
                            }
                            else
                            {
                                stateSpecifier.Permit(
                                    transition.Trigger,
                                    () =>
                                    RouteDecision(dynamicNextWorker, dynamicNextRoute, transition.Trigger,
                                                  StateMachines[machineTemplate.Name].State));
                            }
                        }
                        else
                        {
                            if (transition.Next == st.Name)
                            {
                                if (null != guardworker)
                                {
                                    stateSpecifier.PermitReentryIf(transition.Trigger,
                                                                   () =>
                                                                   RouteCondition(guardworker, guardroute,
                                                                              StateMachines[machineTemplate.Name].State, transition.Trigger));
                                }
                                else
                                {
                                    stateSpecifier.PermitReentry(transition.Trigger);
                                }
                            }
                            else if (transition.Next == "sys.ignore")
                            {
                                if (null != guardworker)
                                {
                                    stateSpecifier.IgnoreIf(transition.Trigger,
                                                            () =>
                                                            RouteCondition(guardworker, guardroute,
                                                                       StateMachines[machineTemplate.Name].State, transition.Trigger));
                                }
                                else
                                {
                                    stateSpecifier.Ignore(transition.Trigger);
                                }
                            }
                            else
                            {
                                if (null != guardworker)
                                {
                                    stateSpecifier.PermitIf(transition.Trigger, transition.Next,
                                                            () =>
                                                            RouteCondition(guardworker, guardroute,
                                                                       StateMachines[machineTemplate.Name].State, transition.Trigger));
                                }
                                else
                                {
                                    stateSpecifier.Permit(transition.Trigger, transition.Next);
                                }
                            }
                        }
                    }


                    if (null != retry)
                    {
                        var retryName = st.Name + "_retry";
                        stateSpecifier.Permit(CommonTriggers.StateRetryTrigger, retryName);

                        var retryConfig = machine.Specify(retryName);
                        if (null != retry.RecoveryAction)
                        {
                            ValidateWork(retry.RecoveryAction.Worker, retry.RecoveryAction.Route);
                            retryConfig.OnEntry(
                                () => RouteAction(retry.RecoveryAction.Worker, retry.RecoveryAction.Route));
                        }

                        retryConfig
                            .OnEntry(
                                () => ScheduleRetry(machineTemplate.Name, retry.Count, retry.Minimum, retry.Maximum, retry.Delta, retry.Sleep))
                            .Permit(CommonTriggers.StateRetryTrigger, st.Name)
                            .Permit(CommonTriggers.StateFailTrigger, retry.FailState);
                    }
                }

                var machineName = sm.Name;
                machine.OnUnhandledTrigger((state, trigger) => UnhandledTrigger(machineName, state, trigger));

                StateMachines.Add(sm.Name, new WorkflowStateMachine(context, machine, sm.ActivationTrigger));
            }

            if (null != template.Workspace)
            {
                if (template.Workspace.KeyAliases.EmptyIfNull().Any())
                {
                    try
                    {
                        if (!_wfLock.Wait(TimeSpan.FromSeconds(300.0)))
                            throw new SynchronizationLockException();

                        Workspace.RegisterKeyOverloads(template.Workspace.KeyAliases.Select(ka => Tuple.Create(ka.AliasKey, ka.Key)).ToArray());


                    }
                    catch (SynchronizationLockException)
                    {
                        var es = string.Format("Timed out waiting to write workspace data to {0}", Id);
                        _log.Error(es);
                        var on = Catalog.Factory.Resolve<IApplicationAlert>();
                        on.RaiseAlert(ApplicationAlertKind.System, es);
                    }
                    finally
                    {

                        _wfLock.Release();
                    }
                }

                _inputDefinitions = template.Workspace.Inputs;
            }


        }


        private void ScheduleRetry(string machine, int retryCount, int minMinutes, int maxMinutes, int delta, bool retrySleep)
        {
            int currentRetryCount = 0;

            try
            {
                if(!_wfLock.Wait(TimeSpan.FromSeconds(60.0)))
                    throw new SynchronizationLockException();
                Workspace.Batch(wsd =>
                {
                    currentRetryCount = wsd.Get(WorkflowWorkspace.CurrentRetryCount, 1);
                    currentRetryCount++;
                });
            }
            catch (SynchronizationLockException)
            {
                var es = string.Format("Timed out waiting to write workspace data to {0}", Id);
                _log.Error(es);
                var on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);
            }
            finally
            {

                _wfLock.Release();
            }
            


            if (currentRetryCount < retryCount)
            {
                var rand = GoodSeedRandom.Create();
                var incrementSeconds = (int) ((Math.Pow(2, currentRetryCount) - 1)*rand.Next((int) (delta*60*0.8),
                                                                                             (int) (delta*60*1.2)));
                var timeToSleepSec = Math.Min((minMinutes*60) + incrementSeconds, (maxMinutes*60));
                var retryInterval = TimeSpan.FromSeconds(timeToSleepSec);
                var retrySchedule = DateTime.UtcNow + retryInterval;

                var js = Catalog.Factory.Resolve<IJobScheduler>();
                js.ScheduleJob(
                    new WorkflowStateMachineRetry
                        {
                            Id = Id,
                            Machine = machine
                        },
                    retrySchedule,
                    null,
                    WorkflowShared.WorkflowJobsRoute);

                try
                {
                    if(!_wfLock.Wait(TimeSpan.FromSeconds(60.0)))
                        throw new SynchronizationLockException();
                    Workspace.Batch(wsd => wsd.Put(WorkflowWorkspace.CurrentRetryCount, currentRetryCount));
                }catch (SynchronizationLockException)
                {
                    var es = string.Format("Timed out waiting to write workspace data to {0}", Id);
                    _log.Error(es);
                    var on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.System, es);
                }
                finally
                {
                    _wfLock.Release();
                }

                if (retrySleep)
                    Sleep();
            }
            else
            {
                StateMachines[machine].Fire(CommonTriggers.StateFailTrigger);
            }
        }


        

        public static WorkflowAgent AcquireSleepingOnLocked(IDistributedMutex wfLock, WorkflowHost host, string id,
                                                            CancellationToken ct, string templateData, string wfdrk, string wfmk, string wfwsk)
        {
            return new WorkflowAgent(host, id, ct, wfLock, templateData, wfdrk, wfmk, wfwsk);
        }

        public void Start()
        {
            Task.Factory.StartNew(RunAgent, TaskCreationOptions.LongRunning);
            StateMachines.Values.ForEach(v => v.Activate());
        }

        public void Stop()
        {
            Sleep();
        }

        public void Bump()
        {
            _bumpEvent.Set();
        }

        private void RunAgent()
        {
            var autoSleepTimeout = DateTime.UtcNow + _initialSleepTimeout;

            var instanceTriggersQuery = new QuerySpecification
                                            {
                                                BookMark = new GenericPageBookmark { PageSize = 1000 },
                                                Where = new Filter
                                                            {
                                                                Rules = new Comparison[]
                                                                            {
                                                                                new Comparison
                                                                                    {
                                                                                        Data = Id,
                                                                                        Field = "InstanceTarget",
                                                                                        Test = Test.Equal
                                                                                    }
                                                                            }
                                                            }
                                            };

            while (_runAgent && !_ct.IsCancellationRequested)
            {
                try
                {
                    // scan triggers
                    IEnumerable<WorkflowTrigger> instTrigs = Enumerable.Empty<WorkflowTrigger>();
                    instanceTriggersQuery.BookMark = new GenericPageBookmark(new GenericPageBookmark { PageSize = 1000});
                    do
                    {
                        var qr = _triggers.Query(instanceTriggersQuery);
                        if (qr.Items.Any())
                            instTrigs = instTrigs.Concat(qr.Items);
                        instanceTriggersQuery.BookMark = qr.Bookmark;
                    } while (instanceTriggersQuery.BookMark.More);

                    var myTrigs = instTrigs.ToArray().GroupBy(t => t.Route);

                    _ct.ThrowIfCancellationRequested();

                    if (myTrigs.Any())
                    {
                        var fireTrigs = myTrigs.Where(t => t.Key == TriggerRoutes.FireRoute);
                        foreach (var t in fireTrigs.SelectMany())
                        {
                            try
                            {
                                StateMachines[t.MachineContext].Fire(t.TriggerName);
                            }
                            catch (Exception ex)
                            {
                                _log.Error(ex.TraceInformation());
                            }

                            _triggers.IdDelete(t.Id);

                            


                            _ct.ThrowIfCancellationRequested();
                        }

                        var completeTrigs = myTrigs.Where(t => t.Key == TriggerRoutes.EndRoute);
                        if (completeTrigs.Any())
                        {
                            CompleteWorkflow();

                            _triggers.DeleteBatch(myTrigs.SelectMany().ToList());

                            

                            break;
                        }
                        else
                        {
                            var napTrigs = myTrigs.Where(t => t.Key == TriggerRoutes.NapRoute);
                            if (napTrigs.Any())
                            {
                                _triggers.DeleteBatch(napTrigs.SelectMany().ToList());

                                

                                if (StateMachines.Values.Any(sm => sm.CanActivate))
                                {
                                    // TODO: nap time variable
                                    var nextActivationTime = DateTime.UtcNow + TimeSpan.FromMinutes(60.0);
                                        
                                    Sleep(nextActivationTime);
                                }
                                else
                                {
                                    // no activation triggers, so this sleep would be until triggered
                                    Sleep();
                                }

                                break;
                            }
                        }

                        autoSleepTimeout = DateTime.UtcNow + _autoSleepTime;
                    }
                    
                    _bumpEvent.Wait(TimeSpan.FromMinutes(5.0)); 
                    _bumpEvent.Reset();


                    // auto sleep?
                    if (DateTime.UtcNow > autoSleepTimeout)
                    {
                        //Sleep();
                    }
                    
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    var es = string.Format("Workflow agent exception: {0}", ex);
                    _log.Error(es);
                    var on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.System, es);
                }
            }
        }

        private void Sleep(DateTime? nextActivation = null)
        {
            _log.InfoFormat("Workflow {0} sleeping until {1}", Id,
                            nextActivation.HasValue ? nextActivation.Value.ToString() : "waked");

            DateTime na = nextActivation ?? DateTime.MaxValue;

            var instance = new WorkflowInstanceInfo
                               {
                                   Id = Id,
                                   TemplateName = Name,
                                   LastActivity = DateTime.UtcNow,
                                   NextActivationTime = na,
                                   Status = WorkflowStatus.Sleeping.ToString(),
                               };


            _instanceData.Store(instance);
           


            _runAgent = false;

            // schedule next activation if applicable
            if (na != DateTime.MaxValue)
            {
                var js = Catalog.Factory.Resolve<IJobScheduler>();
                var waker = new WakeWorkflowJob
                                {
                                    Id = Id
                                };

                js.ScheduleJob(waker, na, null, WorkflowShared.WorkflowJobsRoute);
            }


            _host.DeHost(Id);
            
        }

        public void CompleteWorkflow()
        {
            _runAgent = false;


            var instance = new WorkflowInstanceInfo
                               {
                                   Id = Id,
                                   Status = WorkflowStatus.Complete.ToString(),
                                   NextActivationTime = DateTime.MaxValue,
                                   LastActivity = DateTime.UtcNow,
                                   TemplateName = Name
                               };

            _instanceData.Store(instance);

           

            _host.DeHost(Id);

            _wfLock.Release();

            _log.InfoFormat("Workflow {0} completed, released", Id);
        }
    }


    public class WakeWorkflowJob
    {
        public string Id { get; set; }
    }


    public class WorkflowTemplateException : Exception
    {
        public WorkflowTemplateException()
        {
        }

        public WorkflowTemplateException(string msg)
            : base(msg)
        {
        }
    }
}