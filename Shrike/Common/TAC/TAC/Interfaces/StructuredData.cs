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
    public enum StructuredDataClientLocalConfig
    {
        Server,
        Inbox
    }


    public class ReadAtom<T>
    {
        public string Key { get; set; }
        public T Data { get; set; }
        public Guid ETag { get; set; }
    }

    public interface IStructuredDataDictionary<T>
    {
        IEnumerable<string> Keys { get; }
        IEnumerable<ReadAtom<T>> Fetch(params string[] keys);

        void AddNew(string key, T data);
        void Update(string key, T data, Guid eTag);
        void Delete(string key);
    }

    public interface IStructuredDataClient : IDisposable
    {
        IStructuredDataDictionary<T> OpenTable<T>(Enum tableId);
        IDisposable BeginTransaction();
        void Commit();
        void Rollback();
    }


    public enum StructuredDataServerLocalConfig
    {
        Inbox,
        Store,
        OutboxFactory
    }

    public interface IStructuredDataServer : IDisposable
    {
        void Run(Type tableDeclarationsEnum, CancellationToken ct);
    }
}