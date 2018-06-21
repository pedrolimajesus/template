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
using Newtonsoft.Json.Linq;

namespace AppComponents
{
    public interface IImmutableTree<TKey, TValue>
    {
        IImmutableTree<TKey, TValue> Left { get; }
        IImmutableTree<TKey, TValue> LeftMost { get; }
        IImmutableTree<TKey, TValue> Right { get; }
        IImmutableTree<TKey, TValue> RightMost { get; }

        TKey RootKey { get; }
        TValue RootValue { get; }

        IEnumerable<TKey> Keys { get; }
        IEnumerable<TKey> KeysDescending { get; }

        IEnumerable<KeyValuePair<TKey, TValue>> Items { get; }

        IEnumerable<TValue> Values { get; }
        IEnumerable<TValue> ValuesDescending { get; }
        int Count { get; }
        bool IsEmpty { get; }
        IImmutableTree<TKey, TValue> Add(TKey key, TValue value);
        IImmutableTree<TKey, TValue> AddOrUpdate(TKey key, TValue value, Func<TKey, TValue, TValue> updateValueFactory);
        Tuple<IImmutableTree<TKey, TValue>, bool, TValue> TryRemove(TKey key);
        IEnumerable<TValue> FindGreaterThan(TKey key);
        IEnumerable<TValue> FindGreaterOrEqualTo(TKey key);
        IEnumerable<TValue> FindLesserThan(TKey key);
        IEnumerable<TValue> FindLessorOrEqualTo(TKey key);

        IImmutableTree<TKey, TValue> Search(TKey key);
        IImmutableTree<TKey, TValue> Search(TKey key, Predicate<TValue> condition);
        bool Contains(TKey key);
        Tuple<bool, TValue> TryGetValue(TKey key);

        JObject ToJObject();
    }


    public interface IImmutableStack<T> : IEnumerable<T>
    {
        bool IsEmpty { get; }
        IImmutableStack<T> Push(T item);
        IImmutableStack<T> Pop();
        T Peek();
    }
}