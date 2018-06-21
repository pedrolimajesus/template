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
using System.Linq;

namespace AppComponents
{

    #region Classes

    internal class FixedArray<T> : ICollection<T>
    {
        private int _capacity;
        private T[] _list;
        private int _tailIndex;

        public FixedArray(T[] list, int lastItemIndex)
        {
            _list = list;
            _capacity = _list.Length;
            _tailIndex = lastItemIndex;
        }

        public FixedArray(int length)
        {
            _list = new T[length];
            _capacity = length;
        }

        #region ICollection<T> Members

        public int Count
        {
            get { return _capacity; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }


        public void Add(T item)
        {
            _list[_tailIndex] = item;
            _tailIndex++;
        }

        public void Clear()
        {
            for (int i = 0; i != _list.Length; i++) _list[i] = default(T);
            _tailIndex = 0;
            _capacity = _list.Length;
        }

        public bool Contains(T item)
        {
            return _list.Any(each => each.Equals(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_list, arrayIndex, array, 0, _capacity);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new NestedSimpleEnumerator(_list, _tailIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Remove(T item)
        {
            return false;
        }

        #endregion

        public bool Equals(FixedArray<T> value)
        {
            if (ReferenceEquals(null, value))
            {
                return false;
            }
            if (ReferenceEquals(this, value))
            {
                return true;
            }
            return _tailIndex == value._tailIndex &&
                   _capacity == value._capacity &&
                   Equals(_list, value._list);
        }

        public override bool Equals(object obj)
        {
            FixedArray<T> temp = obj as FixedArray<T>;
            if (temp == null)
                return false;
            return Equals(temp);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = 17;
                result = result*23 + _tailIndex.GetHashCode();
                result = result*23 + _capacity.GetHashCode();
                result = result*23 + ((_list != null) ? _list.GetHashCode() : 0);
                return result;
            }
        }

        public override string ToString()
        {
            return string.Format("_tailIndex: {0}, _capacity: {1}, _list: {2}, Count: {3}, IsReadOnly: {4}", _tailIndex,
                                 _capacity, _list, Count, IsReadOnly);
        }

        #region Nested type: NestedSimpleEnumerator

        internal class NestedSimpleEnumerator : IEnumerator<T>
        {
            private int _currentIdx = -1;
            private int _len;
            private T[] _list;

            public NestedSimpleEnumerator(T[] list, int length)
            {
                _list = list;
                _len = length;
            }

            #region IEnumerator<T> Members

            public T Current
            {
                get { return _list[_currentIdx]; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }


            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _currentIdx++;
                return _currentIdx < _len;
            }

            public void Reset()
            {
                _currentIdx = 0;
            }

            #endregion
        }

        #endregion
    }

    #endregion Classes
}