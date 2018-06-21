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
using AppComponents.Data;

namespace AppComponents
{
    public class StoreAddress<TKey>
    {
        public long Position { get; set; }
        public int Size { get; set; }
        public TKey Key { get; set; }
    }

    public class ReadInfo<TKey>
    {
        public StoreAddress<TKey> Address { get; set; }
        public Func<byte[]> GetData { get; set; }
        public Guid ETag { get; set; }
    }

    public enum CommandCode : byte
    {
        Put = 1,
        Delete = 2,
        PutCurrent = 3,
        Skip = 9
    }

    public class SdsCommand<TKey>
    {
        public SdsCommand()
        {
            Position = 1;
            Size = 0;
        }

        public CommandCode Code { get; set; }
        public int Size { get; set; }
        public TKey Key { get; set; }
        public long Position { get; set; }
        public int TableId { get; set; }
        public byte[] Data { get; set; }
        public Guid ETag { get; set; }
    }

    public enum PersistedHashTableLocalConfig
    {
        Name,
        Comparer,
        Cloner,
        Store,
        Id,
        Database,
        Transaction
    }

    public interface IPersistedHashTable<TKey> : IEnumerable<ReadInfo<TKey>>, IDisposable
    {
        string Name { get; set; }
        int Id { get; set; }
        int Count { get; }

        IEnumerable<TKey> Keys { get; }
        int Garbage { get; }
        ReadInfo<TKey> Read(TKey key);
        Tuple<T, Guid> ReadObject<T>(TKey key);


        bool Remove(TKey key);
        bool Put(TKey key, byte[] data, Guid eTag);
        bool PutObject<T>(TKey key, T obj, Guid eTag);
        bool PutObject<T>(TKey key, T obj);


        bool Commit(Guid trx);
        void Rollback(Guid trx);


        bool UpdateKey(TKey key);
        void CleanState();
        bool Put(TKey key, byte[] data, Guid eTag, Guid trx);
        bool Remove(TKey key, Guid trx);
        bool UpdateKey(TKey key, long position, int size);
        void ApplyCommands(IEnumerable<SdsCommand<TKey>> commands);
        IEnumerable<SdsCommand<TKey>> CopyCommitted(Stream temp);
        IList<SdsCommand<TKey>> CommandsToCommit(Guid transaction);
    }


    [Serializable]
    public class StoreState
    {
        public string Path { get; set; }
        public string Prefix { get; set; }
        public byte[] Log { get; set; }
    }


    public class PersistedHashTableState<TKey>
    {
        public PersistedHashTableState(IComparer<TKey> keyComparer, Func<TKey, TKey> keyCloner)
        {
            KeyComparer = keyComparer;
            KeyCloner = keyCloner;
            KeyAddresses = new ImmutableTreeAcorn<TKey, StoreAddress<TKey>>(
                KeyComparer, keyCloner,
                f => new StoreAddress<TKey> {Key = keyCloner(f.Key), Position = f.Position, Size = f.Size}
                );
        }

        public IImmutableTree<TKey, StoreAddress<TKey>> KeyAddresses { get; set; }
        public IComparer<TKey> KeyComparer { get; set; }
        public Func<TKey, TKey> KeyCloner { get; set; }
    }

    public interface IPersistentStore<TKey> : IDisposable
    {
        bool IsCreated { get; }
        IList<PersistedHashTableState<TKey>> TableStates { get; }

        T Read<T>(Func<T> readAction);
        T Read<T>(Func<Stream, T> readAction);

        void Write(Action<Stream> action);

        void ReplaceAtomically(Stream newLog);
        Stream ProvideTempStream();

        void FlushLog();
        void ClearPool();
        void SetCapacity(int capacity);

        StoreState ProvideState();
    }


    public enum StructuredDataStorageLocalConfig
    {
        Store,
        Comparer,
        Cloner
    }

    public interface IStructuredDataStorage<TKey> : IDisposable
    {
        IEnumerable<IPersistedHashTable<TKey>> Tables { get; }
        IPersistedHashTable<TKey> this[string tableName] { get; }
        IPersistedHashTable<TKey> this[int tableId] { get; }
        Guid CurrentTransaction { get; }
        IList<PersistedHashTableState<TKey>> TableStates { get; }

        int AddTable(string name);
        void DeclareTables(Type enumType);

        IDisposable BeginTransaction();
        IDisposable SuppressTransaction();
        void Commit();
        void Rollback();

        void Compact();
    }

    public sealed class DataCorruptionException : ApplicationException
    {
        public DataCorruptionException()
        {
        }

        public DataCorruptionException(string msg) : base(msg)
        {
        }
    }
}