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
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace AppComponents.Messaging
{
    public static class MemoryMappedTransferQueueConstants
    {
        public const int DefaultCapacity = 1024*1024*4;
        public const double DefaultAutoSendSeconds = 5.0;
    }


    public class MemoryMappedTransferOutbox : IMessageOutbox
    {
        private readonly long _capacity;
        private readonly ConcurrentQueue<object> _pending = new ConcurrentQueue<object>();
        private readonly MemoryMappedTransferPipe _pipe;
        private readonly object _sendLock = new object();
        private readonly JsonSerializer _serializer = new JsonSerializer();
        private bool _isDisposed;
        private Timer _sendTimer;


        public MemoryMappedTransferOutbox(string name, int capacity = MemoryMappedTransferQueueConstants.DefaultCapacity)
        {
            _pipe = new MemoryMappedTransferPipe(name, capacity);
            _capacity = capacity;
            Name = name;
        }

        public MemoryMappedTransferOutbox()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var name = config[MessageBoxLocalConfig.Name];
            var capacity = config.Get(MessageBoxLocalConfig.OptionalCapacity,
                                      MemoryMappedTransferQueueConstants.DefaultCapacity);
            _pipe = new MemoryMappedTransferPipe(name, capacity);
            _capacity = capacity;
            Name = name;
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

            _pipe.Dispose();
        }

        public string Name { get; set; }

        public long Capacity
        {
            get { return _capacity; }
        }

        public bool Pending
        {
            get { return _pending.Any(); }
        }

        public void Enqueue<T>(T item)
        {
            _pending.Enqueue(item);
        }


        public void Send()
        {
            lock (_sendLock)
            {
                if (_pending.Any())
                {
                    _pipe.AppendPipe(str =>
                                         {
                                             str.Seek(str.Length, SeekOrigin.Begin);

                                             object item;
                                             while (_pending.TryDequeue(out item))
                                             {
                                                 try
                                                 {
                                                     var writer = new BsonWriter(str);
                                                     _serializer.Serialize(writer, item);
                                                     writer.Flush();
                                                 }
                                                 catch (EndOfStreamException)
                                                 {
                                                     _pending.Enqueue(item);
                                                     break;
                                                 }
                                             }


                                             str.Flush();
                                         });
                }
            }
        }


        public void AutomaticSend()
        {
            AutomaticSend(TimeSpan.FromSeconds(MemoryMappedTransferQueueConstants.DefaultAutoSendSeconds));
        }

        public void AutomaticSend(TimeSpan sendDuration)
        {
            if (null != _sendTimer)
                return;

            _sendTimer = new Timer(CheckSend, this, 0L, (long) sendDuration.TotalMilliseconds);
        }

        #endregion

        private static void CheckSend(object obj)
        {
            var that = (MemoryMappedTransferOutbox) obj;
            that.Send();
        }
    }


    public class MemoryMappedTransferInbox : IMessageInbox
    {
        private readonly MemoryMappedTransferPipe _pipe;
        private readonly JsonSerializer _serializer = new JsonSerializer();


        public MemoryMappedTransferInbox(string name, int capacity = MemoryMappedTransferQueueConstants.DefaultCapacity)
        {
            _pipe = new MemoryMappedTransferPipe(name, capacity);
            Name = name;
        }

        public MemoryMappedTransferInbox()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var name = config[MessageBoxLocalConfig.Name];
            var capacity = config.Get(MessageBoxLocalConfig.OptionalCapacity,
                                      MemoryMappedTransferQueueConstants.DefaultCapacity);
            _pipe = new MemoryMappedTransferPipe(name, capacity);
            Name = name;
        }

        #region IMessageInbox Members

        public void Dispose()
        {
            _pipe.Dispose();
        }

        public string Name { get; set; }

        public IEnumerable<object> WaitForMessages(TimeSpan duration)
        {
            var retval = new List<object>();

            var res = _pipe.PipeMessage.WaitOne(duration);
            if (res)
            {
                _pipe.ReadPipe(str =>
                                   {
                                       while (str.Position != str.Length)
                                       {
                                           try
                                           {
                                               var br = new BsonReader(str);
                                               var obj = _serializer.Deserialize(br);
                                               retval.Add(obj);
                                           }
                                           catch (EndOfStreamException)
                                           {
                                               break;
                                           }
                                       }
                                   });
            }

            return retval;
        }

        public IEnumerable<T> WaitForMessages<T>(TimeSpan duration)
        {
            var retval = new List<T>();

            var res = _pipe.PipeMessage.WaitOne(duration);
            if (res)
            {
                _pipe.ReadPipe(str =>
                                   {
                                       while (str.Position != str.Length)
                                       {
                                           try
                                           {
                                               var br = new BsonReader(str);
                                               var obj = (T) _serializer.Deserialize(br, typeof (T));
                                               retval.Add(obj);
                                           }
                                           catch (EndOfStreamException)
                                           {
                                               break;
                                           }
                                       }
                                   });
            }

            return retval;
        }

        #endregion
    }
}