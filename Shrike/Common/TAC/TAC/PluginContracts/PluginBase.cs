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
using System.Linq;

namespace PluginContracts
{
    public abstract class PluginBase : IWorkflowPlugin
    {
        protected string _assumedRouteName;

        protected Dictionary<string, Func<string, IWorkflowPluginHost, string, IWorkflowWorker>> _factories =
            new Dictionary<string, Func<string, IWorkflowPluginHost, string, IWorkflowWorker>>();

        private object _initializeLock = new object();
        private bool _initialized;


        public PluginBase()
        {
            var routeAttr = GetType().GetCustomAttributes(typeof (ExportMetadataAttribute), true)
                .Select(o => o as ExportMetadataAttribute)
                .SingleOrDefault(a => a.Name == "Route");

            _assumedRouteName = (routeAttr == null) ? GetType().Name : (string) routeAttr.Value;
        }

        #region IWorkflowPlugin Members

        [Import]
        public IWorkflowPluginHost Host { get; set; }

        public IWorkflowWorker CreateWorkerInstance(string route, string id)
        {
            Initialize();
            return _factories[GetWorkerRoute(route)](id, Host, _assumedRouteName);
        }


        public IEnumerable<string> ListWorkerRoutes()
        {
            Initialize();
            return _factories.Keys.Select(k => _assumedRouteName + @"/" + k);
        }

        #endregion

        private void Initialize()
        {
            lock (_initializeLock)
            {
                if (!_initialized)
                {
                    var factories = ProvideWorkers();
                    foreach (var f in factories)
                    {
                        _factories.Add(f.WorkerRoute, f.Factory);
                    }

                    _initialized = true;
                }
            }
        }

        protected string GetWorkerRoute(string route)
        {
            return _factories.Keys.Where(k => k.StartsWith(route)).First();
        }

        protected abstract IEnumerable<WorkerEntry> ProvideWorkers();

        #region Nested type: WorkerEntry

        public class WorkerEntry
        {
            public WorkerEntry()
            {
            }

            public WorkerEntry(string workerRoute, Func<string, IWorkflowPluginHost, string, IWorkflowWorker> factory)
            {
                WorkerRoute = workerRoute;
                Factory = factory;
            }


            public string WorkerRoute { get; set; }
            public Func<string, IWorkflowPluginHost, string, IWorkflowWorker> Factory { get; set; }
        }

        #endregion
    }
}