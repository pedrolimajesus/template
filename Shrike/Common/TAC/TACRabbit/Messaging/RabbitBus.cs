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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppComponents.Extensions.EnumEx;
using AppComponents.Messaging;
using AppComponents.Messaging.Declarations;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using RabbitMQ.Client;

namespace AppComponents.Rabbit
{
    public class RabbitMessageAcknowledge : IMessageAcknowledge
    {
        private readonly ulong _ackTag;
        private readonly RabbitListener _listener;

        public RabbitMessageAcknowledge(RabbitListener listener, ulong ackTag)
        {
            _ackTag = ackTag;
            _listener = listener;
        }

        #region IMessageAcknowledge Members

        public void MessageAcknowledged()
        {
            _listener.Ack(_ackTag);
        }

        public void MessageAbandoned()
        {
            _listener.Nack(_ackTag);
        }

        public void MessageRejected()
        {
            _listener.Rej(_ackTag);
        }

        #endregion
    }

    public class RabbitPublisher : IMessagePublisher
    {
        private readonly ConnectionFactory _cf;
        private readonly IModel _channel;
        private readonly IConnection _connection;
        private readonly string _exchangeName;
        private readonly IBasicProperties _props;
        private readonly JsonSerializer _serializer = new JsonSerializer();

        public RabbitPublisher()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var conn = config[MessagePublisherLocalConfig.HostConnectionString];
            _exchangeName = config[MessagePublisherLocalConfig.ExchangeName];

            _cf = new ConnectionFactory {Uri = conn};
            _connection = _cf.CreateConnection();
            _channel = _connection.CreateModel();

            var specifier = new MessageBusTopologySpecifier();
            var spex = specifier.SpecifyExchange(_exchangeName);
            if (spex == null)
                throw new ArgumentException(_exchangeName);

            _channel.ExchangeDeclare(_exchangeName, MessageBusTopologySpecifier.Translate(spex.ExchangeType));
            _props = _channel.CreateBasicProperties();
            _props.DeliveryMode = 2;
        }

        #region IMessagePublisher Members

        public void Send<T>(T msg, string routeKey)
        {
            var message = new MessageQueueEnvelope(msg);
            var ms = new MemoryStream();
            var writer = new BsonWriter(ms);
            _serializer.Serialize(writer, message);
            writer.Flush();
            _channel.BasicPublish(_exchangeName, routeKey, _props, ms.ToArray());
        }

        public void Send<T>(T msg, Enum routeKey)
        {
            Send(msg, routeKey.EnumName());
        }

        public void Dispose()
        {
            _channel.Close();
            _connection.Close();
        }

        #endregion
    }

    public class RabbitListener : IMessageListener
    {
        private readonly ConnectionFactory _cf;
        private readonly IModel _channel;
        private readonly IConnection _connection;
        private readonly string _exchangeName;
        private readonly string _queueName;
        private readonly JsonSerializer _serializer = new JsonSerializer();

        private readonly Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>> _sinks =
            new Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>>();

        private CancellationToken _ct;
        private CancellationTokenSource _cts;
        private bool _isDisposed;
        private Task _listening;

        public RabbitListener()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _exchangeName = config[MessageListenerLocalConfig.ExchangeName];
            _queueName = config[MessageListenerLocalConfig.QueueName];
            var conn = config[MessageListenerLocalConfig.HostConnectionString];
            _cf = new ConnectionFactory {Uri = conn};
            _connection = _cf.CreateConnection();
            _channel = _connection.CreateModel();

            var specifier = new MessageBusTopologySpecifier();
            var spex = specifier.SpecifyExchange(_exchangeName);
            if (spex == null)
                throw new ArgumentException(_exchangeName);

            _channel.ExchangeDeclare(_exchangeName, MessageBusTopologySpecifier.Translate(spex.ExchangeType));
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

                _listening.Wait();
                _cts.Dispose();
                _listening.Dispose();

                _channel.Close();
                _connection.Close();
            }
        }

        #endregion

        public void Ack(ulong tag)
        {
            lock (_channel)
                _channel.BasicAck(tag, false);
        }

        public void Nack(ulong tag)
        {
            lock (_channel)
                _channel.BasicNack(tag, false, true);
        }

        public void Rej(ulong tag)
        {
            lock (_channel)
                _channel.BasicReject(tag, false);
        }

        private static void Listen(object that)
        {
            var @this = (RabbitListener) that;

            while (!@this._ct.IsCancellationRequested)
            {
                BasicGetResult res;
                do
                {
                    lock (@this._channel)
                        res = @this._channel.BasicGet(@this._queueName, false);

                    if (null != res)
                    {
                        byte[] body = res.Body;
                        MessageQueueEnvelope env;
                        using (var ms = new MemoryStream(body))
                        {
                            var br = new BsonReader(ms);

                            env =
                                (MessageQueueEnvelope) @this._serializer.Deserialize(br, typeof (MessageQueueEnvelope));
                        }

                        var msg = env.Decode();

                        if (@this._sinks.ContainsKey(env.MessageType))
                        {
                            var sink = @this._sinks[env.MessageType];
                            sink(msg, @this._cts.Token, new RabbitMessageAcknowledge(@this, res.DeliveryTag));
                        }
                    }
                } while (res != null);


                @this._ct.WaitHandle.WaitOne(1000);
            }
        }
    }
}