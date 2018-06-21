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

namespace AppComponents.Workflow
{

    /// <summary>
    /// Workflow configuration keys
    /// </summary>
    public enum WorkflowConfiguration
    {
        /// <summary>
        /// The workflow host message queue bus configuration key. If the configuration item is 
        /// not provided, CommonConfiguration.DefaultBusConnection will be used.
        /// </summary>
        OptionalWorkflowMessageBusConnection,

        WorkflowDataRepositoryKey,

        WorkflowMessagingKey,

        WorkflowWorkspaceKey,

        OptionalWorkflowSubstituteConfigKey
    }

    /// <summary>
    /// Provides access to a presently running workflow instance in the workflow host farm. A workflow instance
    /// runs one or more state machines as defined by the template that started it.
    /// </summary>
    public interface IWorkflow
    {
        /// <summary>
        /// Identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Status
        /// </summary>
        WorkflowStatus Status { get; }


        /// <summary>
        /// The data workspace for the workflow tracking state
        /// </summary>
        IWorkspace Workspace { get; }

        /// <summary>
        /// Gets the state of the state machine indicated by the context parameter.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        string GetState(string context);

        /// <summary>
        /// Fires a trigger on the given state machine for this workflow instance.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="trigger"></param>
        void Fire(string context, string trigger);

        


        /// <summary>
        /// Ends the workflow instance
        /// </summary>
        void End();

        /// <summary>
        /// Puts the workflow instance to sleep for the specified duration or
        /// until another trigger wakes it up.
        /// </summary>
        /// <param name="ts"></param>
        void Nap(TimeSpan ts);
    }

    /// <summary>
    /// Provides access to create and open workflow instances
    /// </summary>
    public interface IWorkflowCatalog
    {
        /// <summary>
        /// Creates a workflow instance with the given initial data and using the given template id. 
        /// The template is referenced by id in the blob store or in string resources.
        /// Initial data should be a json serialized Dictionary[string,string]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="initialData"></param>
        /// <param name="templateId"></param>
        /// <returns>The id of the created workflow instance.</returns>
        string CreateInstance<T>(T initialData, string templateId) where T : class;
        
        
        /// <summary>
        /// Creates a workflow instance with the given initial data and using the given template id. 
        /// The template is referenced by id in the blob store or in string resources.
        /// Initial data should be a json serialized Dictionary[string,string]
        /// </summary>
        /// <param name="initialData"></param>
        /// <param name="templateId"></param>
        /// <returns>The id of the created workflow instance.</returns>
        string CreateInstance(string initialData, string templateId);

        /// <summary>
        /// Creates a workflow instance using the given initial data and template in json form. 
        /// The WorkflowTemplate class may be used to create the json template data. 
        /// Initial data should be a json serialized Dictionary[string,string]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="initialData"></param>
        /// <param name="jsonTemplateContent"></param>
        /// <returns>The id of the created workflow instance.</returns>
        string CreateInstanceForDefinition<T>(T initialData, string jsonTemplateContent) where T : class;

        /// <summary>
        /// Creates a workflow instance using the given initial data and template in json form. 
        /// The WorkflowTemplate class may be used to create the json template data. 
        /// Initial data should be a json serialized Dictionary[string,string]
        /// </summary>
        /// <param name="initialData"></param>
        /// <param name="jsonTemplateContent"></param>
        /// <returns>The id of the created workflow instance.</returns>
        string CreateInstanceForDefinition(string initialData, string jsonTemplateContent);

        /// <summary>
        /// Opens an existing workflow instance.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IWorkflow OpenInstance(string id);

        /// <summary>
        /// Gets a list of workflow instances. If the filter parameter has a value,
        /// the list will be filtered by the given status value.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        IEnumerable<WorkflowInstanceInfo> GetInstances(WorkflowStatus? filter = null);
    }


    public class InvalidWorkflowInputsException: ApplicationException
    {
        public InvalidWorkflowInputsException()
        {
        }

        public InvalidWorkflowInputsException(string msg): base(msg)
        {
        }
    }
}