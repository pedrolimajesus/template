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
using AppComponents.Data;
using AppComponents.Extensions.EnumEx;
using AppComponents.Extensions.EnumerableEx;
using Newtonsoft.Json;


namespace AppComponents.Workflow
{
    public class WorkflowCatalog : IWorkflowCatalog
    {
    
       
        private readonly IMessagePublisher _sender;
        

        private IDataRepositoryService<WorkflowInstanceInfo,
                                        WorkflowInstanceInfo,
                                        DataEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                        NoMetadata,
                                        DatumEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                        NoMetadata> _instanceData;

        

        private string _workspaceKey;
        private IConfig _wfConfig;

        public WorkflowCatalog()
        {
            _wfConfig = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var host = WorkflowShared.GetMessageQueueHost(_wfConfig);
            WorkflowShared.DeclareWorkflowExchanges(host, _wfConfig[WorkflowConfiguration.WorkflowMessagingKey]);



            _instanceData = Catalog.Preconfigure().ConfigureWorkflowDataRepository<WorkflowInstanceInfo>()
                                            .ConfiguredResolve<IDataRepositoryService<WorkflowInstanceInfo,
                                                WorkflowInstanceInfo,
                                                DataEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                                NoMetadata,
                                                DatumEnvelope<WorkflowInstanceInfo, NoMetadata>,
                                                NoMetadata>>
                                                (_wfConfig[WorkflowConfiguration.WorkflowDataRepositoryKey]);

            

            _sender = Catalog.Preconfigure()
                .Add(MessagePublisherLocalConfig.HostConnectionString, host)
                .Add(MessagePublisherLocalConfig.ExchangeName, WorkflowShared.WorkflowExchange)
                .ConfiguredResolve<IMessagePublisher>(_wfConfig[WorkflowConfiguration.WorkflowMessagingKey]);
           
        }

        #region IWorkflowCatalog Members

        public IWorkflow OpenInstance(string id)
        {
            return Catalog.Preconfigure((ConstructConfiguration) _wfConfig)
                .ConfiguredCreate<IWorkflow>(() => new Workflow(id));
        }


        public string CreateInstance<T>(T initialData, string templateId) where T : class
        {
            return CreateInstance(JsonConvert.SerializeObject(initialData), templateId);
        }

        public string CreateInstance(string initialData, string templateId)
        {
            var wfc = new WorkflowCreate
                          {
                              Id = Guid.NewGuid().ToString(),
                              TemplateName = templateId,
                              InitialData = initialData
                          };

            _sender.Send(wfc, WorkflowShared.WorkflowLoadBalanceRoute);
            
            return wfc.Id;
        }

        public string CreateInstanceForDefinition(string initialData, string jsonTemplateContent)
        {
            var wfc = new WorkflowCreate
                          {
                              Id = Guid.NewGuid().ToString(),
                              TemplateContent = jsonTemplateContent,
                              InitialData = initialData
                          };


            _sender.Send(wfc, WorkflowShared.WorkflowLoadBalanceRoute);
            return wfc.Id;
        }

        public IEnumerable<WorkflowInstanceInfo> GetInstances(WorkflowStatus? filter = null)
        {

            if (null == filter)
            {
                
                var queryResult = _instanceData.All();
                return queryResult.Items.EmptyIfNull();
            }
            else
            {
                var qs = new QuerySpecification
                             {
                                 BookMark = new GenericPageBookmark { PageSize = 1000 },
                                 Where = new Filter
                                             {
                                                 PredicateJoin = PredicateJoin.And,
                                                 Rules = new Comparison[]
                                                             {
                                                                 new Comparison
                                                                     {
                                                                         Data = filter.Value.EnumName(),
                                                                         Field = "Status",
                                                                         Test = Test.Equal
                                                                     }

                                                             }
                                             }
                             };
                var queryResult = _instanceData.Query(qs);
                return queryResult.Items.EmptyIfNull();

            }
            
        }


        public string CreateInstanceForDefinition<T>(T initialData, string jsonTemplateContent) where T : class
        {
            return CreateInstanceForDefinition(JsonConvert.SerializeObject(initialData), jsonTemplateContent);
        }

        #endregion
    }
}