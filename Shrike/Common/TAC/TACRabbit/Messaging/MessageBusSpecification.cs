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
using AppComponents.Extensions.EnumEx;
using AppComponents.Messaging.Declarations;
using AppComponents.Data;

namespace AppComponents.Messaging
{
    

    internal class QueueSpecifier : IQueueSpecifier
    {
        private MessageQueueDeclaration _messageQueueDeclaration;

        public QueueSpecifier(MessageQueueDeclaration mqd)
        {
            _messageQueueDeclaration = mqd;
        }

        #region IQueueSpecifier Members

        public string Name
        {
            get { return _messageQueueDeclaration.Name; }
        }

        public IEnumerable<string> Bindings
        {
            get { return _messageQueueDeclaration.Bindings; }
        }

        #endregion
    }


    internal class ExchangeTopologySpecifier : IExchangeSpecifier
    {
        private MessageExchangeDeclaration _exchangeDeclaration;
        private MessageBusTopologySpecifier _messageBusTopologySpecifier;

        public ExchangeTopologySpecifier(MessageBusTopologySpecifier mbts, MessageExchangeDeclaration exd)
        {
            _exchangeDeclaration = exd;
            _messageBusTopologySpecifier = mbts;
        }

        #region IExchangeSpecifier Members

        public string Name
        {
            get { return _exchangeDeclaration.Name; }
        }

        public ExchangeTypes ExchangeType
        {
            get { return _exchangeDeclaration.Type; }
        }

        public IExchangeSpecifier DeclareQueue(string queueName, params string[] boundRoutes)
        {
            var rep = DataRepositoryServiceFactory.CreateSimple<MessageExchangeDeclaration>();
            _exchangeDeclaration = rep.Load(_exchangeDeclaration.Name).Item;
            if (null != _exchangeDeclaration)
            {
                if (!_exchangeDeclaration.Queues.Any(q => q.Name == queueName))
                {
                    var mqd = new MessageQueueDeclaration
                    {
                        Name = queueName,
                        Bindings = boundRoutes.ToList()
                    };

                    _exchangeDeclaration.Queues.Add(mqd);
                    rep.Update(_exchangeDeclaration);
                }
            }
            
            return this;
        }

        public IExchangeSpecifier DeclareQueue(Enum queueName, params string[] boundRoutes)
        {
            return DeclareQueue(queueName.EnumName(), boundRoutes);
        }

        public IExchangeSpecifier DeleteQueue(string queueName)
        {
            var rep = DataRepositoryServiceFactory.CreateSimple<MessageExchangeDeclaration>();
            var qrep = DataRepositoryServiceFactory.CreateSimple<MessageQueueDeclaration>();

            _exchangeDeclaration = rep.Load(_exchangeDeclaration.Name).Item;
            if (null != _exchangeDeclaration)
            {
                 var q = _exchangeDeclaration.Queues.FirstOrDefault(tq => tq.Name == queueName);
                if (null != q)
                {
                    _exchangeDeclaration.Queues.Remove(q);
                    rep.Update(_exchangeDeclaration);
                    qrep.Delete(q);
                }

            }

            
            // todo: rabbit delete exchange

            return this;
        }

        public IExchangeSpecifier DeleteQueue(Enum queueName)
        {
            return DeleteQueue(queueName.EnumName());
        }

        public IQueueSpecifier SpecifyQueue(string queueName)
        {
            var item = _exchangeDeclaration.Queues.FirstOrDefault(q => q.Name == queueName);
            if (null != item)
                return new QueueSpecifier(item);

            return null;
        }

        public IQueueSpecifier SpecifyQueue(Enum queueName)
        {
            return SpecifyQueue(queueName.EnumName());
        }

        public IEnumerable<IQueueSpecifier> Queues
        {
            get { foreach (var item in _exchangeDeclaration.Queues) yield return new QueueSpecifier(item); }
        }

        #endregion
    }


    public class MessageBusTopologySpecifier : IMessageBusSpecifier
    {
        #region IMessageBusSpecifier Members

        public IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType)
        {
            var rep = DataRepositoryServiceFactory.CreateSimple<MessageExchangeDeclaration>();
            rep.CreateNew(new MessageExchangeDeclaration {Name = exchangeName, Type = exchangeType});
                
            return this;
        }

        public IMessageBusSpecifier DeclareExchange(Enum exchangeName, ExchangeTypes exchangeType)
        {
            return DeclareExchange(exchangeName.EnumName(), exchangeType);
        }

        public IMessageBusSpecifier DeleteExchange(string exchangeName)
        {
            var rep = DataRepositoryServiceFactory.CreateSimple<MessageExchangeDeclaration>();
            var ex = rep.Load(exchangeName);
            if(null != ex)
                rep.Delete(ex.Item);
            
            return this;
        }

        public IMessageBusSpecifier DeleteExchange(Enum exchangeName)
        {
            return DeleteExchange(exchangeName.EnumName());
        }

        public IExchangeSpecifier SpecifyExchange(string exchangeName)
        {
            var rep = DataRepositoryServiceFactory.CreateSimple<MessageExchangeDeclaration>();
            var ex = rep.Load(exchangeName);
            if(null != ex)
                {
                    return new ExchangeTopologySpecifier(this, ex.Item);
                }
            
            return null;
        }

        public IExchangeSpecifier SpecifyExchange(Enum exchangeName)
        {
            return SpecifyExchange(exchangeName.EnumName());
        }

        public void Dispose()
        {
        }

        #endregion





        internal static string Translate(ExchangeTypes exchangeTypes)
        {
            return exchangeTypes.EnumName();
        }
    }
}