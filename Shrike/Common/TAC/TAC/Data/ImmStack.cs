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

namespace AppComponents.Data
{
    internal class ImmutableStackEmpty<T> : IImmutableStack<T>
    {
        #region IImmutableStack<T> Members

        public IImmutableStack<T> Push(T item)
        {
            return new ImmutableStack<T>(item, this);
        }

        public IImmutableStack<T> Pop()
        {
            throw new ArgumentOutOfRangeException();
        }

        public T Peek()
        {
            throw new ArgumentOutOfRangeException();
        }

        public bool IsEmpty
        {
            get { return true; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public class ImmutableStack<T> : IImmutableStack<T>
    {
        private static readonly ImmutableStackEmpty<T> _empty = new ImmutableStackEmpty<T>();

        private readonly T _head;
        private readonly IImmutableStack<T> _tail;

        internal ImmutableStack(T head, IImmutableStack<T> tail)
        {
            _head = head;
            _tail = tail;
        }

        public static IImmutableStack<T> Start
        {
            get { return _empty; }
        }

        #region IImmutableStack<T> Members

        public IImmutableStack<T> Push(T item)
        {
            return new ImmutableStack<T>(item, this);
        }

        public IImmutableStack<T> Pop()
        {
            return _tail;
        }

        public T Peek()
        {
            return _head;
        }

        public bool IsEmpty
        {
            get { return false; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (IImmutableStack<T> st = this; !st.IsEmpty; st = st.Pop())
                yield return st.Peek();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}