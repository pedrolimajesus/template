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
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using AppComponents.Extensions.EnumerableEx;
using Newtonsoft.Json;
//using Newtonsoft.Json.Bson;
using Raven.Imports.Newtonsoft.Json.Bson;
//using Newtonsoft.Json.Linq;
using Raven.Imports.Newtonsoft.Json.Linq;

namespace AppComponents.Data
{
    public class StructuredDataStorage<TKey> : IStructuredDataStorage<TKey>
    {
        private const int _version = 10;
        private const int _small = 10000;
        private const int _medium = 100000;
        private readonly Func<TKey, TKey> _cloner;
        private readonly IComparer<TKey> _comparer;
        private readonly ThreadLocal<Guid> _currentTransaction = new ThreadLocal<Guid>(() => Guid.Empty);
        private readonly IPersistentStore<TKey> _store;

        private readonly ConcurrentList<IPersistedHashTable<TKey>> _tables =
            new ConcurrentList<IPersistedHashTable<TKey>>();


        public StructuredDataStorage()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _store = config.Get<IPersistentStore<TKey>>(StructuredDataStorageLocalConfig.Store);
            _comparer = config.Get<IComparer<TKey>>(StructuredDataStorageLocalConfig.Comparer);
            _cloner = config.Get<Func<TKey, TKey>>(StructuredDataStorageLocalConfig.Cloner);

            _store.Write(log =>
                             {
                                 if (0 == log.Length)
                                 {
                                     WriteHeader(log);
                                     log.Flush();
                                 }

                                 Validate(log);

                                 while (true)
                                 {
                                     var lastPos = log.Position;

                                     if (log.Position == log.Length)
                                         break;

                                     var commands = ReadCommands(log, lastPos);
                                     if (null == commands)
                                         break;

                                     if (commands.Length == 1 && commands[0].Code == CommandCode.Skip)
                                     {
                                         log.Position += commands[0].Size;
                                         continue;
                                     }

                                     var commandGroups = commands.GroupBy(c => c.TableId);

                                     foreach (var c in commandGroups)
                                     {
                                         _tables[c.Key].ApplyCommands(c);
                                     }
                                 }
                             });
        }

        #region IStructuredDataStorage<TKey> Members

        public Guid CurrentTransaction
        {
            get { return _currentTransaction.Value; }
        }


        public IEnumerable<IPersistedHashTable<TKey>> Tables
        {
            get { return _tables; }
        }

        public IPersistedHashTable<TKey> this[string tableName]
        {
            get { return _tables.First(t => t.Name == tableName); }
        }

        public IPersistedHashTable<TKey> this[int tableId]
        {
            get { return _tables[tableId]; }
        }


        public IList<PersistedHashTableState<TKey>> TableStates
        {
            get { return _store.TableStates; }
        }

        public IDisposable BeginTransaction()
        {
            if (_currentTransaction.Value != Guid.Empty)
                return Disposable.Create(() => { });

            _currentTransaction.Value = Guid.NewGuid();
            return Disposable.Create(() =>
                                         {
                                             if (_currentTransaction.Value != Guid.Empty)
                                                 Rollback();
                                         });
        }

        public IDisposable SuppressTransaction()
        {
            var hideTrx = _currentTransaction.Value;
            _currentTransaction.Value = Guid.Empty;

            return Disposable.Create(() => { _currentTransaction.Value = hideTrx; });
        }

        public void Commit()
        {
            if (_currentTransaction.Value == Guid.Empty)
                return;

            Commit(_currentTransaction.Value);

            _currentTransaction.Value = Guid.Empty;
        }

        public void Rollback()
        {
            Rollback(_currentTransaction.Value);
            _currentTransaction.Value = Guid.Empty;
        }

        public void Compact()
        {
            MaybeCompact();
        }

        public void Dispose()
        {
            foreach (var t in _tables) t.Dispose();
            _currentTransaction.Dispose();
        }

        public int AddTable(string name)
        {
            var existing = _tables.FirstIndexOf(t => t.Name == name);
            if (existing != -1)
                return existing;

            TableStates.Add(null);

            var reservation = new ReservePHT();
            _tables.Add(reservation);
            var id = _tables.IndexOf(reservation);


            var newTable = Catalog.Preconfigure()
                .Add(PersistedHashTableLocalConfig.Name, name)
                .Add(PersistedHashTableLocalConfig.Cloner, _cloner)
                .Add(PersistedHashTableLocalConfig.Comparer, _comparer)
                .Add(PersistedHashTableLocalConfig.Store, _store)
                .Add(PersistedHashTableLocalConfig.Database, this)
                .Add(PersistedHashTableLocalConfig.Id, id)
                .Add(PersistedHashTableLocalConfig.Transaction, _currentTransaction)
                .ConfiguredCreate(() => new PersistentHashTable<TKey>());


            _tables[id] = newTable;
            _store.Write(log =>
                             {
                                 WriteHeader(log);
                                 log.Flush();
                             });
            return id;
        }

