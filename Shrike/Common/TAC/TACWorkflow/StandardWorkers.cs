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
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using AppComponents.Extensions.EnumerableEx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using PluginContracts;

namespace AppComponents.Workflow
{
    /// <summary>
    /// A job task for triggering a workflow,
    /// for use with scheduled jobs.
    /// </summary>
    public class FireWorkflowTriggerJob
    {
        public string Instance { get; set; }
        public string Context { get; set; }
        public string Trigger { get; set; }
    }


    [Export(typeof (IWorkflowPlugin))]
    [ExportMetadata("Route", "sys")]
    public class SystemWorkflowPlugin : PluginBase
    {
        protected override IEnumerable<WorkerEntry> ProvideWorkers()
        {

            yield return new WorkerEntry(
                WorkerBase.RouteFor<IntraTalk>(),
                (id, host, factoryRoute) => new IntraTalk(id, host, factoryRoute));
        }
    }

    /// <summary>
    /// Plugin that provides means for statemachines to trigger each other or check 
    /// each others' states within the same workflow instance.
    /// </summary>
    [WorkerRoute]
    internal class IntraTalk : WorkerBase
    {
        public IntraTalk(string id, IWorkflowPluginHost host, string factoryRoute)
            : base(id, host, factoryRoute)
        {
        }

        /// <summary>
        /// Action for invoking a trigger on a given state machine.
        /// </summary>
        /// <param name="contextId"></param>
        /// <param name="route"></param>
        [ExposeRoute(RouteKinds.Invoke)]
        private void FireOn(string contextId, string route)
        {
            var args = ExtractValueArguments(route, FireOnRoute.Context, FireOnRoute.Trigger);

            _host.FireOnHostedWorkflow(contextId, args[FireLaterRoute.Context], args[FireLaterRoute.Trigger]);
        }


        
        /// <summary>
        /// Action for invoking a trigger on a given statemachine, later. 
        /// Route must have context, trigger, and seconds.
        /// </summary>
        /// <param name="contextId"></param>
        /// <param name="route"></param>

        [ExposeRoute(RouteKinds.Invoke)]
        private void FireLaterOn(string contextId, string route)
        {
            var args = ExtractValueArguments(route, FireLaterRoute.Context, FireLaterRoute.Trigger,
                                             FireLaterRoute.ElapsedSeconds);
            TimeSpan elapsedTime = TimeSpan.FromSeconds(int.Parse(args[FireLaterRoute.ElapsedSeconds]));
            IJobScheduler js = Catalog.Factory.Resolve<IJobScheduler>();
            js.ScheduleJob(
                new FireWorkflowTriggerJob
                    {
                        Instance = contextId,
                        Context = args[FireLaterRoute.Context],
                        Trigger = args[FireLaterRoute.Trigger]
                    },
                DateTime.UtcNow + elapsedTime,
                null,
                WorkflowShared.WorkflowJobsRoute);
        }

        private enum LoadConfigKeyToWorkspaceRoute
        {
            Key
        }

        /// <summary>
        /// Action for loading a config value into the workflow workspace
        /// Route must have key argument.
        /// </summary>
        /// <param name="contextId"></param>
        /// <param name="route"></param>
        /// <param name="triggerMsg"></param>
        [ExposeRoute(RouteKinds.Invoke)]
        private void LoadConfigKeyToWorkspace(string contextId, string route)
        {
            var args = ExtractValueArguments(route, LoadConfigKeyToWorkspaceRoute.Key);
            var key = args[LoadConfigKeyToWorkspaceRoute.Key];
            var cf =
                Catalog.Factory.CanResolve<IConfig>(WorkflowConfiguration.OptionalWorkflowSubstituteConfigKey) 
                ? Catalog.Factory.Resolve<IConfig>(WorkflowConfiguration.OptionalWorkflowSubstituteConfigKey) 
                : Catalog.Factory.Resolve<IConfig>();

            var val = cf[key];
            _host.WriteWorkspaceDataStrings(contextId, EnumerableEx.OfOne<KeyValuePair<string, string>>(new KeyValuePair<string, string>(key, val)));
        }


        private enum LoadNextItemRoute
        {
            CollectionKey,
            ItemKey,
            MachineContext,
            LoadedTrigger,
            NoMoreTrigger
        }

