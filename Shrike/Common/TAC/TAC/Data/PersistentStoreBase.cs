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
using System.Linq;
using System.Threading;

namespace AppComponents.Data
{
    public abstract class PersistentStoreBase<TKey> : IPersistentStore<TKey>
    {
        private readonly ThreadLocal<IList<PersistedHashTableState<TKey>>> _currentStates =
            new ThreadLocal<IList<PersistedHashTableState<TKey>>>(() => null);

        private readonly ObjectPool<Stream> _pool;
        private IList<PersistedHashTableState<TKey>> _globalStates = new ConcurrentList<PersistedHashTableState<TKey>>();

        private bool _isDisposed;


        protected PersistentStoreBase()
        {
            _pool = new ObjectPool<Stream>(ReadOnlyClonedStream);
        }

        protected IList<PersistedHashTableState<TKey>> CurrentStates
        {
            get { return _currentStates.Value; }
            set { _currentStates.Value = value; }
        }

        protected abstract Stream Log { get; }

        #region IPersistentStore<TKey> Members

        public bool IsCreated { get; protected set; }


        public T Read<T>(Func<T> readAction)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PersistentStore");

            var old = CurrentStates;
            CurrentStates = old ?? _globalStates;

            try
            {
                Stream stream;
                using (_pool.Resolve(out stream))
                    return readAction();
            }
            finally
            {
                CurrentStates = old;
            }
        }

        public T Read<T>(Func<Stream, T> readAction)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PersistentStore");

            var old = CurrentStates;
            CurrentStates = old ?? _globalStates;

            try
            {
                Stream stream;
                using (_pool.Resolve(out stream))
                    return readAction(stream);
            }
            finally
            {
                CurrentStates = old;
            }
        }

        public void Write(Action<Stream> action)
        {
            lock (this)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException("PersistentStore");

                try
                {
                    CurrentStates = new ConcurrentList<PersistedHashTableState<TKey>>(_globalStates.Select(s =>
                                                                                                           new PersistedHashTableState
                                                                                                               <TKey>(
                                                                                                               s.
                                                                                                                   KeyComparer,
                                                                                                               s.
                                                                                                                   KeyCloner)
                                                                                                               {
                                                                                                                   KeyAddresses
                                                                                                                       =
                                                                                                                       s
                                                                                                                       .
                                                                                                                       KeyAddresses
                                                                                                               }));

                    action(Log);
                }
                finally
                {
                    _pool.Clear();
                    Interlocked.Exchange(ref _globalStates, CurrentStates);
                    CurrentStates = null;
                }
            }
        }


        public IList<PersistedHashTableState<TKey>> TableStates
        {
            get { return CurrentStates ?? _globalStates; }
        }


        public void ClearPool()
        {
            _pool.Clear();
        }


        public virtual void Dispose()
        {
            _pool.Clear();
            _currentStates.Dispose();
            _isDisposed = true;
        }

        public abstract void ReplaceAtomically(Stream newLog);
        public abstract Stream ProvideTempStream();
        public abstract void FlushLog();
        public abstract StoreState ProvideState();
        public abstract void SetCapacity(int size);

        #endregion

        protected abstract Stream ReadOnlyClonedStream();
    }
}