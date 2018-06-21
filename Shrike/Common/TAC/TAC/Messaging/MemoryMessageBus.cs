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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppComponents.Extensions.EnumEx;
using AppComponents.Messaging.Declarations;
using AppComponents.Extensions.ExceptionEx;

namespace AppComponents.Messaging
{
    public class MemMessageListener : IMessageListener
    {
        private readonly Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>> _sinks =
            new Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>>();

        private readonly IMessageBusSpecifier _bus;

        private CancellationToken _ct;
        private CancellationTokenSource _cts;
        private bool _isDisposed;
        private Task _listening;
        private readonly ManualResetEventSlim _sev;

        private readonly ConcurrentQueue<LightMessageQueueEnvelope> _q;

        public MemMessageListener()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var exName = config[MessageListenerLocalConfig.ExchangeName];
            var qName = config[MessageListenerLocalConfig.QueueName];

            _bus = MemMessageBus.Instance();
            var qs = _bus.SpecifyExchange(exName).SpecifyQueue(qName);
            _q = ((IMemQueueAccess)qs).Queue;
            _sev = ((IMemQueueAccess) qs).SentEvent;
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
            var @this = (MemMessageListener)that;
            var log = ClassLogger.Create(typeof(MemMessageListener));
            bool first = true;
            try
            {
                while (!@this._ct.IsCancellationRequested)
                {
                    bool sent = true;
                    
                    {
                        first = false;
                        sent = @this._sev.Wait(60000, @this._ct);
                    }

                    if (sent && !@this._ct.IsCancellationRequested)
                    {

                        try
                        {
                            LightMessageQueueEnvelope env;
                            var messageRead = @this._q.TryDequeue(out env);
                            while (messageRead)
                            {
                                var msg = env.Decode();

                                if (@this._sinks.ContainsKey(env.MessageType))
                                {
                                    var sink = @this._sinks[env.MessageType];
                                    sink(msg, @this._cts.Token, new MemQueueAcknowledge(@this, env));
                                }

                                messageRead = @this._q.TryDequeue(out env);
                            }
                        }
                        catch (Exception ex)
                        {
                            var es = ex.TraceInformation();
                            log.Error(es);
                        }

                        @this._sev.Reset();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                
            }

        }

        internal void Reenqueue(LightMessageQueueEnvelope messageQueue)
        {
            _q.Enqueue(messageQueue);
            (_q as IMemQueueAccess).SentEvent.Set();
        }
    }

    internal class MemQueueAcknowledge : IMessageAcknowledge
    {
        private readonly LightMessageQueueEnvelope _messageQueue;
        private readonly MemMessageListener _parent;

        public MemQueueAcknowledge(MemMessageListener parent, LightMessageQueueEnvelope messageQueue)
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

    public class MemMessagePublisher : IMessagePublisher
    {
        private readonly string _exchangeName;
        private readonly IMessageBusSpecifier _bus;
        private ExchangeTypes _exchangeType;
        private bool _isDisposed;
        private List<IQueueSpecifier> _queues;


        public MemMessagePublisher()
        {
            var cfg = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _exchangeName = cfg[MessagePublisherLocalConfig.ExchangeName];

            _bus = MemMessageBus.Instance();
        }

        #region IMessagePublisher Members

