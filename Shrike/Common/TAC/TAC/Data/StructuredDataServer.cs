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
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents.Data
{
    public class StructuredDataServer : IStructuredDataServer
    {
        private readonly IMessageInbox _commandsInbox;
        private readonly IStructuredDataStorage<string> _dataStore;
        private readonly Func<string, IMessageOutbox> _outboxFactory;
        private bool _isDisposed;
        private DateTime _nextCompaction = DateTime.UtcNow + TimeSpan.FromMinutes(20.0);

        public StructuredDataServer()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _commandsInbox = config.Get<IMessageInbox>(StructuredDataServerLocalConfig.Inbox);
            _outboxFactory = config.Get<Func<string, IMessageOutbox>>(StructuredDataServerLocalConfig.OutboxFactory);
            _dataStore = config.Get<IStructuredDataStorage<string>>(StructuredDataServerLocalConfig.Store);
        }

        #region IStructuredDataServer Members

        public void Run(Type tableDeclarations, CancellationToken ct)
        {
            _dataStore.DeclareTables(tableDeclarations);

            while (!ct.IsCancellationRequested)
            {
                var commands =
                    _commandsInbox.WaitForMessages<StructuredDataBatchRequest>(TimeSpan.FromSeconds(1.0));


                if (!ct.IsCancellationRequested && commands.Any())
                {
                    foreach (var r in commands)
                    {
                        ThreadPool.QueueUserWorkItem(ExecuteBatch,
                                                     new BatchExecution {Server = this, Request = r});
                    }
                }

                if (!ct.IsCancellationRequested && DateTime.UtcNow > _nextCompaction)
                {
                    _nextCompaction = DateTime.UtcNow + TimeSpan.FromMinutes(20.0);
                    _dataStore.Compact();
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _commandsInbox.Dispose();
                _dataStore.Dispose();
            }
        }

        #endregion

        private static void ExecuteBatch(object context)
        {
            var be = (BatchExecution) context;

            using (var replyBack = be.Server._outboxFactory(be.Request.ReturnBox))
            {
                try
                {
                    using (be.Server._dataStore.BeginTransaction())
                    {
                        var grouped = be.Request.Request.GroupBy(r => r.Table);
                        var putOk = false;

                        foreach (var t in grouped)
                        {
                            var table = be.Server._dataStore[t.Key];

                            foreach (var req in t)
                            {
                                switch (req.RequestCode)
                                {
                                    case StructuredDataRequestCode.Read:
                                        var data = table.Read(req.Key);
                                        replyBack.Enqueue(new ReadAtomLarva
                                                              {
                                                                  Key = req.Key,
                                                                  Data = data.GetData(),
                                                                  ETag = data.ETag
                                                              });
                                        break;

                                    case StructuredDataRequestCode.Create:
                                        table.Put(req.Key, req.Data, Guid.Empty);
                                        putOk = true;
                                        break;

                                    case StructuredDataRequestCode.Update:
                                        table.Put(req.Key, req.Data, req.ETag);
                                        putOk = true;
                                        break;

                                    case StructuredDataRequestCode.Delete:
                                        table.Remove(req.Key);
                                        putOk = true;
                                        break;

                                    case StructuredDataRequestCode.ReadKeys:
                                        table.Keys.ForEach(replyBack.Enqueue);
                                        break;
                                }
                            }
                        }

                        be.Server._dataStore.Commit();

                        if (putOk)
                            replyBack.Enqueue(new PutResult {Code = PutResultCode.Ok});
                    }
                }
                catch (EndOfStreamException eosEx)
                {
                    replyBack.Enqueue(new PutResult {Code = PutResultCode.StorageCapacity, Message = eosEx.Message});
                }
                catch (DBConcurrencyException dbcEx)
                {
                    replyBack.Enqueue(new PutResult {Code = PutResultCode.Concurrency, Key = dbcEx.Message});
                }
                catch (Exception allEx)
                {
                    replyBack.Enqueue(new PutResult
                                          {Code = PutResultCode.Unknown, Message = allEx.Message + allEx.StackTrace});
                }

                replyBack.Send();
            }
        }

        #region Nested type: BatchExecution

        internal class BatchExecution
        {
            public StructuredDataServer Server { get; set; }
            public StructuredDataBatchRequest Request { get; set; }
        }

        #endregion
    }
}