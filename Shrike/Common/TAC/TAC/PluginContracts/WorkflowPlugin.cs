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

namespace PluginContracts
{
    public interface IWorkflowPluginHost
    {
        void FireOnHostedWorkflow(string agentId, string machine, string trigger);
        string GetHostedWorkflowState(string agentId, string machine);
        void EndWorkflow(string agentId);
        void WriteWorkspaceDataStrings(string agentId, IEnumerable<KeyValuePair<string, string>> workspaceData);
        string ReadWorkspaceDataString(string agentId, string key);
    }

    public interface IWorkflowPluginMetadata
    {
        string Route { get; }
    }


    public interface IWorkflowPlugin
    {
        IWorkflowPluginHost Host { get; set; }

        IWorkflowWorker CreateWorkerInstance(string route, string id);
        IEnumerable<string> ListWorkerRoutes();
    }


    [Flags]
    public enum RouteKinds
    {
        None = 0,
        Invoke = 1,
        Decide = 1 << 1,
        Guard = 1 << 2,
        All = Invoke | Decide | Guard
    };

    public interface IWorkflowWorker
    {
        string WorkerRoute { get; }
        IEnumerable<string> ListRoutes(RouteKinds kinds = RouteKinds.All);

        bool SupportsRoute(string route);
        void Invoke(string contextId, string route);
        string DecideTransition(string contextId, string route, string trigger, string state);
        bool Guard(string contextId, string route, string state, string trigger);
    }
}