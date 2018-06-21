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
using System.Globalization;
using System.IO;
using System.Linq;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.RandomNumbers;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace AppComponents.Messaging.Declarations
{
    public enum AMQPDeclarations
    {
        MessageExchange
    }

    public class MessageQueueDeclaration
    {
        public MessageQueueDeclaration()
        {
            Bindings = new List<string>();
        }

        [DocumentIdentifier]
        public string Name { get; set; }

        public List<string> Bindings { get; set; }
    }

    public class MessageExchangeDeclaration
    {
        public MessageExchangeDeclaration()
        {
            Queues = new List<MessageQueueDeclaration>();
        }

        [DocumentIdentifier]
        public string Name { get; set; }

        public ExchangeTypes Type { get; set; }

        public List<MessageQueueDeclaration> Queues { get; private set; }

        public static IEnumerable<string> BindMessageToQueues(string key, ExchangeTypes ex,
                                                              IEnumerable<IQueueSpecifier> qs)
        {
            switch (ex)
            {
                case ExchangeTypes.Direct:
                    {
                        var routeBound =
                            qs.Where(
                                q =>
                                !q.Bindings.Any() ||
                                q.Bindings.Any(k => string.Compare(key, k, true, CultureInfo.InvariantCulture) == 0));

                        if (routeBound.Count() == 1)
                            return EnumerableEx.OfOne(routeBound.First().Name);

                        if (routeBound.Any())
                        {
                            var matches = (from q in routeBound
                                           select q.Name).Distinct().Shuffle(GoodSeedRandom.Create());


                            return matches.Take(1);
                        }

                        return Enumerable.Empty<string>();
                    }
                    //break;

                case ExchangeTypes.Fanout:
                    {
                        return qs.Select(q => q.Name);
                    }
                    //break;

                case ExchangeTypes.Topic:
                    {
                        return (from q in qs
                                where q.Bindings.Any(k => TopicMatch(key, k))
                                select q.Name).Distinct();
                    }
                    //break;
            }

            return Enumerable.Empty<string>();
        }

        public static bool TopicMatch(string messageKey, string binding)
        {
            if (string.IsNullOrEmpty(messageKey) || string.IsNullOrEmpty(binding))
                return false;

            if (binding == "#")
                return true;

            var keyParts = messageKey.Split('.');
            var bindingParts = messageKey.Split('.');


            for (var eachPart = 0; eachPart != keyParts.Length; eachPart++)
            {
                var key = keyParts[eachPart];

                if (eachPart > bindingParts.Length)
                    return false;

                var binder = bindingParts[eachPart];
                if (binder == "*")
                    continue;

                if (binder == "#")
                    break;

                if (string.Compare(binder, key, false, CultureInfo.InvariantCulture) != 0)
                    return false;
            }

            return true;
        }
    }

    public class MessageQueueEnvelope
    {
        private readonly JsonSerializer _serializer = new JsonSerializer();

        public MessageQueueEnvelope()
        {
        }

        public MessageQueueEnvelope(object data)
        {
            var ms = new MemoryStream();
            var bsw = new BsonWriter(ms);
            MessageType = data.GetType();
            _serializer.Serialize(bsw, data);
            Data = ms.ToArray();
        }

        public Type MessageType { get; set; }
        public byte[] Data { get; set; }

        public object Decode()
        {
            var ms = new MemoryStream(Data);
            var bsr = new BsonReader(ms);
            return _serializer.Deserialize(bsr, MessageType);
        }
    }
}