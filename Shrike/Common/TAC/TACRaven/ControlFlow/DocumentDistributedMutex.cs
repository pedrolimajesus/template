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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Raven.Abstractions.Exceptions;
using Raven.Client.Linq;
using log4net;

namespace AppComponents.Raven
{

    internal class DistributedMutexDocument
    {
        [DocumentIdentifier]
        public string Name { get; set; }

        public Guid Acquirer { get; set; }
        public DateTime AcquiredTime { get; set; }
        public DateTime ExpirationTime { get; set; }
        public TimeSpan UnusedExpiration { get; set; }
    }

    public class DocumentDistributedMutex : IDistributedMutex
    {
        private CancellationTokenSource _cancelGrooming;
        private CancellationToken _cancelGroomingToken;
        private CancellationTokenSource _cts;
        private DebugOnlyLogger _dblog;
        private TimeSpan _expireUnused;
        private Task _groomingTask;
        private bool _isDisposed;
        private ILog _log;
        private string _name;
        private Task _renewLease;
        private Guid _acquirer;
        private TimeSpan _renewWait;

        public DocumentDistributedMutex()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);

            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _name = config[DistributedMutexLocalConfig.Name].ToLowerInvariant();
            var seconds = config.Get<int>(DistributedMutexLocalConfig.UnusedExpirationSeconds);
            if (seconds < 15)
                seconds = 15;
            
            
            _expireUnused = TimeSpan.FromSeconds(seconds);
            _renewWait = TimeSpan.FromSeconds(seconds - 5);

            _cts = new CancellationTokenSource();
            _cancelGrooming = new CancellationTokenSource();
            _cancelGroomingToken = _cancelGrooming.Token;
            
            _groomingTask = Task.Factory.StartNew(GroomExpired,_cancelGroomingToken);
            _acquirer = Guid.NewGuid();
            CreateIfNotExists();
        }

        private void CreateIfNotExists()
        {
            try
            {
                using (var dc = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    var namedMutex = dc.Load<DistributedMutexDocument>(_name);
                    if (null == namedMutex)
                    {

                        namedMutex = new DistributedMutexDocument
                                         {
                                             Name = _name,
                                             UnusedExpiration = _expireUnused,
                                             ExpirationTime = DateTime.UtcNow + _expireUnused,
                                             Acquirer = Guid.Empty
                                         };
                       
                        dc.Store(namedMutex);
                        dc.SaveChanges();
                    }
                }
            }
            catch (ConcurrencyException)
            {
                
            }

        }


        public void GroomExpired(object parm)
        {
            var ct = (CancellationToken) parm;
            while (!ct.IsCancellationRequested)
            {
                try
                {



                    DateTime checkTime = DateTime.UtcNow - TimeSpan.FromSeconds(10); // grace period

                    using (var dc = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                    {

                        var expired = from mut in dc.Query<DistributedMutexDocument>()
                                      where mut.ExpirationTime < checkTime
                                      select mut;
                        if (expired.Any())
                        {
                            foreach (var mutex in expired)
                            {
                                if (ct.IsCancellationRequested)
                                    return;
                                dc.Delete(mutex);
                            }
                            dc.SaveChanges();
                        }
                    }

                    ct.WaitHandle.WaitOne(3000);
                }
                catch (Exception)
                {

                   
                }

                
            }

        }

        #region IDistributedMutex Members


        public bool Open()
        {
            var retval = false;
            CreateIfNotExists();


            try
            {
                using (var dc = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    var namedMutex = dc.Load<DistributedMutexDocument>(_name);
                    if (null == namedMutex)
                        return retval;
                    
                    if (namedMutex.Acquirer == Guid.Empty)
                    {
                        namedMutex.Acquirer = _acquirer;
                        namedMutex.AcquiredTime = DateTime.UtcNow;
                        namedMutex.ExpirationTime = DateTime.UtcNow + namedMutex.UnusedExpiration;

                        dc.SaveChanges();
                    }
                }

                retval = true;
            }
            catch (ConcurrencyException)
            {
                
            }

            if (retval)
            {
                var ct = _cts.Token;
                _renewLease = Task.Factory.StartNew(RenewLease, ct, TaskCreationOptions.LongRunning);
            }


            return retval;
        }

        public void RenewLease(object parm)
        {
            var ct = (CancellationToken)parm;

            while (!ct.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;
                var expTime = DateTime.UtcNow + _renewWait;
                int retries = 0;
                bool taken = false;

                while (!taken && retries < 6)
                {
                    try
                    {
                        if (ct.IsCancellationRequested)
                            return;

                        using (var dc = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                        {
                            var namedMutex = dc.Load<DistributedMutexDocument>(_name);
                            if (null != namedMutex && namedMutex.Acquirer == _acquirer)
                            {
                                namedMutex.ExpirationTime = DateTime.UtcNow + namedMutex.UnusedExpiration;
                                namedMutex.AcquiredTime = DateTime.UtcNow;
                                expTime = namedMutex.ExpirationTime;
                            }

                            dc.SaveChanges();
                            taken = true;
                        }
                    }
                    catch (Exception)
                    {
                        retries += 1;
                        if (retries > 5)
                            throw new AbandonedMutexException();
                    }
                }

                if (ct.IsCancellationRequested)
                    return;

                var waitTime = startTime + TimeSpan.FromMilliseconds(_renewWait.TotalMilliseconds /2);
                var waitPeriod = waitTime - DateTime.UtcNow;
                if (waitPeriod.TotalSeconds < 1)
                    waitPeriod = TimeSpan.FromMilliseconds(100.0);

                //System.Threading.Thread.Sleep(waitPeriod);
                ct.WaitHandle.WaitOne(waitPeriod);
            }


        }

        public void Release()
        {
            StopRenewal();
            using (var dc = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var namedMutex = dc.Load<DistributedMutexDocument>(_name);
                if (null != namedMutex && namedMutex.Acquirer == _acquirer)
                {
                    namedMutex.Acquirer = Guid.Empty;
                    namedMutex.ExpirationTime = DateTime.UtcNow + namedMutex.UnusedExpiration;
                    dc.SaveChanges();
                }
            }
            
        }

        public bool Wait(TimeSpan timeout)
        {

            var locked = false;
            using (var ev = new ManualResetEventSlim(false))
            using (var mTimer = new Timer(_ => { ev.Set(); }, null, timeout.Milliseconds, Timeout.Infinite))
            {
                bool timedOut = false;
                do
                {
                    if (Open())
                    {
                        locked = true;
                    }
                    else
                    {
                        if (ev.Wait(40))
                        {
                            timedOut = true;
                            _log.InfoFormat("Timed out waiting to lock {0}", _name);
                        }
                    }
                } while (!locked && !timedOut);
            }

            return locked;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Release();
    
                _cancelGrooming.Cancel();
                _groomingTask.Wait(10000);
                
                _cancelGrooming.Dispose();
                _cts.Dispose();

                _isDisposed = true;
            }
        }

        #endregion

        private void StopRenewal()
        {
            if (null != _renewLease)
            {
                _cts.Cancel();
                
                _renewLease.Wait();
                _renewLease.Dispose();
                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }
        }
    }
}