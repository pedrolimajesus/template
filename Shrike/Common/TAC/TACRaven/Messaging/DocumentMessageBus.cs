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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppComponents.Extensions.EnumEx;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.Messaging.Declarations;
using AppComponents.RandomNumbers;
using AppComponents.Raven;

namespace AppComponents.Messaging
{
    using global::Raven.Imports.Newtonsoft.Json;
    using global::Raven.Imports.Newtonsoft.Json.Bson;

    public class RavenMessageListener : IMessageListener
    {
        private readonly RavenMessageInbox _q;
    

        private readonly Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>> _sinks =
            new Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>>();

        private CancellationToken _ct;
        private CancellationTokenSource _cts;
        private bool _isDisposed;
        private Task _listening;


        public RavenMessageListener()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _q = new RavenMessageInbox(config[MessageListenerLocalConfig.QueueName]);
   
            _cts = new CancellationTokenSource();
        }

        #region IMessageListener Members

        public void Listen(params KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>[] listener)
        {
            if (null != _listening)
            {
                _cts.Cancel();
                _listening.Wait();
                _listening.Dispose();

                _cts = new CancellationTokenSource();
            }

            _sinks.Clear();
            foreach (var sink in listener)
                _sinks.Add(sink.Key, sink.Value);

            _ct = _cts.Token;

            _listening = Task.Factory.StartNew(Listen, this, _ct);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _cts.Cancel();
                _listening.Wait();
                _cts.Dispose();
                _listening.Dispose();
              
       
            }
        }

        #endregion

        private static void Listen(object that)
        {
            var @this = (RavenMessageListener)that;

            while (!@this._ct.IsCancellationRequested)
            {
                var messages = @this._q.WaitForMessages(TimeSpan.FromSeconds(60.0));
                if (messages.EmptyIfNull().Any())
                {
                    foreach (var env in messages)
                    {
                        var msg = env.Decode();

                        if (@this._sinks.ContainsKey(env.MessageType))
                        {
                            var sink = @this._sinks[env.MessageType];
                            sink(msg, @this._cts.Token, new RavenQueueAcknowledge(env));
                        }
                    }
                }
            }
        }

       

        
    }

    internal class RavenQueueAcknowledge : IMessageAcknowledge
    {
        private readonly DocumentMessageEnvelope _message;

        public RavenQueueAcknowledge(DocumentMessageEnvelope message)
        {
            _message = message;
        }

        #region IMessageAcknowledge Members

        public void MessageAcknowledged()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(MessageBusSpecifierLocalConfig.HostConnectionString))
            {
                var it = session.Load<DocumentMessageEnvelope>(_message.Id);
                if (it != null)
                {

                    session.Delete(it);
                    session.SaveChanges();
                }
            }
        }

        public void MessageAbandoned()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(MessageBusSpecifierLocalConfig.HostConnectionString))
            {
                var it = session.Load<DocumentMessageEnvelope>(_message.Id);
                if (it != null)
                {
                    it.Reserved = false;

                    session.SaveChanges();
                }
            }
        }

        public void MessageRejected()
        {
            var ia = Catalog.Factory.Resolve<IApplicationAlert>();
            if (ia != null)
            {
                ia.RaiseAlert(ApplicationAlertKind.System, "Message rejected during processing", this._message);
            }
            MessageAcknowledged();
        }

        #endregion
    }

    public class RavenMessagePublisher : IMessagePublisher
    {
        private readonly RavenMessageBus _bus;
        private readonly string _exchangeName;
        private ExchangeTypes _exchangeType;
        private bool _isDisposed;
        private List<IQueueSpecifier> _queues;


        public RavenMessagePublisher()
        {
            var cfg = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _exchangeName = cfg[MessagePublisherLocalConfig.ExchangeName];

            _bus =  new RavenMessageBus();
        }

        #region IMessagePublisher Members

        public void Send<T>(T msg, string routeKey)
        {
            RefreshQueues();

            var matchingQueues = MessageExchangeDeclaration.BindMessageToQueues(routeKey, _exchangeType, _queues);
            foreach (var q in matchingQueues)
            {
                var outbox = new RavenMessageOutbox(q);
                outbox.Enqueue(msg);
                outbox.Send();
                
            }
        }

        public void Send<T>(T msg, Enum routeKey)
        {
            Send(msg, routeKey.EnumName());
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _bus.Dispose();
            }
        }

        #endregion

        private void RefreshQueues()
        {
            var ex = _bus.SpecifyExchange(_exchangeName);
            if (null != ex)
            {
                _queues = ex.Queues.ToList();
                _exchangeType = ex.ExchangeType;
            }
            else
            {
                _queues = new List<IQueueSpecifier>();
            }
        }
    }


    public class RavenMessageBus : IMessageBusSpecifier
    {
        
        private bool _isDisposed;

        #region IMessageBusSpecifier Members

        public IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType)
        {

            using (var session = DocumentStoreLocator.ResolveOrRoot(MessageBusSpecifierLocalConfig.HostConnectionString))
            {
                var exNameObject = session.Load<MessageExchangeDeclaration>(exchangeName);
                if (exNameObject == null)
                {
                    var newExchange = new MessageExchangeDeclaration {Name = exchangeName, Type = exchangeType};
                    session.Store(newExchange);
                    session.SaveChanges();
                }
            }

            return this;

        }

        public IMessageBusSpecifier DeclareExchange(Enum exchangeName, ExchangeTypes exchangeType)
        {
            return DeclareExchange(exchangeName.EnumName(), exchangeType);
            
        }

        public IMessageBusSpecifier DeleteExchange(string exchangeName)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(MessageBusSpecifierLocalConfig.HostConnectionString))
            {
                var it = session.Load<MessageExchangeDeclaration>(exchangeName);
                if (null != it)
                {
                    session.Delete(it);
                    session.SaveChanges();
                }
            }

           

            return this;
        }

        public IMessageBusSpecifier DeleteExchange(Enum exchangeName)
        {
            return DeleteExchange(exchangeName.EnumName());
        }

        public IExchangeSpecifier SpecifyExchange(string exchangeName)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(MessageBusSpecifierLocalConfig.HostConnectionString))
            {
                var spec = session.Load<MessageExchangeDeclaration>(exchangeName);

                if (spec != null)
                {
                    return new RavenExchange(this, spec);
                }
                return null;
            }
        }

        public IExchangeSpecifier SpecifyExchange(Enum exchangeName)
        {
            return SpecifyExchange(exchangeName.EnumName());
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }

        #endregion

        internal void UpdateExchange(MessageExchangeDeclaration exchangeSpec)
        {

            using (var session = DocumentStoreLocator.ResolveOrRoot(MessageBusSpecifierLocalConfig.HostConnectionString))
            {
                var spec = session.Load<MessageExchangeDeclaration>(exchangeSpec.Name);
                if (null == spec)
                    return;

                spec.Queues.Clear();
                spec.Queues.AddRange(exchangeSpec.Queues);
                spec.Type = exchangeSpec.Type;

                session.SaveChanges();
            }

            
        }

        internal MessageExchangeDeclaration RefreshExchangeSpec(string name)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(MessageBusSpecifierLocalConfig.HostConnectionString))
            {
                return session.Load<MessageExchangeDeclaration>(name);
            }

        }
    }


    public class RavenExchange : IExchangeSpecifier
    {
        private MessageExchangeDeclaration _declSpec;
        private readonly RavenMessageBus _parent;

        internal RavenExchange(RavenMessageBus parent, MessageExchangeDeclaration declSpec)
        {
            _parent = parent;
            _declSpec = declSpec;
        }

        #region IExchangeSpecifier Members

        public string Name
        {
            get { return _declSpec.Name; }
        }

        public ExchangeTypes ExchangeType
        {
            get { return _declSpec.Type; }
        }

        public IExchangeSpecifier DeclareQueue(string queueName, params string[] boundRoutes)
        {
            if (!_declSpec.Queues.Any(sq => sq.Name == queueName))
            {
                var q = new MessageQueueDeclaration {Name = queueName};
                q.Bindings.AddRange(boundRoutes);
                _declSpec.Queues.Add(q);
                _parent.UpdateExchange(_declSpec);
            }
            return this;
        }

        public IExchangeSpecifier DeclareQueue(Enum queueName, params string[] boundRoutes)
        {
            return DeclareQueue(queueName.EnumName(), boundRoutes);
        }

        public IExchangeSpecifier DeleteQueue(string queueName)
        {
            var item = _declSpec.Queues.FirstOrDefault(q => q.Name == queueName);
            if (null == item)
                return this;

            _declSpec.Queues.Remove(item);
            _parent.UpdateExchange(_declSpec);
            return this;
        }

        public IExchangeSpecifier DeleteQueue(Enum queueName)
        {
            return DeleteQueue(queueName.EnumName());
        }

        public IQueueSpecifier SpecifyQueue(string queueName)
        {
            var item = _declSpec.Queues.FirstOrDefault(q => q.Name == queueName);
            if (null == item)
                return null;

            return new RavenQueueSpecifier(item);
        }

        public IQueueSpecifier SpecifyQueue(Enum queueName)
        {
            return SpecifyQueue(queueName.EnumName());
        }


        public IEnumerable<IQueueSpecifier> Queues
        {
            get
            {
                _declSpec = _parent.RefreshExchangeSpec(_declSpec.Name);
                return _declSpec.Queues.Select(q => new RavenQueueSpecifier(q));
            }
        }

        #endregion
    }

    public class RavenQueueSpecifier : IQueueSpecifier
    {
        private readonly MessageQueueDeclaration _queueSpec;

        public RavenQueueSpecifier(MessageQueueDeclaration queueSpec)
        {
            _queueSpec = queueSpec;
        }

        #region IQueueSpecifier Members

        public string Name
        {
            get { return _queueSpec.Name; }
        }

        public IEnumerable<string> Bindings
        {
            get { return _queueSpec.Bindings; }
        }

        #endregion
    }

    public class DocumentMessageEnvelope
    {
        [DocumentIdentifier]
        public string Id { get; set; }

        public byte[] Data { get; set; }
        public DateTime CreationDate { get; set; }
        public Type MessageType { get; set; }
        public string QueueId { get; set; }
        public bool Reserved { get; set; }
        public DateTime ReservedTime { get; set; }
        public Guid Reserver { get; set; }

        public object Decode()
        {
            if (null == Data || Data.Length == 0)
                return null;

            var ms = new MemoryStream(Data);
            var br = new BsonReader(ms);
            var serializer = new JsonSerializer {TypeNameHandling = TypeNameHandling.All};
            var retval = serializer.Deserialize(br, MessageType);
            return retval;
        }
    } 

    public class RavenMessageOutbox 
    {
        
        private readonly ConcurrentQueue<object> _pending = new ConcurrentQueue<object>();
         
        private readonly object _sendLock = new object();
        private readonly JsonSerializer _serializer;
        private bool _isDisposed;
        private Timer _sendTimer;


        public RavenMessageOutbox(string name)
        {
            Name = name;
            _serializer = new JsonSerializer {TypeNameHandling = TypeNameHandling.All};
        }

        public RavenMessageOutbox()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var name = config[MessageBoxLocalConfig.Name];
            Name = name;
            _serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.All };
        }

        #region IMessageOutbox Members

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (null != _sendTimer)
            {
                _sendTimer.Dispose();
                _sendTimer = null;
            }

         
        }

        public string Name { get; set; }

        public long Capacity
        {
            get { return int.MaxValue; }
        }

        public bool Pending
        {
            get { return _pending.Any(); }
        }

        public void Enqueue<T>(T item)
        {
            _pending.Enqueue(item);
        }

        private const int BatchLimit = 100;
        public void Send()
        {
            lock (_sendLock)
            {
                while (_pending.Any())
                {
                    using (var session = DocumentStoreLocator.ResolveOrRoot(MessageBusSpecifierLocalConfig.HostConnectionString))
                    {
                        int batchCount = 0;
                        object item;
                        while (_pending.TryDequeue(out item) && batchCount < BatchLimit)
                        {
                       
                            var ms = new MemoryStream();
                            var writer = new BsonWriter(ms);
                            _serializer.Serialize(writer, item);
                            writer.Flush();

                            
                            session.Store(new DocumentMessageEnvelope
                                                {
                                                    CreationDate = DateTime.UtcNow,
                                                    Data = ms.ToArray(),
                                                    MessageType = item.GetType(),
                                                    QueueId = Name,
                                                    Reserved = false,
                                                    ReservedTime = DateTime.MinValue
                                                });

                            batchCount += 1;
                        }

                        session.SaveChanges();
                        
                    }


                }
            }
        }


        public void AutomaticSend()
        {
            AutomaticSend(TimeSpan.FromSeconds(5.0));
        }

        public void AutomaticSend(TimeSpan sendDuration)
        {
            if (null != _sendTimer)
                return;

            _sendTimer = new Timer(CheckSend, this, 0L, (long)sendDuration.TotalMilliseconds);
        }

        #endregion

        private static void CheckSend(object obj)
        {
            var that = (RavenMessageOutbox)obj;
            that.Send();
        }
    }


    public class RavenMessageInbox 
    {
       

        private readonly Guid _reservationId = Guid.NewGuid(); // yeah. Each inbox reserves messages

        public RavenMessageInbox(string name)
        {
            
            Name = name;
            _rnd = GoodSeedRandom.Create();
        }

        public RavenMessageInbox()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var name = config[MessageBoxLocalConfig.Name];
            
            Name = name;

            _rnd = GoodSeedRandom.Create();
        }

        #region IMessageInbox Members

       
        public string Name { get; set; }

        private static readonly TimeSpan CheckSpan = TimeSpan.FromSeconds(3.0);
        private readonly Random _rnd;

        private DocumentMessageEnvelope MaybeReserveOne()
        {
            using (var rd = DocumentStoreLocator.ResolveOrRoot(MessageBusSpecifierLocalConfig.HostConnectionString))
            {
                int retry = 0;

                DocumentMessageEnvelope chosen;
                do
                {
                    
                
                    chosen = (from dme in rd.Query<DocumentMessageEnvelope>()
                                  where dme.Reserved == false && dme.QueueId == Name
                                    orderby dme.CreationDate
                                  select dme
                                  ).FirstOrDefault();

                    if (null == chosen)
                        return null;

                    chosen.Reserved = true;
                    chosen.Reserver = _reservationId;
                    chosen.ReservedTime = DateTime.UtcNow;

                
                    try
                    {
                        rd.SaveChanges();
                    }
                    catch // collisions are pretty likely
                    {
                        retry += 1;
                    }

                } while (retry > 0 && retry < 5);

                chosen = (from dme in rd.Query<DocumentMessageEnvelope>()
                          where dme.Id == chosen.Id &&
                                dme.Reserver == _reservationId &&
                                dme.Reserved
                          select dme).FirstOrDefault();

                return chosen;

            }

        }

        private readonly TimeSpan _absoluteReservationLimit = TimeSpan.FromHours(24.0);
        private void DoHouseCleaning()
        {
            var earliestReservationTolerable = DateTime.UtcNow - _absoluteReservationLimit;
            using (var session = DocumentStoreLocator.ResolveOrRoot(MessageBusSpecifierLocalConfig.HostConnectionString))
            {
                var expired = 
                        (from dme in session.Query<DocumentMessageEnvelope>()
                          where
                              dme.Reserved 
                              && dme.ReservedTime < earliestReservationTolerable
                          select dme).Take(50).ToArray();
                if (expired.Any())
                {
                    foreach (var item in expired)
                        session.Delete(item);

                    session.SaveChanges();
                }
            }
        }

        public IEnumerable<DocumentMessageEnvelope> WaitForMessages(TimeSpan duration)
        {

            var timeout = DateTime.UtcNow + duration;
            DocumentMessageEnvelope reserved = null;
            while (DateTime.UtcNow < timeout && null == reserved)
            {
                reserved = MaybeReserveOne();
                if(null == reserved)
                    Thread.Sleep(CheckSpan);
            }

            var retval = new List<DocumentMessageEnvelope>();
            
            while (null != reserved)
            {
               retval.Add(reserved);
               reserved = MaybeReserveOne();
            }

            if (!retval.Any() || (_rnd.Next(100) < 10))
            {
                DoHouseCleaning();
            }

            return retval;
        }

        #endregion
    }
}