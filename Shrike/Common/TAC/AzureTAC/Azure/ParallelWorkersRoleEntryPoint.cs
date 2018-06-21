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
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppComponents.Extensions.EnumerableEx;
using Microsoft.WindowsAzure.ServiceRuntime;
using log4net;

namespace AppComponents.Azure
{
    public abstract class ParallelWorkersRoleEntryPoint : RoleEntryPoint, IDisposable
    {
        protected EventWaitHandle EventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        protected CancellationTokenSource _cts;
        private DebugOnlyLogger _dbg;
        private ILog _lg;
        protected Collection<Task> _tasks = new Collection<Task>();

        protected IWorkerEntryPoint[] _workers;

        protected ParallelWorkersRoleEntryPoint()
        {
            _lg = ClassLogger.Create(typeof (ParallelWorkersRoleEntryPoint));
            _dbg = DebugOnlyLogger.Create(_lg);
        }

        #region IDisposable Members

        public void Dispose()
        {
            EventWaitHandle.Dispose();
            _cts.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        private void CreateThreadsForAllWorkers()
        {
            _dbg.InfoFormat("Creating tasks for {0} workers", _workers.EmptyIfNull().Count());
            foreach (var worker in _workers.EmptyIfNull())
            {
                _dbg.InfoFormat("Task for worker of type {1}", worker.GetType().FullName);
                worker.Initialize(_cts.Token);
                _tasks.Add(new Task(worker.ProtectedRun, _cts.Token, TaskCreationOptions.LongRunning));
            }
        }

        private void StartAllWorkerThreads()
        {
            _tasks.EmptyIfNull().ForEach(t => t.Start());
        }

        private void StartAllWorkerRolesInNewThreads()
        {
            if (_cts != null) _cts.Dispose();
            _cts = new CancellationTokenSource();
            CreateThreadsForAllWorkers();
            StartAllWorkerThreads();
        }


        private void RestartDeadWorkerRoleThreads()
        {
            Contract.Requires(_tasks != null);
            Contract.Requires(_tasks.Count == _workers.Count());

            for (var i = 0; i != _tasks.Count; i++)
            {
                if (_tasks[i].Status != TaskStatus.Running)
                {
                    _lg.WarnFormat("Dead worker role thread detected for worker {0}, restarting",
                                   _workers[i].GetType().FullName);

                    _tasks[i] = new Task(_workers[i].Run, _cts.Token);
                    _tasks[i].Start();
                }
            }
        }

        private void KeepThreadsAliveMonitorLoop()
        {
            var checkDeadThreadSleepInSeconds = 60;

            while (!EventWaitHandle.WaitOne(0))
            {
                //RestartDeadWorkerRoleThreads();

                EventWaitHandle.WaitOne(checkDeadThreadSleepInSeconds);
            }
        }

        public override void Run()
        {
            StartAllWorkerRolesInNewThreads();

            KeepThreadsAliveMonitorLoop();
        }

        public bool OnStart(IWorkerEntryPoint[] workers)
        {
            Contract.Requires(null != workers);

            _workers = workers;


            foreach (var worker in workers) worker.OnStart();

            return base.OnStart();
        }

        public override bool OnStart()
        {
            return true;
        }

        private void AbortAllWorkerThreads()
        {
            _lg.Info("Aborting all worker threads");
            if (_cts != null)
                _cts.Cancel();
        }


        private void WaitUntilAllWorkerThreadsAreDead()
        {
            try
            {
                Task.WaitAll(_tasks.ToArray(), TimeSpan.FromMinutes(2.0));
            }
            catch (AggregateException)
            {
            }
        }

        private void NotifyAllWorkerToStopLooping()
        {
            foreach (var worker in _workers) worker.OnStop();
        }

        public override void OnStop()
        {
            EventWaitHandle.Set();

            AbortAllWorkerThreads();
            WaitUntilAllWorkerThreadsAreDead();
            NotifyAllWorkerToStopLooping();

            base.OnStop();
        }
    }
}