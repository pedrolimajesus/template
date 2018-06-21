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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.StorageClient;
using log4net;
using smarx.WazStorageExtensions;

namespace AppComponents.Azure
{
    /// <summary>
    ///   Implements the <see cref="IDistributedMutex" /> interface using azure storage blob leases to control access to the named mutexes. Uses the <see
    ///    cref="DistributedMutexLocalConfig" /> preconfiguration. Creates a system timer to automatically renew the lease; make sure to properly dispose.
    /// </summary>
    public class AzureEnvironmentDistributedMutex : IDistributedMutex
    {
        private CancellationTokenSource _cancelGrooming;
        private CloudBlobContainer _container;

        private CancellationTokenSource _cts;
        private DebugOnlyLogger _dblog;
        private TimeSpan _expireUnused;
        private Task _groomingTask;
        private bool _isDisposed;
        private CloudBlob _leaseBlob;
        private string _leaseId;
        private ILog _log;
        private string _name;
        private Task _renewLease;

        public AzureEnvironmentDistributedMutex()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);

            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _name = config[DistributedMutexLocalConfig.Name].ToLowerInvariant();
            _expireUnused = TimeSpan.FromSeconds(config.Get<int>(DistributedMutexLocalConfig.UnusedExpirationSeconds));

            var account = Client.FromConfig().ForBlobs();
            _container = account.GetContainerReference(AzureConstants.LeaseContainer);
            _container.CreateIfNotExist();

            _leaseBlob = _container.GetBlobReference(_name);

            _cts = new CancellationTokenSource();
            _cancelGrooming = new CancellationTokenSource();

            try
            {
                if (!_leaseBlob.Exists())
                {
                    _leaseBlob.UploadText("1");
                    _leaseBlob.SetExpiration(_expireUnused);

                    _log.InfoFormat("Creating distributed mutex for {0}, auto expires in {1}", _name, _expireUnused);
                }
            }
            catch (StorageClientException storageClientException1)
            {
                StorageClientException storageClientException = storageClientException1;
                if (storageClientException.ErrorCode == StorageErrorCode.BlobAlreadyExists ||
                    storageClientException.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                }
                else
                {
                    throw;
                }
            }


            _groomingTask =
                Task.Factory.StartNew(
                    () =>
                    AzureStorageAssistant.CleanExpiredBlobsFrom(AzureConstants.LeaseContainer, _cancelGrooming.Token,
                                                                false));
        }

        #region IDistributedMutex Members

        public bool Open()
        {
            if (_leaseId != null)
                return true;


            _leaseId = _leaseBlob.TryAcquireLease();
            if (null != _leaseId)
            {
                StopRenewal();
                _leaseBlob.SetExpiration(_expireUnused);

                var ct = _cts.Token;
                _renewLease = new Task(
                    c =>
                        {
                            var cancel = (CancellationToken) c;
                            while (!cancel.IsCancellationRequested)
                            {
                                if (!cancel.WaitHandle.WaitOne(TimeSpan.FromSeconds(40)))
                                {
                                    _leaseBlob.RenewLease(_leaseId);
                                    _leaseBlob.SetExpiration(_expireUnused);

                                    _dblog.InfoFormat("Renewed lock lease for {0}", _name);
                                }
                            }
                        },
                    ct,
                    ct,
                    TaskCreationOptions.LongRunning);
                _renewLease.Start();

                _log.InfoFormat("Acquired lock on {0}", _name);

                return true;
            }

            return false;
        }

        public void Release()
        {
            if (null != _leaseId)
            {
                StopRenewal();
                _leaseBlob.ReleaseLease(_leaseId);
                _leaseId = null;

                _log.InfoFormat("Released lock on {0}", _name);
            }
        }

        public bool Wait(TimeSpan timeout)
        {
            var leased = false;
            using (ManualResetEventSlim ev = new ManualResetEventSlim(false))
            using (Timer mTimer = new Timer(_ => { ev.Set(); }, null, timeout.Milliseconds, Timeout.Infinite))
            {
                bool timedOut = false;
                do
                {
                    if (Open())
                    {
                        leased = true;
                    }
                    else
                    {
                        if (ev.Wait(40))
                        {
                            timedOut = true;
                            _log.InfoFormat("Timed out waiting to lock {0}", _name);
                        }
                    }
                } while (!leased && !timedOut);
            }

            return leased;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Release();
                StopRenewal();
                _cancelGrooming.Cancel();
                _groomingTask.Wait(10000);
                _groomingTask.Dispose();
                _cancelGrooming.Dispose();
                _cts.Dispose();
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