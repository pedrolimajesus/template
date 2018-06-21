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
using AppComponents.Extensions.ExceptionEx;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using log4net;
using smarx.WazStorageExtensions;

namespace AppComponents.Azure
{
    /// <summary>
    ///   Implements a distributed message bus using Azure storage queues. It provides services for the <see cref="IPublisher" /> , <see
    ///    cref="IListener" /> , and <see cref="IMessageBusWorker" /> interfaces, under the distributed catalog context. Messages too large for the queue are serialized to blob storage automatically. Uses the <see
    ///    cref="MessageBusLocalConfig" /> preconfigurations.
    /// </summary>
    public class AzureDistributedMessageBus :
        IPublisher, IListener, IMessageBusWorker
    {
        private const string GlobalBusName = "globalmessagebus";

        private static readonly string _containerName = "pocketedmessages";
        private static readonly TimeSpan oldMessage = CloudQueueMessage.MaxTimeToLive + TimeSpan.FromDays(1.0);
        private CloudBlobClient _bc;
        private CloudBlobContainer _container;
        private DebugOnlyLogger _dblog;
        private DateTime _groomSchedule = DateTime.UtcNow;
        private AzureHostEnvironment _he = new AzureHostEnvironment();
        private ConcurrentBag<Task> _inProcess = new ConcurrentBag<Task>();
        private ILog _log;
        private DateTime _migrateSchedule = DateTime.UtcNow;

        private HorizonalScaleCloudQueue _q;
        private CloudQueueClient _qc;
        private string _queueName = GlobalBusName;
        private Random _randomSelector = new Random();
        private List<Subscription> _subscriptions = new List<Subscription>();
        private object containerLock = new object();
        private object qLock = new object();
        private object subscriptionsLock = new object();

        public AzureDistributedMessageBus()
        {
            var account =
                CloudStorageAccount.FromConfigurationSetting(CommonConfiguration.DefaultStorageConnection.ToString());
            account.Ensure(containers: new[] {_containerName});
            _qc = account.CreateCloudQueueClient();

            IConfig config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);

            int partitionCount = 5;
            var busName = config.Get(MessageBusLocalConfig.OptionalNamedMessageBus, GlobalBusName);
            if (GlobalBusName != busName)
            {
                _queueName = busName;
            }

            _q = new HorizonalScaleCloudQueue(_qc, _queueName, partitionCount);
            _bc = account.CreateCloudBlobClient();
            _container = _bc.GetContainerReference(_containerName);
            _log = ClassLogger.Create(typeof (AzureDistributedMessageBus));
            _dblog = DebugOnlyLogger.Create(_log);
        }

        #region IListener Members

        public void Subscribe<T>(object listener, Action<CancellationToken, T> subscription, string routeFilter = null)
        {
            _log.InfoFormat("Adding subscription: {0}, {1}", typeof (T).FullName, routeFilter);
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

        #region IMessageBusWorker Members

        public bool ProcessMessage(CancellationToken ct)
        {
            CloudQueueMessage qm = null;
            lock (qLock)
                qm = _q.GetMessage(TimeSpan.FromMinutes(1.0));

            if (qm == null)
                return false;

            ct.ThrowIfCancellationRequested();

            Task processTask = Task.Factory.StartNew(
                om =>
                    {
                        try
                        {
                            ct.ThrowIfCancellationRequested();
                            Message m = RehydrateMessage((CloudQueueMessage) om);

                            _dblog.InfoFormat("Received message: ", m.ToString());

                            Subscription[] subscriptions;

                            lock (subscriptionsLock)
                            {
                                subscriptions = (from s in _subscriptions
                                                 where s.ContentType.FullName == m.ContentType &&
                                                       (string.IsNullOrEmpty(s.Filter) || s.Filter == m.Route)
                                                 select s).ToArray();
                            }

                            ct.ThrowIfCancellationRequested();

                            _dblog.InfoFormat("{0} subscribers for message {1} '{2}'", subscriptions.Length,
                                              m.ContentType, m.Route);

                            if (subscriptions.Any())
                            {
                                object data = JsonConvert.DeserializeObject(m.Content, subscriptions.First().ContentType);
                                ParallelOptions po = new ParallelOptions {CancellationToken = ct};

                                ct.ThrowIfCancellationRequested();

                                Parallel.ForEach(subscriptions, po,
                                                 s =>
                                                     {
                                                         po.CancellationToken.ThrowIfCancellationRequested();

                                                         s.Action(ct, data);
                                                     }
                                    );
                            }
                        }
                        catch (AggregateException aex)
                        {
                            var ex = aex.Flatten();

                            _log.TraceException(ex);

                            throw;
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex.Message);
                            _log.Error(ex.ToString());

                            throw;
                        }

                        lock (qLock)
                            _q.DeleteMessage((CloudQueueMessage) om);
                    }, qm);

            _inProcess.Add(processTask);

            ct.ThrowIfCancellationRequested();

            MaybeGroomPocketed(ct);

            MaybeMigrateHostedScope(ct);

            MaybeCleanUpInProcess();


            return true;
        }

