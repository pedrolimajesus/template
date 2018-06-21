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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace AppComponents.Data
{
    public class PersistentHashTable<TKey> : IPersistedHashTable<TKey>
    {
        private readonly Func<TKey, TKey> _cloner;
        private readonly IComparer<TKey> _comparer;
        private readonly ThreadLocal<Guid> _trx = new ThreadLocal<Guid>();
        private readonly ConcurrentDictionary<TKey, Guid> _trxModifiedKeys = new ConcurrentDictionary<TKey, Guid>();

        private readonly ConcurrentDictionary<Guid, ConcurrentList<SdsCommand<TKey>>> _trxOperations =
            new ConcurrentDictionary<Guid, ConcurrentList<SdsCommand<TKey>>>();

        private IStructuredDataStorage<TKey> _parent;
        private IPersistentStore<TKey> _store;

        public PersistentHashTable()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);


            _comparer = config.Get<IComparer<TKey>>(PersistedHashTableLocalConfig.Comparer);
            _cloner = config.Get<Func<TKey, TKey>>(PersistedHashTableLocalConfig.Cloner);
            Name = config[PersistedHashTableLocalConfig.Name];
            _store = config.Get<IPersistentStore<TKey>>(PersistedHashTableLocalConfig.Store);
            _parent = config.Get<IStructuredDataStorage<TKey>>(PersistedHashTableLocalConfig.Database);
            _trx = config.Get<ThreadLocal<Guid>>(PersistedHashTableLocalConfig.Transaction);

            Id = config.Get<int>(PersistedHashTableLocalConfig.Id);

            _parent.TableStates[Id] = new PersistedHashTableState<TKey>(_comparer, _cloner);
        }

        private IImmutableTree<TKey, StoreAddress<TKey>> MapKeyToStoreAddress
        {
            get { return _parent.TableStates[Id].KeyAddresses; }

            set { _parent.TableStates[Id].KeyAddresses = value; }
        }

        private IEqualityComparer<TKey> CompareEq
        {
            get { return (IEqualityComparer<TKey>) _comparer; }
        }

        #region IPersistedHashTable<TKey> Members

        public string Name { get; set; }
        public int Id { get; set; }

        public int Count
        {
            get { return MapKeyToStoreAddress.Count; }
        }

        public IEnumerable<TKey> Keys
        {
            get { return _store.Read(() => MapKeyToStoreAddress.Keys); }
        }

        public ReadInfo<TKey> Read(TKey key)
        {
            return Read(key, _trx.Value);
        }

        public bool UpdateKey(TKey key)
        {
            return UpdateKey(key, _trx.Value);
        }

        public bool UpdateKey(TKey key, long position, int size)
        {
            Guid existing;
            if (_trxModifiedKeys.TryGetValue(key, out existing) && existing != _trx.Value)
                return false;

            _trxOperations.GetOrAdd(_trx.Value, new ConcurrentList<SdsCommand<TKey>>())
                .Add(new SdsCommand<TKey>
                         {
                             Key = key,
                             Position = position,
                             Size = size,
                             TableId = Id,
                             Code = CommandCode.Put
                         });

            if (existing != _trx.Value)
                _trxModifiedKeys.TryAdd(key, _trx.Value);

            return true;
        }

        public bool Remove(TKey key)
        {
            return Remove(key, _trx.Value);
        }

        public bool Put(TKey key, byte[] data, Guid eTag)
        {
            return Put(key, data, eTag, _trx.Value);
        }

        public bool Put(TKey key, byte[] data, Guid eTag, Guid trx)
        {
            Guid existing;
            if (_trxModifiedKeys.TryGetValue(key, out existing) && existing != trx)
                return false;

            var cc = eTag == Guid.Empty ? CommandCode.Put : CommandCode.PutCurrent;


            _trxOperations.GetOrAdd(trx, new ConcurrentList<SdsCommand<TKey>>()).Add(new SdsCommand<TKey>
                                                                                         {
                                                                                             Key = key,
                                                                                             Data = data,
                                                                                             ETag = eTag,
                                                                                             TableId = Id,
                                                                                             Code = cc
                                                                                         });

            if (existing != trx)
                _trxModifiedKeys.TryAdd(key, trx);

            return true;
        }

        public bool Remove(TKey key, Guid trx)
        {
            Guid existing;
            if (_trxModifiedKeys.TryGetValue(key, out existing) && existing != trx)
                return false;

            _trxOperations.GetOrAdd(trx, new ConcurrentList<SdsCommand<TKey>>())
                .Add(new SdsCommand<TKey>
                         {
                             Key = key,
                             TableId = Id,
                             Code = CommandCode.Delete
                         });

            if (existing != trx)
                _trxModifiedKeys.TryAdd(key, trx);

            return true;
        }

        public bool Commit(Guid trx)
        {
            ConcurrentList<SdsCommand<TKey>> commands;
            if (_trxOperations.TryRemove(trx, out commands) == false || commands.Count == 0)
                return false;


            ApplyCommands(commands);
            return true;
        }

        public void Rollback(Guid trx)
        {
            ConcurrentList<SdsCommand<TKey>> commands;
            if (_trxOperations.TryRemove(trx, out commands) == false || commands.Count == 0)
                return;

            foreach (var cmd in commands)
            {
                Guid _;
                _trxModifiedKeys.TryRemove(cmd.Key, out _);
            }
        }

        public int Garbage { get; private set; }

        public void CleanState()
        {
            Garbage = 0;
        }

        public IEnumerator<ReadInfo<TKey>> GetEnumerator()
        {
            foreach (var sa in MapKeyToStoreAddress.Values)
            {
                byte[] data = null;
                var address = sa;
                yield return new ReadInfo<TKey>
                                 {
                                     Address =
                                         new StoreAddress<TKey> {Key = sa.Key, Position = sa.Position, Size = sa.Size},
                                     GetData =
                                         () =>
                                             {
                                                 Guid etag;
                                                 return data ??
                                                        (data =
                                                         ReadData(address.Position, address.Size, address.Key, out etag));
                                             }
                                 };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
        }

        public void ApplyCommands(IEnumerable<SdsCommand<TKey>> commands)
        {
            foreach (var command in commands)
            {
                if (command.Code == CommandCode.Put || command.Code == CommandCode.PutCurrent)
                {
                    InternalAdd(command.Key,
                                new StoreAddress<TKey>
                                    {Key = command.Key, Position = command.Position, Size = command.Size});
                }
                else if (command.Code == CommandCode.Delete)
                {
                    InternalRemove(command.Key);
                }

                Guid _;
                _trxModifiedKeys.TryRemove(command.Key, out _);
            }
        }

        public IEnumerable<SdsCommand<TKey>> CopyCommitted(Stream temp)
        {
            foreach (var kvp in MapKeyToStoreAddress.Items)
            {
                var etag = Guid.Empty;
                var data = ReadData(kvp.Value.Position, kvp.Value.Size, kvp.Key, out etag);

                yield return new SdsCommand<TKey>
                                 {
                                     Key = kvp.Key,
                                     Data = data,
                                     ETag = etag,
                                     TableId = Id,
                                     Size = kvp.Value.Size,
                                     Code = etag == Guid.Empty ? CommandCode.Put : CommandCode.PutCurrent
                                 };
            }
        }

        public IList<SdsCommand<TKey>> CommandsToCommit(Guid transaction)
        {
            ConcurrentList<SdsCommand<TKey>> commands;
            if (_trxOperations.TryGetValue(transaction, out commands) == false)
                return null;

            // test put currents right now.
            var concurrentPuts = from c in commands where c.Code == CommandCode.PutCurrent select c;
            TestEtags(concurrentPuts);

            return commands;
        }

        public Tuple<T, Guid> ReadObject<T>(TKey key)
        {
            var data = Read(key);
            if (null != data)
            {
                var ms = new MemoryStream(data.GetData());
                var reader = new BsonReader(ms);
                var serializer = new JsonSerializer();

                var obj = serializer.Deserialize<T>(reader);
                return Tuple.Create(obj, data.ETag);
            }

            return Tuple.Create(default(T), Guid.NewGuid());
        }

        public bool PutObject<T>(TKey key, T obj, Guid eTag)
        {
            var ms = new MemoryStream();
            var serializer = new JsonSerializer();
            var writer = new BsonWriter(ms);
            serializer.Serialize(writer, obj);
            return Put(key, ms.ToArray(), eTag);
        }

        public bool PutObject<T>(TKey key, T obj)
        {
            return PutObject(key, obj, Guid.Empty);
        }

        #endregion

        private void TestEtags(IEnumerable<SdsCommand<TKey>> concurrentPuts)
        {
            foreach (
                var cp in
                    from cp in concurrentPuts
                    let atom = StoreRead(cp.Key)
                    where null != atom && atom.ETag != cp.ETag
                    select cp)
            {
                throw new DBConcurrencyException(cp.Key.ToString());
            }
        }

        internal bool UpdateKey(TKey key, Guid transaction)
        {
            Guid existing;
            if (_trxModifiedKeys.TryGetValue(key, out existing) && existing != transaction)
                return false;
            var rr = Read(key, transaction);

            if (null != rr && CompareEq.Equals(key, rr.Address.Key))
                return true;

            _trxOperations.GetOrAdd(transaction, new ConcurrentList<SdsCommand<TKey>>()).Add(new SdsCommand<TKey>
                                                                                                 {
                                                                                                     Key = key,
                                                                                                     Position =
                                                                                                         null == rr
                                                                                                             ? -1
                                                                                                             : rr.
                                                                                                                   Address
                                                                                                                   .
                                                                                                                   Position,
                                                                                                     Size =
                                                                                                         null == rr
                                                                                                             ? 0
                                                                                                             : rr.
                                                                                                                   Address
                                                                                                                   .Size,
                                                                                                     TableId = Id,
                                                                                                     Code =
                                                                                                         CommandCode.Put
                                                                                                 });

            if (existing != transaction)
                _trxModifiedKeys.TryAdd(key, transaction);

            return true;
        }


        internal ReadInfo<TKey> Read(TKey key, Guid transaction)
        {
            byte[] data;

            Guid modified;
            if (_trxModifiedKeys.TryGetValue(key, out modified) && modified == transaction)
            {
                var comm = _trxOperations.GetOrAdd(transaction, new ConcurrentList<SdsCommand<TKey>>())
                    .LastOrDefault(x => CompareEq.Equals(x.Key, key));

                if (null != comm)
                {
                    if (comm.Code == CommandCode.Put)
                    {
                        var eTag = Guid.Empty;
                        data = ReadData(comm, eTag);
                        return new ReadInfo<TKey>
                                   {
                                       Address =
                                           new StoreAddress<TKey>
                                               {Key = comm.Key, Position = comm.Position, Size = comm.Size},
                                       GetData = delegate
                                                     {
                                                         var etag = Guid.Empty;
                                                         var retval = data ?? (data = ReadData(comm, etag));
                                                         return retval;
                                                     },
                                       ETag = eTag
                                   };
                    }
                    return null;
                }
            }

            return StoreRead(key);
        }

        private ReadInfo<TKey> StoreRead(TKey key)
        {
            byte[] data;

            return _store.Read(log =>
                                   {
                                       var mk = MapKeyToStoreAddress.TryGetValue(key);
                                       if (!mk.Item1)
                                           return null;

                                       var address = mk.Item2;
                                       Guid eTag;
                                       data = ReadData(address.Position, address.Size, address.Key, out eTag);
                                       return new ReadInfo<TKey>
                                                  {
                                                      Address =
                                                          new StoreAddress<TKey>
                                                              {
                                                                  Key = address.Key,
                                                                  Size = address.Size,
                                                                  Position = address.Position
                                                              },
                                                      GetData =
                                                          delegate
                                                              {
                                                                  Guid etag;
                                                                  var retval = data ??
                                                                               (data =
                                                                                ReadData(address.Position, address.Size,
                                                                                         address.Key, out etag));
                                                                  return retval;
                                                              },
                                                      ETag = eTag
                                                  };
                                   });
        }

        private void InternalRemove(TKey key)
        {
            var res = MapKeyToStoreAddress.TryRemove(key);

            if (res.Item2 == false)
                return;

            MapKeyToStoreAddress = res.Item1;

            Garbage += 1;
        }

        private void InternalAdd(TKey key, StoreAddress<TKey> storeAddress)
        {
            MapKeyToStoreAddress = MapKeyToStoreAddress.AddOrUpdate(key, storeAddress, (token, old) =>
                                                                                           {
                                                                                               Garbage += 1;
                                                                                               return storeAddress;
                                                                                           });
        }

        private byte[] ReadData(SdsCommand<TKey> command, Guid eTag)
        {
            if (null != command.Data)
            {
                eTag = command.ETag;
                return command.Data;
            }

            return ReadData(command.Position, command.Size, command.Key, out eTag);
        }

        private byte[] ReadData(long position, int size, TKey key, out Guid eTag)
        {
            if (-1 == position)
            {
                eTag = Guid.Empty;
                return null;
            }

            var etag = Guid.Empty;
            var retval = _store.Read(log => ReadDataNested(log, position, size, key, out etag));
            eTag = etag;
            return retval;
        }

        private byte[] ReadDataNested(Stream log, long position, int size, TKey key, out Guid eTag)
        {
            log.Position = position;
            var binaryReader = new BinaryReader(log);
            var data = binaryReader.ReadBytes(size);

            if (size != data.Length)
                throw new DataCorruptionException(key.ToString());

            using (var hasher = SHA256.Create())
            {
                var hash = hasher.ComputeHash(data);
                var fileHash = binaryReader.ReadBytes(hash.Length);


                if (fileHash.Where((t, i) => hash[i] != t).Any())
                    throw new DataCorruptionException(key.ToString());
            }

            var etagArr = binaryReader.ReadBytes(Guid.Empty.ToByteArray().Length);
            var etag = new Guid(etagArr);
            eTag = etag;

            return data;
        }
    }
}