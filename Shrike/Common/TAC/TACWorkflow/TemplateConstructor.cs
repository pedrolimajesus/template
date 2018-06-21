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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace AppComponents.Workflow
{
    /// <summary>
    /// Describes a plugin used by a workflow instance
    /// </summary>
    public class PluginTemplate
    {
        /// <summary>
        /// Name of the plugin host
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Instancename { get; set; }

        /// <summary>
        /// Uri route to the plugin to be used
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Route { get; set; }
    }

    /// <summary>
    /// Describes an action to invoke when entering a state
    /// </summary>
    public class EntryActionTemplate
    {
        /// <summary>
        /// If given, the state coming from 
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Which plugin worker to invoke
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Worker { get; set; }

        /// <summary>
        /// Uri route to the method
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Route { get; set; }

        /// <summary>
        /// If provided, automatically fires this trigger when the entry
        /// action completes.
        /// </summary>
        public string AutoNextTrigger { get; set; }

        /// <summary>
        /// If provided, a trigger to fire in case of exception thrown
        /// </summary>
        public string ExceptionTrigger { get; set; }
    }

    /// <summary>
    /// Describes an action to invoke when exiting a state
    /// </summary>
    public class ExitActionTemplate
    {
        /// <summary>
        /// Which plugin worker to invoke
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Worker { get; set; }

        /// <summary>
        /// Uri route to the method
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Route { get; set; }

        /// <summary>
        /// If provided, automatically fires this trigger when the entry
        /// action completes.
        /// </summary>
        public string AutoNextTrigger { get; set; }

        /// <summary>
        /// If provided, a trigger to fire in case of exception thrown
        /// </summary>
        public string ExceptionTrigger { get; set; }
    }

    /// <summary>
    /// Describes a query for which state to transit to
    /// </summary>
    public class DynamicNextTemplate
    {
        /// <summary>
        /// Which plugin worker to invoke
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Worker { get; set; }

        /// <summary>
        /// the route uri to the method
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Route { get; set; }
    }

    /// <summary>
    /// Describes a condition that blocks or allows a state transition
    /// </summary>
    public class ConditionTemplate
    {
        /// <summary>
        /// Which plugin worker to invoke
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Worker { get; set; }

        /// <summary>
        /// the route uri to the method needed
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Route { get; set; }
    }

    /// <summary>
    /// Describes a state transition from one state to another
    /// </summary>
    public class Transition
    {
        /// <summary>
        /// Name of the trigger that causes the transition
        /// </summary>
        public string Trigger { get; set; }

        /// <summary>
        /// Name of the state to go to
        /// </summary>
        public string Next { get; set; }

        

        /// <summary>
        /// If provided, dynamically determine next state by calling this plugin
        /// </summary>
        public DynamicNextTemplate DynamicNextTemplate { get; set; }

        /// <summary>
        /// If provided, check this condition before effecting the transition.
        /// </summary>
        public ConditionTemplate ConditionTemplate { get; set; }
    }

    /// <summary>
    /// Describes retry behavior for a state if the
    /// WorkflowShared.StateRetry trigger is invoked
    /// </summary>
    public class RetryTemplate
    {
        /// <summary>
        /// The state to go to if the WorkflowShared.StateFailTrigger 
        /// is invoked, or if retry count is used up
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string FailState { get; set; }

        /// <summary>
        /// Maximum times to retry
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int Count { get; set; }

        /// <summary>
        /// minimum number of minutes to wait until retry
        /// </summary>
        public int Minimum { get; set; }

        /// <summary>
        /// maximum number of minutes to wait until retry
        /// </summary>
        public int Maximum { get; set; }

        /// <summary>
        /// exponential back off factor
        /// </summary>
        public int Delta { get; set; }

        /// <summary>
        /// true or false, whether or not to put the workflow instance to sleep
        /// during retry
        /// </summary>
        public bool Sleep { get; set; }

        /// <summary>
        /// If set, represents an action to run on failure to 
        /// attempt recovery before retry
        /// </summary>
        public EntryActionTemplate RecoveryAction { get; set; }
    }

    /// <summary>
    /// Describes a state that is embedded in a state machine.
    /// </summary>
    public class StateTemplate
    {
        public StateTemplate()
        {
            EntryActions = new List<EntryActionTemplate>();
            ExitActions = new List<ExitActionTemplate>();
            Transitions = new List<Transition>();
        }

        /// <summary>
        /// State name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Parent state machine.
        /// </summary>
        public string Parent { get; set; }

        /// <summary>
        /// Actions to run on entry into this state
        /// </summary>
        public List<EntryActionTemplate> EntryActions { get; set; }

        /// <summary>
        /// Actions to run on exit from this state
        /// </summary>
        public List<ExitActionTemplate> ExitActions { get; set; }
        
        /// <summary>
        /// State change transitions
        /// </summary>
        public List<Transition> Transitions { get; set; }


        /// <summary>
        /// If given, the retry behavior for this state, reached through
        /// the WorkflowShared.StateRetry Trigger
        /// </summary>
        public RetryTemplate RetryTemplate { get; set; }
    }

    /// <summary>
    /// Describes a state machine.
    /// </summary>
    public class StateMachineTemplate
    {
        public StateMachineTemplate()
        {
            States = new List<StateTemplate>();
        }

        /// <summary>
        /// Name of the state machine
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// State to begin in.
        /// </summary>
        public string InitialState { get; set; }

        /// <summary>
        /// Trigger to call on the state machine when it is first activated.
        /// </summary>
        public string ActivationTrigger { get; set; }

        /// <summary>
        /// States for the statemachine
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<StateTemplate> States { get; set; }
    }

    /// <summary>
    /// Describes a workspace key alias, so that the 
    /// same workspace datum may be accessed by
    /// many keys.
    /// </summary>
    public class Alias
    {
        [JsonProperty(Required = Required.Always)]
        public string AliasKey { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Key { get; set; }
    }

    /// <summary>
    /// Describes an expected workspace value 
    /// when a workflow instance is started.
    /// </summary>
    public class InputDefinition
    {
        [JsonProperty(Required = Required.Always)]
        public string WorkspaceKey { get; set; }


        public string Type { get; set; }

       
        public string Description { get; set; }


        public bool Optional { get; set; }
    }

    /// <summary>
    /// Describes workspace metadata for the workflow.
    /// </summary>
    public class Workspace
    {

        public Workspace() 
        {
            KeyAliases = new List<Alias>();
            Inputs = new List<InputDefinition>();
        }

        /// <summary>
        /// Aliases for workspace keys throughout the lifetime of the workflow instance.
        /// </summary>
      
        public List<Alias> KeyAliases { get; set; }

        /// <summary>
        /// Defines metadata for expected workspace values when the workflow
        /// instance is started.
        /// </summary>

        public List<InputDefinition> Inputs { get; set; } 
        
    }

    /// <summary>
    /// Template used for producing workflow instances.
    /// </summary>
    public class WorkflowTemplate
    {
        public WorkflowTemplate()
        {
            Plugins = new List<PluginTemplate>();
            StateMachines = new List<StateMachineTemplate>();
        }

        /// <summary>
        /// Workspace metadata definition.
        /// </summary>

        public Workspace Workspace { get; set; }

        /// <summary>
        /// Name of the workflow instance 
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// template version.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Version { get; set; }

        /// <summary>
        /// Which plugins are to be loaded for use with state entry and exit actions
        /// and conditional state checking
        /// </summary>
        public List<PluginTemplate> Plugins { get; set; }

        /// <summary>
        /// State machine specifications for this workflow template
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<StateMachineTemplate> StateMachines { get; set; }

        /// <summary>
        /// For any triggers that are unhandled, the name of the statemachine to receive
        /// the trigger
        /// </summary>
        public string Fallthrough { get; set; }

        /// <summary>
        /// How long between triggers before the instance is put to sleep and releases
        /// host threads and resources.
        /// </summary>
        public int AutoSleepSeconds { get; set; }


        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static WorkflowTemplate FromJson(string json)
        {
            return JsonConvert.DeserializeObject<WorkflowTemplate>(json);
        }


        
    }
}