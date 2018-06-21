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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppComponents.Extensions.EnumerableEx;
using log4net;

namespace AppComponents.Azure
{
    public class LocalMessageBus : IPublisher, IListener
    {
        private ConcurrentQueue<object> _bus = new ConcurrentQueue<object>();
        private CancellationToken _ct;
        private DebugOnlyLogger _dblog;
        private ConcurrentBag<Task> _inProcess = new ConcurrentBag<Task>();
        private ILog _log;
        private List<Subscription> _subscriptions = new List<Subscription>();
        private object subscriptionsLock = new object();

        public LocalMessageBus()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);

            var source = new CancellationTokenSource();
            _ct = source.Token;
        }

        #region IListener Members

        public void Subscribe<T>(object listener, Action<CancellationToken, T> subscription, string routeFilter = null)
        {
            lock (subscriptionsLock)
            {
                _subscriptions.Add(new Subscription
                                       {
                                           Listener = listener,
                                           ContentType = typeof (T),
                                           Filter = routeFilter,
                                           Action = (ct, o) => subscription(ct, (T) o)
                                       });
            }
        }

        public void UnSubscribe(object listener)
        {
            lock (subscriptionsLock)
            {
                var subscriptions = from s in _subscriptions
                                    where s.Listener == listener
                                    select s;


                subscriptions.ForEach(s => _subscriptions.Remove(s));
            }
        }

        #endregion

        #region IPublisher Members

        public void Publish<T>(T msgObject, string route = "")
        {
            ProcessMessage(msgObject, msgObject.GetType().FullName, route, _ct);
        }

        public void Publish(string msg, string type, string route = "")
        {
            ProcessMessage(msg, type, route, _ct);
        }

        #endregion

        public bool ProcessMessage(object msg, string type, string route, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            Task processTask = Task.Factory.StartNew(
                om =>
                    {
                        try
                        {
                            ct.ThrowIfCancellationRequested();


                            Subscription[] subscriptions;

                            lock (subscriptionsLock)
                            {
                                subscriptions = _subscriptions
                                    .Where(s => s.ContentType.FullName == type &&
                                                (string.IsNullOrEmpty(s.Filter) || s.Filter == route)).ToArray();
                            }

                            ct.ThrowIfCancellationRequested();


                            if (subscriptions.Any())
                            {
                                ParallelOptions po = new ParallelOptions {CancellationToken = ct};

                                ct.ThrowIfCancellationRequested();

                                Parallel.ForEach(subscriptions, po,
                                                 s =>
                                                     {
                                                         po.CancellationToken.ThrowIfCancellationRequested();

                                                         s.Action(ct, msg);
                                                     }
                                    );
                            }
                        }
                        catch (AggregateException aex)
                        {
                            var ex = aex.Flatten();

                            _log.Error(ex.Message);
                            _log.Error(ex.StackTrace);

                            throw;
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex.Message);
                            _log.Error(ex.ToString());

                            throw;
                        }
                    }, msg);

            _inProcess.Add(processTask);

            ct.ThrowIfCancellationRequested();


            MaybeCleanUpInProcess();


            return true;
        }

        private void MaybeCleanUpInProcess()
        {
            if (_inProcess.Any(t => t.IsCompleted))
            {
                Task each;
                if (_inProcess.TryTake(out each))
                {
                    if (each.IsCompleted)
                    {
                        each.Dispose();
                    }
                    else
                    {
                        _inProcess.Add(each);
                    }
                }
            }
        }

        #region Nested type: Subscription

        private struct Subscription
        {
            public object Listener { get; set; }
            public Type ContentType { get; set; }
            public string Filter { get; set; }
            public Action<CancellationToken, object> Action { get; set; }
        }

        #endregion
    }
}