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
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppComponents.Data;
using AppComponents.Extensions.EnumEx;
using AppComponents.Messaging.Declarations;

namespace AppComponents.Messaging
{
    public class IpcMessageListener : IMessageListener
    {
        private readonly MemoryMappedTransferInbox _q;
        private readonly MemoryMappedTransferOutbox _req;

        private readonly Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>> _sinks =
            new Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>>();

        private CancellationToken _ct;
        private CancellationTokenSource _cts;
        private bool _isDisposed;
        private Task _listening;


        public IpcMessageListener()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _q = new MemoryMappedTransferInbox(config[MessageListenerLocalConfig.QueueName]);
            _req = new MemoryMappedTransferOutbox(config[MessageListenerLocalConfig.QueueName]);
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
                _q.Dispose();
                _req.Dispose();
            }
        }

        #endregion

        private static void Listen(object that)
        {
            var @this = (IpcMessageListener) that;

            while (! @this._ct.IsCancellationRequested)
            {
                var messages = @this._q.WaitForMessages<MessageQueueEnvelope>(TimeSpan.FromSeconds(10.0));
                if (messages.Any())
                {
                    foreach (var env in messages)
                    {
                        var msg = env.Decode();

                        if (@this._sinks.ContainsKey(env.MessageType))
                        {
                            var sink = @this._sinks[env.MessageType];
                            sink(msg, @this._cts.Token, new IpcQueueAcknowledge(@this, env));
                        }
                    }
                }
            }
        }

        internal void Reenqueue(MessageQueueEnvelope messageQueue)
        {
            _req.Enqueue(messageQueue);
            _req.Send();
        }
    }

    internal class IpcQueueAcknowledge : IMessageAcknowledge
    {
        private MessageQueueEnvelope _messageQueue;
        private IpcMessageListener _parent;

        public IpcQueueAcknowledge(IpcMessageListener parent, MessageQueueEnvelope messageQueue)
        {
            _parent = parent;
            _messageQueue = messageQueue;
        }

        #region IMessageAcknowledge Members

        public void MessageAcknowledged()
        {
        }

        public void MessageAbandoned()
        {
            _parent.Reenqueue(_messageQueue);
        }

        public void MessageRejected()
        {
        }

        #endregion
    }

    public class IpcMessagePublisher : IMessagePublisher
    {
        private readonly IpcMessageBus _bus;
        private readonly string _exchangeName;
        private ExchangeTypes _exchangeType;
        private bool _isDisposed;
        private List<IQueueSpecifier> _queues;


        public IpcMessagePublisher()
        {
            var cfg = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var conn = cfg[MessagePublisherLocalConfig.HostConnectionString];
            _exchangeName = cfg[MessagePublisherLocalConfig.ExchangeName];

            _bus = Catalog.Preconfigure()
                .Add(MessageBusSpecifierLocalConfig.HostConnectionString, conn)
                .ConfiguredCreate(() => new IpcMessageBus());
        }

        #region IMessagePublisher Members

        public void Send<T>(T msg, string routeKey)
        {
            RefreshQueues();

            var matchingQueues = MessageExchangeDeclaration.BindMessageToQueues(routeKey, _exchangeType, _queues);
            foreach (var q in matchingQueues)
            {
                using (var outbox = new MemoryMappedTransferOutbox(q))
                {
                    outbox.Enqueue(new MessageQueueEnvelope(msg));
                    outbox.Send();
                }
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


    public class IpcMessageBus : IMessageBusSpecifier
    {
        private readonly StructuredDataClient _client;
        private bool _isDisposed;


        public IpcMessageBus()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var connectionString = config[MessageBusSpecifierLocalConfig.HostConnectionString];


            _client = Catalog.Preconfigure()
                .Add(StructuredDataClientLocalConfig.Inbox, new MemoryMappedTransferInbox(Guid.NewGuid().ToString()))
                .Add(StructuredDataClientLocalConfig.Server, new MemoryMappedTransferOutbox(connectionString))
                .ConfiguredCreate(() => new StructuredDataClient());
        }

        #region IMessageBusSpecifier Members

        public IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType)
        {
            try
            {
                var exchanges = _client.OpenTable<MessageExchangeDeclaration>(AMQPDeclarations.MessageExchange);
                using (_client.BeginTransaction())
                {
                    var check = exchanges.Fetch(exchangeName);
                    if (!check.Any(d=>d.Key==exchangeName))
                    {

                        exchanges.AddNew(exchangeName,
                                         new MessageExchangeDeclaration {Name = exchangeName, Type = exchangeType});
                        _client.Commit();
                    }
                }
            }
            catch (AggregateException aex)
            {
                if (! (aex.InnerExceptions.Any(ex => ex is DBConcurrencyException)))
                    throw;
            }
            return this;
        }

        public IMessageBusSpecifier DeclareExchange(Enum exchangeName, ExchangeTypes exchangeType)
        {
            return DeclareExchange(exchangeName.EnumName(), exchangeType);
        }

        public IMessageBusSpecifier DeleteExchange(string exchangeName)
        {
            var exchanges = _client.OpenTable<MessageExchangeDeclaration>(AMQPDeclarations.MessageExchange);
            using (_client.BeginTransaction())
            {
                 var check = exchanges.Fetch(exchangeName);
                 if (!check.Any(d => d.Key == exchangeName))
                 {
                     exchanges.Delete(exchangeName);
                     _client.Commit();
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
            var exchanges = _client.OpenTable<MessageExchangeDeclaration>(AMQPDeclarations.MessageExchange);
            var spec = exchanges.Fetch(exchangeName).SingleOrDefault();

            if (spec != null)
            {
                return new IpcExchange(this, spec);
            }
            return null;
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

        internal void UpdateExchange(ReadAtom<MessageExchangeDeclaration> exchangeSpec)
        {
            var exchanges = _client.OpenTable<MessageExchangeDeclaration>(AMQPDeclarations.MessageExchange);
            using (_client.BeginTransaction())
            {
                 var check = exchanges.Fetch(exchangeSpec.Key);
                 if (!check.Any(d => d.Key == exchangeSpec.Key))
                 {
                     exchanges.Update(exchangeSpec.Key, exchangeSpec.Data, exchangeSpec.ETag);
                     _client.Commit();
                 }
            }
        }
    }


    internal class IpcExchange : IExchangeSpecifier
    {
        private readonly ReadAtom<MessageExchangeDeclaration> _declSpec;
        private readonly IpcMessageBus _parent;

        internal IpcExchange(IpcMessageBus parent, ReadAtom<MessageExchangeDeclaration> declSpec)
        {
            _parent = parent;
            _declSpec = declSpec;
        }

        #region IExchangeSpecifier Members

        public string Name
        {
            get { return _declSpec.Data.Name; }
        }

        public ExchangeTypes ExchangeType
        {
            get { return _declSpec.Data.Type; }
        }

        public IExchangeSpecifier DeclareQueue(string queueName, params string[] boundRoutes)
        {
            var q = new MessageQueueDeclaration {Name = queueName};
            q.Bindings.AddRange(boundRoutes);
            _declSpec.Data.Queues.Add(q);
            _parent.UpdateExchange(_declSpec);
            return this;
        }

        public IExchangeSpecifier DeclareQueue(Enum queueName, params string[] boundRoutes)
        {
            return DeclareQueue(queueName.EnumName(), boundRoutes);
        }

        public IExchangeSpecifier DeleteQueue(string queueName)
        {
            var item = _declSpec.Data.Queues.FirstOrDefault(q => q.Name == queueName);
            if (null == item)
                return this;

            _declSpec.Data.Queues.Remove(item);
            _parent.UpdateExchange(_declSpec);
            return this;
        }

        public IExchangeSpecifier DeleteQueue(Enum queueName)
        {
            return DeleteQueue(queueName.EnumName());
        }

        public IQueueSpecifier SpecifyQueue(string queueName)
        {
            var item = _declSpec.Data.Queues.FirstOrDefault(q => q.Name == queueName);
            if (null == item)
                return null;

            return new IpcQueueSpecifier(_parent, _declSpec, item);
        }

        public IQueueSpecifier SpecifyQueue(Enum queueName)
        {
            return SpecifyQueue(queueName.EnumName());
        }


        public IEnumerable<IQueueSpecifier> Queues
        {
            get { return _declSpec.Data.Queues.Select(q => new IpcQueueSpecifier(_parent, _declSpec, q)); }
        }

        #endregion
    }

    internal class IpcQueueSpecifier : IQueueSpecifier
    {
        // ReSharper disable NotAccessedField.Local
        private readonly ReadAtom<MessageExchangeDeclaration> _declSpec;
        private readonly IpcMessageBus _parent;
        // ReSharper restore NotAccessedField.Local

        private readonly MessageQueueDeclaration _queueSpec;

        public IpcQueueSpecifier(IpcMessageBus parent, ReadAtom<MessageExchangeDeclaration> declSpec,
                                 MessageQueueDeclaration queueSpec)
        {
            _parent = parent;
            _declSpec = declSpec;
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
}