        /// <summary>
        /// Helps enumerate through a serialized JArray of objects one at a time
        /// The Json array must be found in the workspace at the given CollectionKey. A loaded item will be
        /// stored in the workspace at ItemKey as a json string.
        /// If the item is loaded, the trigger given by LoadedTrigger is fired, otherwise
        /// the trigger given by NoMoreTrigger is fired.
        /// The route requires collection key, item key, loaded trigger, and no more trigger, 
        /// and a Jarray of objects in the workspace.
        /// </summary>
        /// <param name="contextId"></param>
        /// <param name="route"></param>
        /// <param name="triggerMsg"></param>
        [ExposeRoute(RouteKinds.Invoke)]
        private void LoadNextItem(string contextId, string route)
        {
            var args = ExtractValueArguments(route, 
                    LoadNextItemRoute.CollectionKey, LoadNextItemRoute.ItemKey, LoadNextItemRoute.MachineContext, LoadNextItemRoute.LoadedTrigger, LoadNextItemRoute.NoMoreTrigger);

            var collection = _host.ReadWorkspaceDataString(contextId, args[LoadNextItemRoute.CollectionKey]);
            var indexKey = args[LoadNextItemRoute.CollectionKey] + "_index";
            var index = int.Parse(_host.ReadWorkspaceDataString(contextId, indexKey) ?? "0");
            
            if (null != collection)
            {
                var array = JArray.Parse(collection);
                if (index >= array.Count)
                {
                    _host.FireOnHostedWorkflow(contextId, args[LoadNextItemRoute.MachineContext], args[LoadNextItemRoute.NoMoreTrigger]);
                }
                else
                {
                    var item = array[index];
                    index += 1;
                    _host.WriteWorkspaceDataStrings(contextId, 
                        EnumerableEx.OfTwo(new KeyValuePair<string, string>(args[LoadNextItemRoute.ItemKey],item.ToString()),
                                           new KeyValuePair<string, string>(indexKey, index.ToString(CultureInfo.InvariantCulture))
                        ));
                    _host.FireOnHostedWorkflow(contextId, args[LoadNextItemRoute.MachineContext], args[LoadNextItemRoute.LoadedTrigger]);
                }
            }
            else
            {
                _host.FireOnHostedWorkflow(contextId, args[LoadNextItemRoute.MachineContext], args[LoadNextItemRoute.NoMoreTrigger]);
            }


        }


        private enum UnpackJsonPropertiesRoute
        {
            ObjectKey,

        }

        [ExposeRoute(RouteKinds.Invoke)]
        private void UnpackJsonProperties(string contextId, string route)
        {
            var args = ExtractValueArguments(route, UnpackJsonPropertiesRoute.ObjectKey);
            var key = args[UnpackJsonPropertiesRoute.ObjectKey];
            var objJson = _host.ReadWorkspaceDataString(contextId, key);
            var obj = JObject.Parse(objJson);
            var properties = new Dictionary<string, string>();
            var lst = obj.Children<JProperty>();
            foreach (var property in lst)
            {
                properties.Add(key + "." + property.Name, property.Value.ToString());
            }
            _host.WriteWorkspaceDataStrings(contextId, properties);

        }

        #region Nested type: CompareOtherStateRoute

        private enum CompareOtherStateRoute
        {
            Context,
            ForeignState,
            Yes,
            No
        };

        #endregion

        /// <summary>
        /// Conditional for checking the current state to the state of another state machine.
        /// Route must have context, foreign state to check, and the return values for 
        /// if the states match or not, yes and no.
        /// </summary>
        /// <param name="route"></param>
        /// <param name="trigger"></param>
        /// <param name="state"></param>
        /// <param name="triggerMsg"></param>
        /// <returns></returns>
        [ExposeRoute(RouteKinds.Decide)]
        private string CompareOtherState(string context, string route, string trigger, string state)
        {
            var args = ExtractValueArguments(route, CompareOtherStateRoute.Context, CompareOtherStateRoute.ForeignState,
                                             CompareOtherStateRoute.Yes, CompareOtherStateRoute.No);
            var otherState = _host.GetHostedWorkflowState(context, args[CompareOtherStateRoute.Context]);
            if (string.Compare(state, otherState) == 0)
                return args[CompareOtherStateRoute.Yes];
            else
                return args[CompareOtherStateRoute.No];
        }


        


        

        #region Nested type: FireLaterRoute

        private enum FireLaterRoute
        {
            Context,
            Trigger,
            ElapsedSeconds
        };

        #endregion

        #region Nested type: FireOnRoute

        private enum FireOnRoute
        {
            Context,
            Trigger
        };

        #endregion
    }
}