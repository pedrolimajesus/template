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
using AppComponents.Data;
using AppComponents.Dynamic;
using AppComponents.Dynamic.Lambdas;
using AppComponents.Dynamic.Projection;

namespace AppComponents.Workflow
{
    internal enum WorkflowWorkspace
    {
        InitialData,
        CurrentRetryCount
    }

    internal static class WorkflowShared
    {
        public static readonly string WorkflowPersistedContainerPrefix = "workflowdata";
        public static readonly string WorkflowInstanceLockPrefix = "workflowinstancelock";

        public static readonly string WorkflowJobsRoute = "workflowjobs";


        public static readonly string WorkflowExchange = "WorkflowHosting";
        public static readonly string WorkflowFanoutExchange = "WorkflowHostingBroadcast";
        public static readonly string WorkflowQueuePrefix = "wfq";
        public static readonly string WorkflowLoadBalanceRoute = "WorkflowHostingLoadBalancedWork";

        


        public static string WorkflowInstanceWorkspaceName(string workflowId)
        {
            return WorkflowPersistedContainerPrefix + workflowId;
        }

        public static string WorkflowInstanceLockName(string workflowId)
        {
            return WorkflowInstanceLockPrefix + workflowId;
        }

        private static IMessageBusSpecifier MessageBusSpecifier(string host, string messageKey)
        {
            var specifier = Catalog.Preconfigure()
                .Add(MessageBusSpecifierLocalConfig.HostConnectionString, host)
                .ConfiguredResolve<IMessageBusSpecifier>(messageKey);
            return specifier;
        }

        public static void DeclareWorkflowExchanges(string host, string messageKey)
        {
            var specifier = MessageBusSpecifier(host, messageKey);
            specifier.DeclareExchange(WorkflowExchange, ExchangeTypes.Direct);
            specifier.DeclareExchange(WorkflowFanoutExchange, ExchangeTypes.Fanout);

        }

        public static void AttachQueueToWorkflowExchange(string host, string queue, string messageKey)
        {
            var specifier = MessageBusSpecifier(host, messageKey);
            specifier.SpecifyExchange(WorkflowExchange).DeclareQueue(queue, WorkflowLoadBalanceRoute);
        }

        public static void AttachedQueueToWorkflowBroadcast(string host, string queue, string messageKey)
        {
            var specifier = MessageBusSpecifier(host, messageKey);
            specifier.SpecifyExchange(WorkflowFanoutExchange).DeclareQueue(queue, "_");
        }

        public static void RemoveQueueFromWorkflowExchange(string host, string queue, string messageKey)
        {
            var specifier = MessageBusSpecifier(host, messageKey);
            specifier.SpecifyExchange(WorkflowExchange).DeleteQueue(queue);
        }

        public static void RemoveQueueFromWorkflowBroadcast(string host, string queue, string messageKey)
        {
            var specifier = MessageBusSpecifier(host, messageKey);
            specifier.SpecifyExchange(WorkflowFanoutExchange).DeleteQueue(queue);
        }

        public static string GetMessageQueueHost(IConfig cf)
        {
            var host = cf.Get(WorkflowConfiguration.OptionalWorkflowMessageBusConnection, string.Empty);
            if (string.IsNullOrEmpty(host))
                host = cf[CommonConfiguration.DefaultBusConnection];
            return host;
        }


        public static ConstructConfiguration ConfigureWorkflowDataRepository<T>(this ConstructConfiguration that)
            where T: class, new()
        {
            that.Add(DataRepositoryServiceLocalConfig.SummaryMetadataProvider, new NoMetadataProvider<T>())
                .Add(DataRepositoryServiceLocalConfig.ItemMetadataProvider, new NoMetadataProvider<T>())
                .Add(DataRepositoryServiceLocalConfig.Summarizer, new IdentitySummarizer<T,T>())
                .Add(DataRepositoryServiceLocalConfig.UpdateAssignment, new Updater<T>(t=>t.Properties()));
            return that;
        }

    }
}