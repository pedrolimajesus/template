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
using System.IO;
using System.Linq;
using AppComponents.Extensions.EnumEx;
using AppComponents.Extensions.EnumerableEx;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace AppComponents.Data
{
    internal class StructuredDataDictionary<T> : IStructuredDataDictionary<T>
    {
        private readonly StructuredDataClient _parent;
        private readonly string _tableName;


        public StructuredDataDictionary(StructuredDataClient parent, Enum id)
        {
            _parent = parent;
            _tableName = id.EnumName();
        }

        #region IStructuredDataDictionary<T> Members

        public IEnumerable<ReadAtom<T>> Fetch(params string[] keys)
        {
            return _parent.Fetch<T>(_tableName, keys);
        }

        public void AddNew(string key, T data)
        {
            var bytes = SerializeObject(data);

            _parent.Commands.Add(new StructuredDataRequest
                                     {
                                         Table = _tableName,
                                         RequestCode = StructuredDataRequestCode.Create,
                                         Data = bytes,
                                         ETag = Guid.Empty,
                                         Key = key
                                     });
        }

        public void Update(string key, T data, Guid eTag)
        {
            var bytes = SerializeObject(data);

            _parent.Commands.Add(new StructuredDataRequest
                                     {
                                         Table = _tableName,
                                         RequestCode = StructuredDataRequestCode.Update,
                                         Data = bytes,
                                         ETag = eTag,
                                         Key = key
                                     });
        }

        public void Delete(string key)
        {
            _parent.Commands.Add(new StructuredDataRequest
                                     {
                                         Table = _tableName,
                                         RequestCode = StructuredDataRequestCode.Delete,
                                         Key = key
                                     });
        }

        public IEnumerable<string> Keys
        {
            get { return _parent.GetTableKeys(_tableName); }
        }

        #endregion

        private static byte[] SerializeObject(T data)
        {
            var ms = new MemoryStream();
            var serializer = new JsonSerializer();
            var writer = new BsonWriter(ms);
            serializer.Serialize(writer, data);
            ms.Seek(0, SeekOrigin.Begin);
            var bytes = ms.ToArray();
            return bytes;
        }
    }

    internal enum StructuredDataRequestCode
    {
        Read,
        Create,
        Update,
        Delete,
        ReadKeys
    }

    internal class StructuredDataRequest
    {
        public StructuredDataRequestCode RequestCode { get; set; }
        public string Table { get; set; }
        public string Key { get; set; }
        public Guid ETag { get; set; }
        public byte[] Data { get; set; }
    }

    internal enum PutResultCode
    {
        Ok,
        Concurrency,
        StorageCapacity,
        Unknown
    }

    internal class PutResult
    {
        public string Key { get; set; }
        public string Message { get; set; }
        public PutResultCode Code { get; set; }
    }

    internal class StructuredDataBatchRequest
    {
        public string ReturnBox { get; set; }
        public IEnumerable<StructuredDataRequest> Request { get; set; }
    }

    internal class ReadAtomLarva
    {
        public string Key { get; set; }
        public byte[] Data { get; set; }
        public Guid ETag { get; set; }
    }

    public class StructuredDataClient : IStructuredDataClient
    {
        private readonly List<StructuredDataRequest> _commands = new List<StructuredDataRequest>();
        private readonly IMessageInbox _inbox;
        private readonly IMessageOutbox _server;
        private bool _isDisposed;


        public StructuredDataClient()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _inbox = config.Get<IMessageInbox>(StructuredDataClientLocalConfig.Inbox);
            _server = config.Get<IMessageOutbox>(StructuredDataClientLocalConfig.Server);
        }

        internal IList<StructuredDataRequest> Commands
        {
            get { return _commands; }
        }

        #region IStructuredDataClient Members

        public IStructuredDataDictionary<T> OpenTable<T>(Enum tableId)
        {
            return new StructuredDataDictionary<T>(this, tableId);
        }

        public IDisposable BeginTransaction()
        {
            return Disposable.Create(Rollback);
        }

        public void Commit()
        {
            var batch = new StructuredDataBatchRequest
                            {
                                ReturnBox = _inbox.Name,
                                Request = Commands
                            };

            _server.Enqueue(batch);
            _server.Send();

            var reply = _inbox.WaitForMessages<PutResult>(TimeSpan.FromMinutes(2.0));
            if (!reply.Any())
            {
                throw new TimeoutException();
            }

            _commands.Clear();

            if (reply.First().Code == PutResultCode.Ok)
                return;

            var innerExceptions = new List<Exception>();
            foreach (var result in reply)
            {
                var info = string.Format("For {0}: {1}", result.Key, result.Message);
                switch (result.Code)
                {
                    case PutResultCode.Concurrency:
                        innerExceptions.Add(new DBConcurrencyException(info));
                        break;

                    case PutResultCode.StorageCapacity:
                        innerExceptions.Add(new EndOfStreamException(info));
                        break;

                    default:
                        innerExceptions.Add(new ApplicationException(info));
                        break;
                }
            }
            var badResult = new AggregateException(innerExceptions);
            throw badResult;
        }

        public void Rollback()
        {
            _commands.Clear();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _inbox.Dispose();
                _server.Dispose();
            }
        }

        #endregion

        internal IEnumerable<ReadAtom<T>> Fetch<T>(string table, IEnumerable<string> keys)
        {
            var batch = new StructuredDataBatchRequest
                            {
                                ReturnBox = _inbox.Name,
                                Request = keys.Select(k =>
                                                      new StructuredDataRequest
                                                          {
                                                              Table = table,
                                                              Key = k,
                                                              RequestCode = StructuredDataRequestCode.Read
                                                          })
                            };

            _server.Enqueue(batch);
            _server.Send();

            var reply = _inbox.WaitForMessages<ReadAtomLarva>(TimeSpan.FromMinutes(2.0));
            if (!reply.Any())
            {
                throw new TimeoutException();
            }

            var firstItem = reply.First();
            if (null == firstItem || string.IsNullOrEmpty(firstItem.Key) || null == firstItem.Data)
                return Enumerable.Empty<ReadAtom<T>>();

            return reply.Select(item =>
                                    {
                                        var ms = new MemoryStream(item.Data);
                                        var reader = new BsonReader(ms);
                                        var serializer = new JsonSerializer();

                                        var data = serializer.Deserialize<T>(reader);
                                        return new ReadAtom<T> {Data = data, ETag = item.ETag, Key = item.Key};
                                    });
        }

        internal IEnumerable<string> GetTableKeys(string tableName)
        {
            var batch = new StructuredDataBatchRequest
                            {
                                ReturnBox = _inbox.Name,
                                Request = EnumerableEx.OfOne(new StructuredDataRequest
                                                                 {
                                                                     Table = tableName,
                                                                     RequestCode = StructuredDataRequestCode.ReadKeys
                                                                 })
                            };

            _server.Enqueue(batch);
            _server.Send();

            var reply = _inbox.WaitForMessages<string>(TimeSpan.FromMinutes(2.0));

            return reply;
        }
    }
}