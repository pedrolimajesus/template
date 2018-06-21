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
using System.Linq;
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents
{
    public class ConcurrentList<T> : IList<T>, IList
    {
        private readonly object _syncRoot = new object();
        private readonly List<T> _underlyingList = new List<T>();
        private readonly ConcurrentQueue<T> _underlyingQueue = new ConcurrentQueue<T>();
        private volatile bool _isDirty;
        private volatile bool _requiresSync;

        public ConcurrentList()
        {
        }

        public ConcurrentList(IEnumerable<T> items)
        {
            AddRange(items);
        }

        public T this[int index]
        {
            get { return ((IList<T>) this)[index]; }
            set { ((IList<T>) this)[index] = value; }
        }

        #region IList Members

        public int Add(object value)
        {
            if (_requiresSync)
                lock (_syncRoot)
                    _underlyingQueue.Enqueue((T) value);
            else
                _underlyingQueue.Enqueue((T) value);
            _isDirty = true;
            lock (_syncRoot)
            {
                UpdateLists();
                return _underlyingList.IndexOf((T) value);
            }
        }

        public bool Contains(object value)
        {
            lock (_syncRoot)
            {
                UpdateLists();
                return _underlyingList.Contains((T) value);
            }
        }

        public int IndexOf(object value)
        {
            lock (_syncRoot)
            {
                UpdateLists();
                return _underlyingList.IndexOf((T) value);
            }
        }

        public void Insert(int index, object value)
        {
            lock (_syncRoot)
            {
                UpdateLists();
                _underlyingList.Insert(index, (T) value);
            }
        }

        public void Remove(object value)
        {
            lock (_syncRoot)
            {
                UpdateLists();
                _underlyingList.Remove((T) value);
            }
        }

        object IList.this[int index]
        {
            get { return ((IList<T>) this)[index]; }
            set { ((IList<T>) this)[index] = (T) value; }
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public void CopyTo(Array array, int index)
        {
            lock (_syncRoot)
            {
                UpdateLists();
                _underlyingList.CopyTo((T[]) array, index);
            }
        }

        public object SyncRoot
        {
            get { return _syncRoot; }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        #endregion

        #region IList<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            lock (_syncRoot)
            {
                UpdateLists();
                var l = _underlyingList.ToList();
                if (l.Count == 0)
                    return Enumerable.Empty<T>().GetEnumerator();
                return l.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (_requiresSync)
                lock (_syncRoot)
                    _underlyingQueue.Enqueue(item);
            else
                _underlyingQueue.Enqueue(item);
            _isDirty = true;
        }

        public void RemoveAt(int index)
        {
            lock (_syncRoot)
            {
                UpdateLists();
                _underlyingList.RemoveAt(index);
            }
        }

        T IList<T>.this[int index]
        {
            get
            {
                lock (_syncRoot)
                {
                    UpdateLists();
                    return _underlyingList[index];
                }
            }
            set
            {
                lock (_syncRoot)
                {
                    UpdateLists();
                    _underlyingList[index] = value;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                UpdateLists();
                _underlyingList.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (_syncRoot)
            {
                UpdateLists();
                return _underlyingList.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_syncRoot)
            {
                UpdateLists();
                _underlyingList.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            lock (_syncRoot)
            {
                UpdateLists();
                return _underlyingList.Remove(item);
            }
        }

        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    UpdateLists();
                    return _underlyingList.Count;
                }
            }
        }

        public int IndexOf(T item)
        {
            lock (_syncRoot)
            {
                UpdateLists();
                return _underlyingList.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (_syncRoot)
            {
                UpdateLists();
                _underlyingList.Insert(index, item);
            }
        }

        #endregion

        private void UpdateLists()
        {
            if (!_isDirty)
                return;
            lock (_syncRoot)
            {
                _requiresSync = true;
                T temp;
                while (_underlyingQueue.TryDequeue(out temp))
                    _underlyingList.Add(temp);
                _requiresSync = false;
                _isDirty = false;
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (_requiresSync)
                lock (_syncRoot)
                {
                    items.ForEach(item => _underlyingQueue.Enqueue(item));
                }
            else
            {
                items.ForEach(item => _underlyingQueue.Enqueue(item));
            }
            _isDirty = true;
        }
    }
}