        #endregion

        #region IPublisher Members

        public void Publish<T>(T msgObject, string route = "")
        {
            var jsonContent = JObject.FromObject(msgObject);
            Publish(jsonContent.ToString(), typeof (T).FullName, route);
        }


        public void Publish(string msg, string type, string route = "")
        {
            _log.InfoFormat("Publishing {0} for type{1} on route {2}", msg, type, route);

            Message m = new Message
                            {
                                ContentType = type,
                                Route = route,
                                Content = msg
                            };

            var textMessage = JsonConvert.SerializeObject(m);
            if (textMessage.Length >= CloudQueueMessage.MaxMessageSize)
            {
                var id = new TemporalId();
                m.Reference = id.Id;
                m.Content = null;

                _dblog.InfoFormat("Pocketing message at length {0} to reference {1}", textMessage.Length, id.Id);

                CloudBlob blob = null;
                lock (containerLock)
                    blob = _container.GetBlobReference(m.Reference);

                blob.UploadText(msg);
                textMessage = JsonConvert.SerializeObject(m);
            }

            lock (qLock)
                _q.AddMessage(new CloudQueueMessage(textMessage));
        }

        #endregion

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

        private void MaybeGroomPocketed(CancellationToken ct)
        {
            if (DateTime.UtcNow > _groomSchedule)
            {
                AzureStorageAssistant.GroomOldBlobsFrom(_containerName, oldMessage, ct);
                _groomSchedule = DateTime.UtcNow + TimeSpan.FromHours(1.0);
            }
        }

        private void MaybeMigrateHostedScope(CancellationToken ct)
        {
            // only one worker needs to do this job.
            if (_queueName == GlobalBusName && DateTime.UtcNow > _migrateSchedule)
            {
                // get a list of all hostscope queues
                var namedBusQs = _qc.ListQueues("hostscope_");
                if (namedBusQs.Any())
                {
                    foreach (var nbq in namedBusQs)
                    {
                        var parts = nbq.Name.Split('_');
                        var scopeName = parts[1];
                        var roleName = parts[2];
                        var roleId = parts[3];

                        // are there any roles of the right type?
                        var matchingRole =
                            RoleEnvironment.Roles.Select(r => r.Value).Where(r => r.Name == roleName).FirstOrDefault();

                        if (matchingRole == null)
                        {
                            // very bad, there are NO roles to adopt these messages.
                            break;
                        }


                        // check if host exists
                        if (!matchingRole.Instances.Any(i => i.Id == roleId))
                        {
                            // this queue represents an older role instance.

                            // find a role instance that can adopt the messages.
                            var adoptee = matchingRole.Instances.At(_randomSelector.Next(matchingRole.Instances.Count()));

                            // open up the zombie q
                            var q = _qc.GetQueueReference(nbq.Name);

                            // adoptee is first partition of the hosted scope bus.
                            var ad =
                                _qc.GetQueueReference(string.Format("{0}0",
                                                                    _he.MakeIdentifier(scopeName, roleName, adoptee.Id)));

                            // it will exist sooner or later, right?
                            ad.CreateIfNotExist();

                            if (q.Exists())
                            {
                                // migrate messages
                                CloudQueueMessage m;
                                do
                                {
                                    m = q.GetMessage();
                                    if (null != m)
                                    {
                                        ad.AddMessage(m);
                                        q.DeleteMessage(m.Id, m.PopReceipt);
                                    }
                                } while (null != m);

                                // clean up old q
                                if (q.RetrieveApproximateMessageCount() == 0)
                                    q.Delete();
                            }
                        }
                    }
                }

                _groomSchedule = DateTime.UtcNow + TimeSpan.FromHours(1.0);
            }
        }


        private Message RehydrateMessage(CloudQueueMessage cqm)
        {
            Message message = JsonConvert.DeserializeObject<Message>(cqm.ToString());
            if (!string.IsNullOrEmpty(message.Reference))
            {
                CloudBlob blob = null;

                lock (containerLock)
                    blob = _container.GetBlobReference(message.Reference);

                var text = blob.DownloadText();
                _dblog.InfoFormat("Retrieved pocketed message from {0} with length {1}", blob.Uri, text.Length);


                message.Content = text;
            }

            return message;
        }

        #region Nested type: Message

        private class Message
        {
            public Message()
            {
                Version = "1";
            }

            public string Version { get; set; }
            public string ContentType { get; set; }
            public string Route { get; set; }
            public string Reference { get; set; }
            public string Content { get; set; }
        }

        #endregion

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