        public void DeclareTables(Type enumType)
        {
            Debug.Assert(enumType.IsEnum);
            if (!enumType.IsEnum)
                throw new ArgumentException("enumType");

            foreach (var tn in Enum.GetNames(enumType))
            {
                AddTable(tn);
            }
        }

        #endregion

        private void WriteHeader(Stream log)
        {
            log.Seek(0, SeekOrigin.Begin);
            Debug.Assert(Tables != null, "Tables != null");
            var h = new JObject
                        {
                            {"Version", _version},
                            {"Tables", new JArray(Tables.Select(t => t.Name).ToArray())}
                        };
            h.WriteTo(new BsonWriter(log));
        }

        private void Validate(Stream log)
        {
            log.Seek(0, SeekOrigin.Begin);
            var br = new BsonReader(log);
            var dbInfo = (JObject) JToken.ReadFrom(br);
            if (dbInfo.Value<int>("Version") != _version)
                throw new VersionNotFoundException();

            var tableInfo = dbInfo.Value<JArray>("Tables");

            if (tableInfo.Count != _tables.Count)
                throw new DataCorruptionException();

            for (int each = 0; each != tableInfo.Count; each++)
                if (tableInfo[each].Value<string>() != _tables[each].Name)
                    throw new DataCorruptionException();
        }

        private static void WriteCommands(IEnumerable<SdsCommand<TKey>> commands, Stream log)
        {
            const int hashSize = 32;

            var dataSize =
                commands.Where(c => (c.Code == CommandCode.Put || c.Code == CommandCode.PutCurrent) && null != c.Data)
                    .Sum(c => c.Data.Length + hashSize);

            if (dataSize > 0)
                WriteTo(log, new JArray(new JObject {{"Code", (short) CommandCode.Skip}, {"Size", dataSize}}));

            var commArr = new JArray();


            Debug.Assert(commands != null, "commands != null");
            foreach (var c in commands)
            {
                var jKey = JsonConvert.SerializeObject(c.Key);
                var jsonComm = new JObject
                                   {
                                       {"Code", (short) c.Code},
                                       {"TableId", c.TableId},
                                       {"Key", jKey}
                                   };

                if (c.Code == CommandCode.Put || c.Code == CommandCode.PutCurrent)
                {
                    if (null != c.Data)
                    {
                        c.Position = log.Position;
                        c.Size = c.Data.Length;
                        c.ETag = Guid.NewGuid();

                        log.Write(c.Data, 0, c.Data.Length);

                        using (var hasher = SHA256.Create())
                        {
                            var hash = hasher.ComputeHash(c.Data);
                            log.Write(hash, 0, hash.Length);
                        }

                        // write etag
                        var etag = c.ETag.ToByteArray();
                        log.Write(etag, 0, etag.Length);
                    }

                    jsonComm.Add("Position", c.Position);
                    jsonComm.Add("Size", c.Size);
                }

                commArr.Add(jsonComm);
            }

            if (commArr.Count == 0 && dataSize == 0)
                return;

            WriteTo(log, commArr);
        }

        private static SdsCommand<TKey>[] ReadCommands(Stream log, long lastPosition)
        {
            try
            {
                var comms = ReadObject(log);
                return comms.Values<JToken>().Select(c => new
                                                              SdsCommand<TKey>
                                                              {
                                                                  Key =
                                                                      JsonConvert.DeserializeObject<TKey>(
                                                                          c.Value<string>("Key")),
                                                                  Position = c.Value<long>("Position"),
                                                                  Size = c.Value<int>("Size"),
                                                                  Code = (CommandCode) c.Value<byte>("Code"),
                                                                  TableId = c.Value<int>("TableId"),
                                                                  ETag = c.Value<Guid>("ETag")
                                                              }).ToArray();
            }
            catch (Exception)
            {
                log.SetLength(lastPosition);
                return null;
            }
        }

        private static void WriteTo(Stream log, JToken token)
        {
            var bw = new BsonWriter(log) {DateTimeKindHandling = DateTimeKind.Utc};
            token.WriteTo(bw);
        }

        private static JObject ReadObject(Stream log)
        {
            var br = new BsonReader(log) {DateTimeKindHandling = DateTimeKind.Utc};
            return JObject.Load(br);
        }