        public void Send<T>(T msg, string routeKey)
        {
            RefreshQueues();

            var matchingQueues = MessageExchangeDeclaration.BindMessageToQueues(routeKey, _exchangeType, _queues);
            foreach (var q in matchingQueues)
            {
                var queue = _queues.FirstOrDefault(s => s.Name == q);
                var outbox = queue as IMemQueueAccess;
                if (null != outbox)
                {
                    outbox.Queue.Enqueue(new LightMessageQueueEnvelope(msg));
                    outbox.SentEvent.Set();
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


    public class MemMessageBus : IMessageBusSpecifier
    {
        private static readonly ConcurrentDictionary<string, MemExchange> Exchanges =
            new ConcurrentDictionary<string, MemExchange>();

        private static readonly object InitLock = new object();
        private static volatile MemMessageBus _instance;
        private bool _isDisposed;

        private MemMessageBus()
        {
        }

        #region IMessageBusSpecifier Members

        public IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType)
        {
            if (!Exchanges.ContainsKey(exchangeName))
            {
                Exchanges.TryAdd(exchangeName,
                                 new MemExchange(this,
                                                 new MessageExchangeDeclaration
                                                     {Name = exchangeName, Type = exchangeType}));
            }
            return this;
        }

        public IMessageBusSpecifier DeclareExchange(Enum exchangeName, ExchangeTypes exchangeType)
        {
            return DeclareExchange(exchangeName.EnumName(), exchangeType);
        }

        public IMessageBusSpecifier DeleteExchange(string exchangeName)
        {
            MemExchange removed;
            Exchanges.TryRemove(exchangeName, out removed);

            return this;
        }

        public IMessageBusSpecifier DeleteExchange(Enum exchangeName)
        {
            return DeleteExchange(exchangeName.EnumName());
        }

        public IExchangeSpecifier SpecifyExchange(string exchangeName)
        {
            MemExchange me;
            Exchanges.TryGetValue(exchangeName, out me);

            return me;
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

        public static MemMessageBus Instance()
        {
            if (null == _instance)
            {
                lock (InitLock)
                {
                    if (null == _instance)
                    {
                        _instance = new MemMessageBus();
                    }
                }
            }

            return _instance;
        }
    }


    internal class MemExchange : IExchangeSpecifier
    {
        private readonly MessageExchangeDeclaration _declSpec;
        private readonly MemMessageBus _parent;
        private readonly ConcurrentDictionary<string, MemQueueSpecifier> _queueSpecifiers = new ConcurrentDictionary<string, MemQueueSpecifier>(); 

        internal MemExchange(MemMessageBus parent, MessageExchangeDeclaration declSpec)
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
            if (_declSpec.Queues.Any(dsq => dsq.Name == queueName))
            {

                return this;
            }

            var q = new MessageQueueDeclaration { Name = queueName };
            q.Bindings.AddRange(boundRoutes);
            _declSpec.Queues.Add(q);

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
            MemQueueSpecifier mqs;
            if(_queueSpecifiers.TryRemove(queueName, out mqs))
                mqs.Dispose();

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

            return GetCachedSpecifier(item);
        }

        public IQueueSpecifier SpecifyQueue(Enum queueName)
        {
            return SpecifyQueue(queueName.EnumName());
        }


        public IEnumerable<IQueueSpecifier> Queues
        {
            get { return _declSpec.Queues.Select(GetCachedSpecifier); }
        }

        private MemQueueSpecifier GetCachedSpecifier( MessageQueueDeclaration queue )
        {
            MemQueueSpecifier mqs;
            if (!_queueSpecifiers.TryGetValue(queue.Name, out mqs))
            {
                mqs = new MemQueueSpecifier(_parent, _declSpec, queue);
                if (!_queueSpecifiers.TryAdd(queue.Name, mqs))
                {
                    
                    mqs = _queueSpecifiers[queue.Name];
                }
            }

            return mqs;
        }

        #endregion
    }
    
    internal interface IMemQueueAccess
    {
        ConcurrentQueue<LightMessageQueueEnvelope> Queue { get; }
        ManualResetEventSlim SentEvent { get;  }
    }

    internal class MemQueueSpecifier : IQueueSpecifier, IMemQueueAccess, IDisposable
    {
        // ReSharper disable NotAccessedField.Local
        private readonly MessageExchangeDeclaration _declSpec;
        private readonly MemMessageBus _parent;
        private readonly ManualResetEventSlim _queueEvent;

        // ReSharper restore NotAccessedField.Local

        private readonly ConcurrentQueue<LightMessageQueueEnvelope> _queue = new ConcurrentQueue<LightMessageQueueEnvelope>();
        private readonly MessageQueueDeclaration _queueSpec;

        public MemQueueSpecifier(MemMessageBus parent, MessageExchangeDeclaration declSpec,
                                 MessageQueueDeclaration queueSpec)
        {
            _parent = parent;
            _declSpec = declSpec;
            _queueSpec = queueSpec;
            _queueEvent = new ManualResetEventSlim(false);
        }

        #region IMemQueueAccess Members

        public ConcurrentQueue<LightMessageQueueEnvelope> Queue
        {
            get { return _queue; }
        }

        public ManualResetEventSlim SentEvent { get { return _queueEvent; } }

        #endregion

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

        private bool _isDisposed = false;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _queueEvent.Dispose();
            }
            
        }
    }

    internal class LightMessageQueueEnvelope
    {


        public LightMessageQueueEnvelope(object data)
        {
            Data = data;
            MessageType = data.GetType();
        }

        public Type MessageType { get; set; }
        public object Data { get; set; }

        public object Decode()
        {
            return Data;
        }
    }
}