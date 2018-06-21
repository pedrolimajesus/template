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
using System.Diagnostics;
using System.Threading;
using log4net;

namespace AppComponents
{
    public abstract class SleepyWorkerEntryPoint : IWorkerEntryPoint
    {
        /// Application name
        protected internal string _applicationName;

        protected DebugOnlyLogger _dblog;
        protected ILog _log;
        protected CancellationToken _token;

        protected SleepyWorkerEntryPoint()
        {
            MinThreadSleep = 100;
            MaxThreadSleep = 2200;
            CurrentThreadSleep = MinThreadSleep;
            _log = ClassLogger.Create(typeof (SleepyWorkerEntryPoint));
            _dblog = DebugOnlyLogger.Create(_log);

            _log.InfoFormat("Creating new {0}", GetType());
        }

        /// <summary>
        ///   Current number of seconds to thread sleep. A value between MinThreadSleepInSeconds and MaxThreadSleepInSeconds.
        /// </summary>
        private int CurrentThreadSleep { get; set; }

        /// <summary>
        ///   Minimum number of seconds to thread sleep
        /// </summary>
        public int MinThreadSleep { get; set; }

        /// <summary>
        ///   Maximum number of seconds to thread sleep
        /// </summary>
        public int MaxThreadSleep { get; set; }

        #region IWorkerEntryPoint Members

        public void Initialize(CancellationToken token)
        {
            _token = token;
        }

        /// <summary>
        ///   Starts the worker thread. Will not return until thread abort is called.
        /// </summary>
        public void Run()
        {
            _log.InfoFormat("Worker was started");
            Debug.Indent();

            // Loop until the thread is closing
            while (!_token.IsCancellationRequested)
            {
                var numberOfProcessedMessagesInQueue = ProcessItems();
                if (numberOfProcessedMessagesInQueue > 0)
                {
                    _log.InfoFormat("{0} items was processed; so resetting thread sleep duration",
                                    numberOfProcessedMessagesInQueue);
                    CurrentThreadSleep = MinThreadSleep;
                }
                else
                {
                    _log.InfoFormat(
                        "No items were processed; so backing off by doubling the thread sleep duration until the MaxThreadSleepInSeconds limit has been reached.");
                    BackOff();
                }
            }

            Debug.Unindent();
            _log.InfoFormat("Worker is stopping");
        }

        public virtual bool OnStart()
        {
            return true;
        }

        public virtual void OnStop()
        {
        }

        /// <summary>
        ///   This method prevents unhandled exceptions from being thrown from the worker thread.
        /// </summary>
        public void ProtectedRun()
        {
            try
            {
                // Call the Workers Run() method
                Run();
            }
            catch (SystemException sex)
            {
                var es = "Worker system exception, exiting";
                IApplicationAlert on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es, sex);

                _log.Fatal(es, sex);
                throw;
            }
            catch (Exception ex)
            {
                var es = string.Format("Swallowed exception while running worker {0}:", GetType().FullName);
                IApplicationAlert on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.Defect, es, ex);

                _dblog.Error(es, ex);
            }
        }

        #endregion

        /// <summary>
        ///   If there was nothing to do for the worker then double the thread sleep duration from last time, but make sure that the set max sleep duration MaxThreadSleepInSeconds is respected.
        /// </summary>
        private void DoubleThreadSleepDuration()
        {
            _log.InfoFormat("Old sleep duration was {0} ...", CurrentThreadSleep);

            CurrentThreadSleep *= 2;

            if (CurrentThreadSleep == 0)
                CurrentThreadSleep = 1;
            else if (CurrentThreadSleep > MaxThreadSleep)
                CurrentThreadSleep = MaxThreadSleep;

            _log.InfoFormat("... new sleep duration is {0}", CurrentThreadSleep);
        }

        /// <summary>
        ///   Blocks the current thread for CurrentThreadSleepInSeconds seconds
        /// </summary>
        private void Sleep()
        {
            _log.InfoFormat("Thread is sleeping for {0} ms", CurrentThreadSleep);
            _token.WaitHandle.WaitOne(CurrentThreadSleep);
            

            _log.InfoFormat("Thread is done sleeping for {0} ms", CurrentThreadSleep);
        }

        /// <summary>
        ///   Implements a Back Off software design pattern that works as follows: A thread sleep interval after processing the queue is started at 0 seconds. Every time there are no items to process in the queue then the thread sleep interval is increased with one second until a set limit (let say one minute for the sake of the example) is reached. Every time there are items to process in the queue the thread sleep interval is set to 0 seconds again. Since you with Azure pay for the processing time you use the Back Off pattern is quite useful, but be careful to use it with care so that your users will not have to wait a very long time for something that should be done immidately.
        /// </summary>
        internal void BackOff()
        {
            _log.InfoFormat(
                "No items processed, so using a Back Off software design pattern to conserve otherwise idle resources");

            DoubleThreadSleepDuration();
            Sleep();
        }

        /// <summary>
        ///   Proesses all items
        /// </summary>
        /// <returns> Number of items that was processed </returns>
        public abstract int ProcessItems();
    }
}