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
using System.Globalization;
using System.Linq;
using AppComponents.Data;
using Newtonsoft.Json;

namespace AppComponents.Workflow
{
    public class Workflow : IWorkflow
    {
       
        
        private WorkflowInstanceInfo _info;
        private readonly string _instance;
        private readonly IMessagePublisher _sender;
        private readonly IDataRepositoryService< WorkflowMachineState,
                                        WorkflowMachineState,
                                        DataEnvelope<WorkflowMachineState,NoMetadata>,
                                        NoMetadata,
                                        DatumEnvelope<WorkflowMachineState, NoMetadata>,
                                        NoMetadata> _machineStateData;

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

        private readonly string _workspaceKey;

        
        


        public Workflow(string instance)
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);

            var host = WorkflowShared.GetMessageQueueHost(cf);
            WorkflowShared.DeclareWorkflowExchanges(host, cf[WorkflowConfiguration.WorkflowMessagingKey]);
            _instance = instance;
            

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

            _sender = Catalog.Preconfigure()
                .Add(MessagePublisherLocalConfig.HostConnectionString, host)
                .Add(MessagePublisherLocalConfig.ExchangeName, WorkflowShared.WorkflowFanoutExchange)
                .ConfiguredResolve<IMessagePublisher>(cf[WorkflowConfiguration.WorkflowMessagingKey]);

            _workspaceKey = cf[WorkflowConfiguration.WorkflowWorkspaceKey];

            RefreshInstanceInfo();
        }

        #region IWorkflow Members

        public string Id
        {
            get { return _info.Id; }
        }

        public string GetState(string context)
        {
            

            var qs = new QuerySpecification
                         {
                             BookMark = new GenericPageBookmark {PageSize = 100},
                             Where = new Filter
                                         {
                                             PredicateJoin = PredicateJoin.And,
                                             Rules = new[]
                                                         {
                                                             new Comparison
                                                                 {
                                                                     Data = Id,
                                                                     Field = "Parent",
                                                                     Test = Test.Equal
                                                                 },
                                                             new Comparison
                                                                 {
                                                                     Data = context,
                                                                     Field = "StateMachine",
                                                                     Test = Test.Equal
                                                                 }
                                                         }
                                         }
                         };

            var st = string.Empty;
            var found = _machineStateData.Query(qs).Items;
            if (found.Any())
                st = found.First().State;

            return st;

            
        }

        public WorkflowStatus Status
        {
            get
            {
                RefreshInstanceInfo();
                if(_info == null)
                    return WorkflowStatus.NoInstance;
                
                return (WorkflowStatus) Enum.Parse(typeof (WorkflowStatus), _info.Status);
            }
        }


        public void Fire(string context, string trigger)
        {
            var fire = new WorkflowTrigger
                           {
                               TriggerName = trigger,
                               InstanceTarget = Id,
                               MachineContext = context
                           };

            StoreWorkflowTrigger(fire);

            BroadcastTrigger(fire);
        }

       

        public IWorkspace Workspace
        {
            get
            {
                var workspace = Catalog.Preconfigure()
                    .Add(WorkspaceLocalConfig.WorkspaceName, WorkflowShared.WorkflowInstanceWorkspaceName(Id))
                    .ConfiguredResolve<IWorkspace>(_workspaceKey);

                return workspace;
            }
        }


        public void End()
        {
            var fire = new WorkflowTrigger
                           {
                               Route = TriggerRoutes.EndRoute,
                               InstanceTarget = Id,
                           };

            StoreWorkflowTrigger(fire);
        }

        public void Nap(TimeSpan ts)
        {
            var fire = new WorkflowTrigger
                           {
                               Route = TriggerRoutes.NapRoute,
                               InstanceTarget = Id
                           };

            StoreWorkflowTrigger(fire);
            BroadcastTrigger(fire);
        }

        #endregion

        private void RefreshInstanceInfo()
        {
            _info = _instanceData.Load(_instance).Item;
            


        }

        public void StoreWorkflowTrigger(WorkflowTrigger wft)
        {
            _triggers.CreateNew(wft);

            
        }

        private void BroadcastTrigger(WorkflowTrigger trigger)
        {
            _sender.Send(trigger, string.Empty);
            
        }
    }


    public partial class WorkflowInstanceInfo
    {
        

        public IWorkflow OpenInstance()
        {
            var wfc = Catalog.Factory.Resolve<IWorkflowCatalog>();
            return wfc.OpenInstance(Id);
        }
    }
}