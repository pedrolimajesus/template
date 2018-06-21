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
using System.Threading;

namespace AppComponents
{
    // message queueing based on AMQP constructs


    /// <summary>
    ///   Message consumer preconfiguration keys
    /// </summary>
    public enum MessageListenerLocalConfig
    {
        /// <summary>
        ///   String describing how to connect to queue exchange host.
        /// </summary>
        HostConnectionString,

        /// <summary>
        ///   Name of exchange to connect to
        /// </summary>
        ExchangeName,

        /// <summary>
        ///   Name of queue to bind to
        /// </summary>
        QueueName
    }


    /// <summary>
    ///   This interface is sent to message listeners, so that they may call back and release control over a message
    /// </summary>
    public interface IMessageAcknowledge
    {
        /// <summary>
        ///   If a listener calls this method, it means that it has processed the queue message and it may be taken off of the queue.
        /// </summary>
        void MessageAcknowledged();


        /// <summary>
        ///   If a listener calls this method, it is to be kept on the queue.
        /// </summary>
        void MessageAbandoned();

        /// <summary>
        ///   If a listen calls this method, then the message cannot be processed.
        /// </summary>
        void MessageRejected();
    }

    /// <summary>
    ///   Listener objects received queue messages, filtered by message type
    /// </summary>
    public interface IMessageListener : IDisposable
    {
        /// <summary>
        ///   Registers a listener of a message queue
        /// </summary>
        /// <param name="listener"> Filtering by message type, a delegate that processes a message </param>
        void Listen(params KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>[] listener);
    }

    public enum MessageFetcherLocalConfig
    {
        /// <summary>
        ///   String describing how to connect to queue exchange host.
        /// </summary>
        HostConnectionString,

        /// <summary>
        ///   Name of exchange to connect to
        /// </summary>
        ExchangeName,

        /// <summary>
        ///   Name of queue to bind to
        /// </summary>
        QueueName
    }

    public interface IMessageFetcher
    {
        IEnumerable<object> FetchMessages();
        IEnumerable<T> Fetch<T>();
    }

    /// <summary>
    ///   Message publisher preconfiguration keys
    /// </summary>
    public enum MessagePublisherLocalConfig
    {
        /// <summary>
        ///   String describing how to connect to queue exchange host.
        /// </summary>
        HostConnectionString,

        /// <summary>
        ///   Name of exchange to connect to
        /// </summary>
        ExchangeName
    }

    /// <summary>
    ///   Publisher objects send messages onto a queue
    /// </summary>
    public interface IMessagePublisher : IDisposable
    {
        /// <summary>
        ///   sends a message onto a queue
        /// </summary>
        /// <typeparam name="T"> Message class type </typeparam>
        /// <param name="msg"> message to be serialized and enqueued on the exchange </param>
        /// <param name="routeKey"> Routekey defining how the exchange is to match the message to the queue </param>
        void Send<T>(T msg, string routeKey);

        /// <summary>
        ///   sends a message onto a queue
        /// </summary>
        /// <typeparam name="T"> Message class type </typeparam>
        /// <param name="msg"> message to be serialized and enqueued on the exchange </param>
        /// <param name="routeKey"> Routekey defining how the exchange is to match the message to the queue </param>
        void Send<T>(T msg, Enum routeKey);
    }


    /// <summary>
    ///   Defines a message queue
    /// </summary>
    public interface IQueueSpecifier
    {
        /// <summary>
        ///   The name of the queue
        /// </summary>
        string Name { get; }

        /// <summary>
        ///   Queue message bindings. In a direct exchange, messages with matching keys are put in this queue. In a fanout exchange, bindings are ignored, and this queue will receive all messages. In a topic exchange, the bindings are wildcard strings that describe which messages the queue will receive
        /// </summary>
        IEnumerable<string> Bindings { get; }
    }

    /// <summary>
    ///   Describes the types of message exchanges
    /// </summary>
    public enum ExchangeTypes
    {
        /// <summary>
        ///   In a direct exchange, messages are routed by route key match to queue binding.
        /// </summary>
        Direct,

        /// <summary>
        ///   In a fanout exchange, each message is fed to every queue in the exchange
        /// </summary>
        Fanout,

        /// <summary>
        ///   In a topic exchange, the message route key is matched to wildcard queue bindings and routed to the appropriate queue
        /// </summary>
        Topic
    }

    /// <summary>
    ///   Defines a message exchange, declares queues
    /// </summary>
    public interface IExchangeSpecifier
    {
        /// <summary>
        ///   Name of the exchange
        /// </summary>
        string Name { get; }

