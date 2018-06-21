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
using System.Linq;
using AppComponents.Data;
using AppComponents.Extensions.EnumerableEx;
using log4net;

namespace AppComponents.Workflow
{
    public class WorkflowContext
    {
        private string _cachedId;
        private string _cachedState;
        private string _machineContext;
        private string _persistenceContext;

        private IDataRepositoryService<WorkflowMachineState,
                                        WorkflowMachineState,
                                        DataEnvelope<WorkflowMachineState, NoMetadata>,
                                        NoMetadata,
                                        DatumEnvelope<WorkflowMachineState, NoMetadata>,
                                        NoMetadata> _machineStateData;


        public WorkflowContext(string persistenceContext, string machineContext, string initialState, string repositoryKey)
        {
            _persistenceContext = persistenceContext;
            _machineContext = machineContext;
            _cachedState = initialState;

            _machineStateData = Catalog.Preconfigure().ConfigureWorkflowDataRepository<WorkflowMachineState>()
                .ConfiguredResolve<IDataRepositoryService<WorkflowMachineState,
                                                WorkflowMachineState,
                                                DataEnvelope<WorkflowMachineState, NoMetadata>,
                                                NoMetadata,
                                                DatumEnvelope<WorkflowMachineState, NoMetadata>,
                                                NoMetadata>>
                                                (repositoryKey);

            WorkflowMachineState current = null;

            var qs = new QuerySpecification
                         {
                             BookMark = new GenericPageBookmark {PageSize = 100},
                             Where = new Filter
                                         {
                                             PredicateJoin = PredicateJoin.And,
                                             Rules = new Comparison[]
                                                         {
                                                             new Comparison
                                                                 {
                                                                     Data = _persistenceContext,
                                                                     Field = "Parent",
                                                                     Test = Test.Equal
                                                                 },
                                                             new Comparison
                                                                 {
                                                                     Data = _machineContext,
                                                                     Field = "StateMachine",
                                                                     Test = Test.Equal
                                                                 }
                                                         }
                                         }
                         };

            var queryResult = _machineStateData.Query(qs).Items.EmptyIfNull();
            current = queryResult.FirstOrDefault();

            if (null != current)
            {
                _cachedState = current.State;
                _cachedId = current.Id;
            }
            else
            {
                _cachedId = string.Format("MS{0}-{1}", _persistenceContext, _machineContext);
                ChangeState(_cachedState);
            }
        }

        public string AccessState()
        {
            // we can assume the cached state is correct,
            // since the workflow instance is locked
            // by the workflow host that is the owner of this
            // object
            return _cachedState;
        }

        public void ChangeState(string st)
        {
            _cachedState = st;
            var update = new WorkflowMachineState
                             {
                                 Id = _cachedId,
                                 Parent = _persistenceContext,
                                 StateMachine = _machineContext,
                                 State = _cachedState,
                                 LastStateChanged = DateTime.UtcNow
                             };

            _machineStateData.Store(update);

            
        }
    }

    public class WorkflowStateMachine
    {
        private string _activationTrigger;
        private WorkflowContext _ctx;
        private ILog _log;
        private StateMachine<string, string> _machine;

        public WorkflowStateMachine(WorkflowContext ctx, StateMachine<string, string> inner, string activationTrigger)
        {
            _ctx = ctx;
            _machine = inner;
            _activationTrigger = activationTrigger;
            _log = ClassLogger.Create(GetType());
        }

        public bool CanActivate
        {
            get { return null != _activationTrigger; }
        }

        public string State
        {
            get { return _machine.State; }
        }

        public void Activate()
        {
            if (CanActivate)
            {
                _log.InfoFormat("Activating with {0}", _activationTrigger);
                _machine.Fire(_activationTrigger);
            }
        }


        public void Fire(Enum triggerId)
        {
            _machine.Fire(triggerId.ToString());
        }

        public void Fire(Enum triggerId, string msg)
        {
            Fire(triggerId.ToString(), msg);
        }

        public void Fire(string triggerId, string msg)
        {
            var triggerModel = _machine.SetTriggerParameters<string>(triggerId);
            _machine.Fire(triggerModel, msg);
        }

        public void Fire(string triggerId)
        {
            _machine.Fire(triggerId);
        }

        public EnumState GetState<EnumState>()
        {
            return (EnumState) Enum.Parse(typeof (EnumState), _machine.State);
        }

        public IEnumerable<string> SeekGoal(Func<string, bool> foundGoalState)
        {
            throw new NotImplementedException();
        }
    }
}