        private bool NeedsCompaction()
        {
            var keyCount = _tables.Sum(t => t.Count);
            var garbageCount = _tables.Sum(t => t.Garbage);

            if (keyCount < _small)
            {
                return garbageCount > keyCount;
            }

            if (keyCount < _medium)
            {
                return garbageCount > (keyCount/2);
            }

            return garbageCount > (keyCount/10);
        }

        private void Commit(Guid transaction)
        {
            var commands = new List<SdsCommand<TKey>>();
            foreach (var t in _tables)
            {
                var toCommit = t.CommandsToCommit(transaction);
                if (null == toCommit)
                    continue;

                commands.AddRange(toCommit);
            }


            if (commands.Count == 0)
                return;

            _store.Write(log =>
                             {
                                 log.Position = log.Length;

                                 for (int each = 0; each != commands.Count; each++)
                                 {
                                     WriteCommands(commands.Skip(each).Take(1024), log);
                                 }

                                 _store.FlushLog();

                                 foreach (var t in _tables)
                                 {
                                     t.Commit(transaction);
                                 }
                             });
        }

        private void Rollback(Guid transaction)
        {
            foreach (var t in _tables)
                t.Rollback(transaction);
        }


        private void MaybeCompact()
        {
            if (NeedsCompaction())
            {
                _store.Write(log =>
                                 {
                                     var temp = _store.ProvideTempStream();
                                     WriteHeader(temp);

                                     var commands = new ConcurrentList<SdsCommand<TKey>>();
                                     foreach (var t in _tables)
                                     {
                                         commands.AddRange(t.CopyCommitted(temp));
                                         t.CleanState();
                                     }

                                     WriteCommands(commands, temp);
                                     _store.ClearPool();
                                     _store.ReplaceAtomically(temp);

                                     using (SuppressTransaction())
                                     {
                                         _currentTransaction.Value = Guid.NewGuid();
                                         foreach (var command in commands)
                                         {
                                             _tables[command.TableId].UpdateKey(command.Key, command.Position,
                                                                                command.Size);
                                             _tables[command.TableId].Commit(_currentTransaction.Value);
                                         }
                                     }
                                 });
            }
        }

        #region Nested type: ReservePHT

        private class ReservePHT : IPersistedHashTable<TKey>
        {
            #region IPersistedHashTable<TKey> Members

            public string Name
            {
                get { return default(string); }
                set { }
            }

            public int Id
            {
                get { return 0; }
                set { }
            }

            public int Count
            {
                get { return 0; }
            }

            public IEnumerable<TKey> Keys
            {
                get { return Enumerable.Empty<TKey>(); }
            }

            public ReadInfo<TKey> Read(TKey key)
            {
                return null;
            }

            public Tuple<T, Guid> ReadObject<T>(TKey key)
            {
                return null;
            }

            public bool Remove(TKey key)
            {
                return false;
            }

            public bool Put(TKey key, byte[] data, Guid eTag)
            {
                return false;
            }

            public bool PutObject<T>(TKey key, T obj, Guid eTag)
            {
                return false;
            }

            public bool PutObject<T>(TKey key, T obj)
            {
                return false;
            }

            public bool Commit(Guid trx)
            {
                return true;
            }

            public void Rollback(Guid trx)
            {
            }

            public bool UpdateKey(TKey key)
            {
                return true;
            }

            public int Garbage
            {
                get { return 0; }
            }

            public void CleanState()
            {
            }

            public bool Put(TKey key, byte[] data, Guid eTag, Guid trx)
            {
                return false;
            }

            public bool Remove(TKey key, Guid trx)
            {
                return false;
            }

            public bool UpdateKey(TKey key, long position, int size)
            {
                return true;
            }

            public void ApplyCommands(IEnumerable<SdsCommand<TKey>> commands)
            {
            }

            public IEnumerable<SdsCommand<TKey>> CopyCommitted(Stream temp)
            {
                return Enumerable.Empty<SdsCommand<TKey>>();
            }

            public IList<SdsCommand<TKey>> CommandsToCommit(Guid transaction)
            {
                return new List<SdsCommand<TKey>>();
            }

            public IEnumerator<ReadInfo<TKey>> GetEnumerator()
            {
                return (IEnumerator<ReadInfo<TKey>>) Enumerable.Empty<ReadInfo<TKey>>();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return (IEnumerator<ReadInfo<TKey>>) Enumerable.Empty<ReadInfo<TKey>>();
            }

            public void Dispose()
            {
            }

            #endregion
        }

        #endregion
    }
}