        /// <summary>
        ///   Type of the exchange, defining its message routing behavior
        /// </summary>
        ExchangeTypes ExchangeType { get; }

        IEnumerable<IQueueSpecifier> Queues { get; }

        /// <summary>
        ///   Declares a message queue in this exchange
        /// </summary>
        /// <param name="queueName"> the queue name </param>
        /// <param name="boundRoutes"> the queue route parameters </param>
        /// <returns> this </returns>
        IExchangeSpecifier DeclareQueue(string queueName, params string[] boundRoutes);

        /// <summary>
        ///   Declares a message queue in this exchange
        /// </summary>
        /// <param name="queueName"> the queue name </param>
        /// <param name="boundRoutes"> the queue route parameters </param>
        /// <returns> this </returns>
        IExchangeSpecifier DeclareQueue(Enum queueName, params string[] boundRoutes);


        /// <summary>
        ///   Deletes a queue from the exchange
        /// </summary>
        /// <param name="queueName"> Name of the queue to delete </param>
        /// <returns> this </returns>
        IExchangeSpecifier DeleteQueue(string queueName);

        /// <summary>
        ///   Deletes a queue from the exchange
        /// </summary>
        /// <param name="queueName"> Name of the queue to delete </param>
        /// <returns> this </returns>
        IExchangeSpecifier DeleteQueue(Enum queueName);

        /// <summary>
        ///   Accesses the queue specifier
        /// </summary>
        /// <param name="queueName"> name of the queue </param>
        /// <returns> a queue specifier </returns>
        IQueueSpecifier SpecifyQueue(string queueName);

        /// <summary>
        ///   Accesses the queue specifier
        /// </summary>
        /// <param name="queueName"> name of the queue </param>
        /// <returns> a queue specifier </returns>
        IQueueSpecifier SpecifyQueue(Enum queueName);
    }


    /// <summary>
    ///   Preconfiguration keys for message bus.
    /// </summary>
    public enum MessageBusSpecifierLocalConfig
    {
        /// <summary>
        ///   connection string describing how to connect to the message bus host.
        /// </summary>
        HostConnectionString
    }

    /// <summary>
    ///   Specifies the exchanges in the message bus
    /// </summary>
    public interface IMessageBusSpecifier : IDisposable
    {
        /// <summary>
        ///   Declares a message exchange
        /// </summary>
        /// <param name="exchangeName"> exchange name </param>
        /// <param name="exchangeType"> exchange type </param>
        /// <returns> this </returns>
        IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType);

        /// <summary>
        ///   Declares a message exchange
        /// </summary>
        /// <param name="exchangeName"> exchange name </param>
        /// <param name="exchangeType"> exchange type </param>
        /// <returns> </returns>
        IMessageBusSpecifier DeclareExchange(Enum exchangeName, ExchangeTypes exchangeType);

        /// <summary>
        ///   Deletes a message exchange
        /// </summary>
        /// <param name="exchangeName"> exchange name </param>
        /// <returns> this </returns>
        IMessageBusSpecifier DeleteExchange(string exchangeName);

        /// <summary>
        ///   Deletes a message exchange
        /// </summary>
        /// <param name="exchangeName"> exchange name </param>
        /// <returns> this </returns>
        IMessageBusSpecifier DeleteExchange(Enum exchangeName);

        /// <summary>
        ///   Provides access to specify exchange queues.
        /// </summary>
        /// <param name="exchangeName"> The exchange name. </param>
        /// <returns> An exchange specifier </returns>
        IExchangeSpecifier SpecifyExchange(string exchangeName);

        /// <summary>
        ///   Provides access to specify exchange queues.
        /// </summary>
        /// <param name="exchangeName"> The exchange name. </param>
        /// <returns> An exchange specifier </returns>
        IExchangeSpecifier SpecifyExchange(Enum exchangeName);
    }

    /// <summary>
    /// When the message bus is referenced, any classes in the loaded assemblys that derive from this one will be created, 
    /// given the message bus specifier, then receive a Bootstrap() call.
    /// </summary>
    public abstract class AbstractMessageBusSpecificationBootstrapper
    {
        /// <summary>
        ///   Receives the message bus specifer
        /// </summary>
        public IMessageBusSpecifier Specifier { get; set; }

        /// <summary>
        ///   Called in derived classes when the message bus is referenced
        /// </summary>
        public abstract void Bootstrap();
    }
}