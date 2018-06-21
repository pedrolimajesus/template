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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppComponents.RandomNumbers;
using log4net;

namespace AppComponents
{
    public class StochasticRecurringWorker : IWorkerEntryPoint
    {
        public class RecurringWork
        {
            private Random rand;

            public RecurringWork()
            {
                rand = GoodSeedRandom.Create();
            }

            public DateTime Schedule { get; set; }
            public TimeSpan StochasticBoundaryMax { get; set; }
            public TimeSpan StochasticBoundaryMin { get; set; }
            public Action<CancellationToken> Work { get; set; }

            public bool Due()
            {
                return DateTime.UtcNow > Schedule;
            }

            public void Initialize(DateTime fromTime)
            {
                var delay = TimeSpan.FromSeconds(rand.Next(StochasticBoundaryMax.Seconds));
                Schedule = fromTime + delay;
            }

            public void Reschedule()
            {
                var range = StochasticBoundaryMax.Seconds - StochasticBoundaryMin.Seconds;
                var delay = TimeSpan.FromSeconds(rand.Next(range) + StochasticBoundaryMin.Seconds);
                Schedule = DateTime.UtcNow + delay;
            }
        }

        protected DateTime _baseTime;
        protected CancellationToken _token;
        protected ILog _log;
        protected DebugOnlyLogger _dblog;
        protected List<RecurringWork> _work = new List<RecurringWork>();
        protected object _workLock = new object();
        protected ManualResetEventSlim _stop = new ManualResetEventSlim(false);
        protected bool _started;

#if DEBUG
        private const int _delay = 3;
#else
        const int _delay = 60;
#endif

        public StochasticRecurringWorker()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);

            var rand = GoodSeedRandom.Create();
            _baseTime = DateTime.UtcNow + TimeSpan.FromMinutes(rand.Next(_delay));
        }

        public StochasticRecurringWorker AddWork(Action<CancellationToken> work, TimeSpan maxDelay, TimeSpan minDelay)
        {
            var rw = new RecurringWork
                         {
                             StochasticBoundaryMin = minDelay,
                             StochasticBoundaryMax = maxDelay,
                             Work = work
                         };

            AddInitializeWork(rw);

            return this;
        }

        private void AddInitializeWork(RecurringWork rw)
        {
            DateTime fromTime = _baseTime;
            lock (_workLock)
            {
                if (_work.Any())
                    fromTime = _work.Max(w => w.Schedule);
                rw.Initialize(fromTime);
                _work.Add(rw);
            }
        }

        public StochasticRecurringWorker AddWorkFromCatalog()
        {
            var registeredWork = Catalog.Factory.ResolveAll<RecurringWork>();
            foreach (var work in registeredWork)
            {
                AddInitializeWork(work);
            }
            return this;
        }

        public void Initialize(CancellationToken token)
        {
            _token = token;
        }

        public bool OnStart()
        {
            _started = true;
            _stop.Reset();
            bool hasWork = false;
            lock (_workLock)
            {
                hasWork = _work.Any();
            }
            if (hasWork)
            {
                Task.Factory.StartNew(() => ProtectedRun());
            }
            return hasWork;
        }

        public void OnStop()
        {
            _stop.Set();
            _started = false;
        }

        public void Run()
        {
            while (!_token.IsCancellationRequested && !_stop.IsSet)
            {
                RecurringWork[] dueWork = null;

                lock (_workLock)
                {
                    dueWork = (from rw in _work where rw.Due() select rw).ToArray();
                }

                foreach (var dw in dueWork)
                {
                    dw.Work(_token);
                    dw.Reschedule();
                }

                if (!_stop.IsSet && !_token.IsCancellationRequested)
                {
                    var soonest = (from rw in _work select rw.Schedule).Min();
                    var delay = soonest - DateTime.UtcNow;
                    if (delay.Seconds > 0)
                        WaitHandle.WaitAny(new[] {_token.WaitHandle, _stop.WaitHandle}, delay.Milliseconds);
                }
            }
        }

        public void ProtectedRun()
        {
            try
            {
                Run();
            }
            catch
            {
            }
        }
    }
}