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
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AppComponents.Data
{
    public sealed class ImmutableTreeAcorn<TKey, TValue> : IImmutableTree<TKey, TValue>
    {
        private readonly IComparer<TKey> _comparer;
        private readonly Func<TKey, TKey> _keyCloner;
        private readonly Func<TValue, TValue> _valueCloner;

        public ImmutableTreeAcorn(IComparer<TKey> keyComparer, Func<TKey, TKey> keyCloner, Func<TValue, TValue> valueCloner)
        {
            _comparer = keyComparer;
            _keyCloner = keyCloner;
            _valueCloner = valueCloner;
        }

        #region IImmutableTree<TKey,TValue> Members

        public IImmutableTree<TKey, TValue> Add(TKey key, TValue value)
        {
            return new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, key, value, this, this);
        }

        public IImmutableTree<TKey, TValue> AddOrUpdate(TKey key, TValue value, Func<TKey, TValue, TValue> updateValueFactory)
        {
            return new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, key, value, this, this);
        }

        public Tuple<IImmutableTree<TKey, TValue>, bool, TValue> TryRemove(TKey key)
        {
            return Tuple.Create((IImmutableTree<TKey, TValue>) this, false, default(TValue));
        }

        public IImmutableTree<TKey, TValue> Left
        {
            get { throw new ArgumentOutOfRangeException(); }
        }

        public IImmutableTree<TKey, TValue> LeftMost
        {
            get { return this; }
        }

        public IImmutableTree<TKey, TValue> Right
        {
            get { throw new ArgumentOutOfRangeException(); }
        }

        public IImmutableTree<TKey, TValue> RightMost
        {
            get { return this; }
        }

        public TKey RootKey
        {
            get { throw new ArgumentOutOfRangeException(); }
        }

        public TValue RootValue
        {
            get { throw new ArgumentOutOfRangeException(); }
        }

        public IEnumerable<TKey> Keys
        {
            get { return Enumerable.Empty<TKey>(); }
        }

        public IEnumerable<TKey> KeysDescending
        {
            get { return Enumerable.Empty<TKey>(); }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> Items
        {
            get { return Enumerable.Empty<KeyValuePair<TKey, TValue>>(); }
        }

        public IEnumerable<TValue> Values
        {
            get { return Enumerable.Empty<TValue>(); }
        }

        public IEnumerable<TValue> ValuesDescending
        {
            get { return Enumerable.Empty<TValue>(); }
        }

        public IEnumerable<TValue> FindGreaterThan(TKey key)
        {
            return Enumerable.Empty<TValue>();
        }

        public IEnumerable<TValue> FindGreaterOrEqualTo(TKey key)
        {
            return Enumerable.Empty<TValue>();
        }

        public IEnumerable<TValue> FindLesserThan(TKey key)
        {
            return Enumerable.Empty<TValue>();
        }

        public IEnumerable<TValue> FindLessorOrEqualTo(TKey key)
        {
            return Enumerable.Empty<TValue>();
        }

        public IImmutableTree<TKey, TValue> Search(TKey key)
        {
            return this;
        }

        public IImmutableTree<TKey, TValue> Search(TKey key, Predicate<TValue> condition)
        {
            return this;
        }

        public bool Contains(TKey key)
        {
            return false;
        }

        public Tuple<bool, TValue> TryGetValue(TKey key)
        {
            return Tuple.Create(false, default(TValue));
        }

        public int Count
        {
            get { return 0; }
        }

        public bool IsEmpty
        {
            get { return true; }
        }

        public JObject ToJObject()
        {
            return new JObject();
        }

        #endregion
    }

    public sealed class ImmutableTree<TKey, TValue> : IImmutableTree<TKey, TValue>
    {
        private const int _heavy = 2;
        private const int _negHeavy = -1*_heavy;
        private readonly IComparer<TKey> _comparer;
        private readonly int _height;
        private readonly TKey _key;
        private readonly Func<TKey, TKey> _keyCloner;
        private readonly IImmutableTree<TKey, TValue> _left;
        private readonly IImmutableTree<TKey, TValue> _right;
        private readonly TValue _value;
        private readonly Func<TValue, TValue> _valueCloner;


        internal ImmutableTree(IComparer<TKey> comparer, Func<TKey, TKey> keyCloner, Func<TValue, TValue> valueCloner,
                         TKey key, TValue value,
                         IImmutableTree<TKey, TValue> left,
                         IImmutableTree<TKey, TValue> right)
        {
            _comparer = comparer;
            _keyCloner = keyCloner;
            _valueCloner = valueCloner;
            _key = key;
            _value = value;
            _left = left;
            _right = right;
            _height = 1 + Math.Max(TreeHeight(_left), TreeHeight(_right));
            Count = 1 + Left.Count + Right.Count;
        }

        #region IImmutableTree<TKey,TValue> Members

        public IImmutableTree<TKey, TValue> Add(TKey key, TValue value)
        {
            var comparison = _comparer.Compare(key, _key);
            if (0 == comparison)
                return new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, key, value, Left, Right);

            ImmutableTree<TKey, TValue> result = comparison > 0
                                               ? new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, _key,
                                                                           _value, Left, Right.Add(key, value))
                                               : new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, _key,
                                                                           _value, Left.Add(key, value), Right);

            return BalanceTree(result);
        }

        public IImmutableTree<TKey, TValue> AddOrUpdate(TKey key, TValue value, Func<TKey, TValue, TValue> updateValueFactory)
        {
            ImmutableTree<TKey, TValue> result;
            var comparison = _comparer.Compare(key, _key);

            if (comparison > 0)
                result = new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, _key, _value, Left,
                                                   Right.AddOrUpdate(key, value, updateValueFactory));
            else if (comparison < 0)
                result = new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, _key, _value,
                                                   Left.AddOrUpdate(key, value, updateValueFactory), Right);
            else
            {
                result = new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, key,
                                                   updateValueFactory(key, _value), Left, Right);
            }

            return BalanceTree(result);
        }

        public Tuple<IImmutableTree<TKey, TValue>, bool, TValue> TryRemove(TKey key)
        {
            IImmutableTree<TKey, TValue> result;
            var comparison = _comparer.Compare(key, _key);
            bool removed;
            TValue val;

            if (0 == comparison)
            {
                removed = true;
                val = _value;

                if (Right.IsEmpty && Left.IsEmpty)
                {
                    result = new ImmutableTreeAcorn<TKey, TValue>(_comparer, _keyCloner, _valueCloner);
                }
                else if (Right.IsEmpty && !Left.IsEmpty)
                {
                    result = Left;
                }
                else if (!Right.IsEmpty && Left.IsEmpty)
                {
                    result = Right;
                }
                else
                {
                    var s = Right;
                    while (!s.Left.IsEmpty)
                        s = s.Left;
                    result = new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, s.RootKey, s.RootValue, Left,
                                                       Right.TryRemove(s.RootKey).Item1);
                }
            }
            else if (comparison < 0)
            {
                var removal = Left.TryRemove(key);
                removed = removal.Item2;
                val = removal.Item3;
                result = new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, _key, _value, removal.Item1,
                                                   Right);
            }
            else
            {
                var removal = Right.TryRemove(key);
                removed = removal.Item2;
                val = removal.Item3;
                result = new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, _key, _value, Left,
                                                   removal.Item1);
            }

            var br = BalanceTree(result);
            return Tuple.Create(br, removed, val);
        }

        public IImmutableTree<TKey, TValue> Left
        {
            get { return _left; }
        }

        public IImmutableTree<TKey, TValue> LeftMost
        {
            get
            {
                if (Left.IsEmpty)
                    return this;
                var each = Left;
                while (!each.Left.IsEmpty)
                    each = each.Left;
                return each.LeftMost;
            }
        }

        public IImmutableTree<TKey, TValue> Right
        {
            get { return _right; }
        }

        public IImmutableTree<TKey, TValue> RightMost
        {
            get
            {
                if (Right.IsEmpty)
                    return this;
                var each = Right;
                while (!each.Right.IsEmpty)
                    each = each.Right;
                return each.RightMost;
            }
        }

        public TKey RootKey
        {
            get { return _keyCloner(_key); }
        }

        public TValue RootValue
        {
            get { return _valueCloner(_value); }
        }

        public IEnumerable<TKey> Keys
        {
            get { return Ascending().Select(i => i.RootKey); }
        }

        public IEnumerable<TKey> KeysDescending
        {
            get { return Descending().Select(i => i.RootKey); }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> Items
        {
            get { return Ascending().Select(i => new KeyValuePair<TKey, TValue>(i.RootKey, i.RootValue)); }
        }

        public IEnumerable<TValue> Values
        {
            get { return Ascending().Select(i => i.RootValue); }
        }

        public IEnumerable<TValue> ValuesDescending
        {
            get { return Descending().Select(i => i.RootValue); }
        }

        public IEnumerable<TValue> FindGreaterThan(TKey key)
        {
            var comparison = _comparer.Compare(_key, key);
            if (comparison <= 0)
            {
                foreach (var each in Right.FindGreaterThan(key))
                    yield return each;
                yield break;
            }

            foreach (var each in Left.FindGreaterThan(key))
            {
                yield return each;
            }

            foreach (var each in Right.FindGreaterThan(key))
            {
                yield return each;
            }
        }

        public IEnumerable<TValue> FindGreaterOrEqualTo(TKey key)
        {
            var comparison = _comparer.Compare(_key, key);
            if (comparison < 0)
            {
                foreach (var each in Right.FindGreaterOrEqualTo(key))
                    yield return each;
                yield break;
            }

            foreach (var each in Left.FindGreaterOrEqualTo(key))
            {
                yield return each;
            }

            yield return _value;

            foreach (var each in Right.FindGreaterOrEqualTo(key))
            {
                yield return each;
            }
        }

        public IEnumerable<TValue> FindLesserThan(TKey key)
        {
            var comparison = _comparer.Compare(_key, key);
            if (comparison < 0)
            {
                foreach (var each in Right.FindLesserThan(key))
                {
                    yield return each;
                }
            }

            if (comparison <= 0)
                yield return _value;

            foreach (var each in Left.FindLesserThan(key))
            {
                yield return each;
            }
        }

        public IEnumerable<TValue> FindLessorOrEqualTo(TKey key)
        {
            var comparison = _comparer.Compare(_key, key);
            if (comparison < 0)
            {
                foreach (var each in Right.FindLessorOrEqualTo(key))
                    yield return each;
            }

            if (comparison <= 0)
                yield return _value;

            foreach (var each in Left.FindLessorOrEqualTo(key))
                yield return each;
        }

        public IImmutableTree<TKey, TValue> Search(TKey key)
        {
            var comparison = _comparer.Compare(key, _key);
            if (0 != comparison)
            {
                return comparison > 0 ? Right.Search(key) : Left.Search(key);
            }

            return this;
        }

        public IImmutableTree<TKey, TValue> Search(TKey key, Predicate<TValue> condition)
        {
            if (condition(_value))
                return this;

            var comparison = _comparer.Compare(key, _key);
            if (0 != comparison)
            {
                return comparison > 0 ? Right.Search(key, condition) : Left.Search(key, condition);
            }

            return this;
        }

        public bool Contains(TKey key)
        {
            return !Search(key).IsEmpty;
        }

        public Tuple<bool, TValue> TryGetValue(TKey key)
        {
            var tree = Search(key);
            return tree.IsEmpty ? Tuple.Create(false, default(TValue)) : Tuple.Create(true, tree.RootValue);
        }

        public int Count { get; private set; }

        public bool IsEmpty
        {
            get { return false; }
        }

        public JObject ToJObject()
        {
            return new JObject
                       {
                           {"key", JToken.FromObject(_key)},
                           {"value", JToken.FromObject(_value)},
                           {"left", Left.ToJObject()},
                           {"right", Right.ToJObject()}
                       };
        }

        #endregion

        private IEnumerable<IImmutableTree<TKey, TValue>> Ascending()
        {
            var st = ImmutableStack<IImmutableTree<TKey, TValue>>.Start;
            for (IImmutableTree<TKey, TValue> each = this; !each.IsEmpty || !st.IsEmpty; each = each.Right)
            {
                while (!each.IsEmpty)
                {
                    st = st.Push(each);
                    each = each.Left;
                }

                each = st.Peek();
                st = st.Pop();
                yield return each;
            }
        }

        private IEnumerable<IImmutableTree<TKey, TValue>> Descending()
        {
            var st = ImmutableStack<IImmutableTree<TKey, TValue>>.Start;
            for (IImmutableTree<TKey, TValue> each = this; !each.IsEmpty || !st.IsEmpty; each = each.Left)
            {
                while (!each.IsEmpty)
                {
                    st = st.Push(each);
                    each = each.Right;
                }

                each = st.Peek();
                st = st.Pop();
                yield return each;
            }
        }

        private static int TreeHeight(IImmutableTree<TKey, TValue> tree)
        {
            return tree.IsEmpty ? 0 : ((ImmutableTree<TKey, TValue>) tree)._height;
        }

        private IImmutableTree<TKey, TValue> RotateLeft(IImmutableTree<TKey, TValue> tree)
        {
            if (tree.Right.IsEmpty)
                return tree;
            return new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, tree.Right.RootKey,
                                             tree.Right.RootValue,
                                             new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, tree.RootKey,
                                                                       tree.RootValue, tree.Left, tree.Right.Left),
                                             tree.Right.Right);
        }

        private IImmutableTree<TKey, TValue> RotateRight(IImmutableTree<TKey, TValue> tree)
        {
            if (tree.Left.IsEmpty)
                return tree;
            return new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, tree.Left.RootKey, tree.Left.RootValue,
                                             tree.Left.Left,
                                             new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, tree.RootKey,
                                                                       tree.RootValue, tree.Left.Right, tree.Right));
        }

        private IImmutableTree<TKey, TValue> RotateLeftLeft(IImmutableTree<TKey, TValue> tree)
        {
            if (tree.Right.IsEmpty)
                return tree;
            var rotatedRightChild = new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, tree.RootKey,
                                                              tree.RootValue, tree.Left, RotateRight(tree.Right));
            return RotateLeft(rotatedRightChild);
        }

        private IImmutableTree<TKey, TValue> RotateRightRight(IImmutableTree<TKey, TValue> tree)
        {
            if (tree.Left.IsEmpty)
                return tree;
            var rotatedLeftChild = new ImmutableTree<TKey, TValue>(_comparer, _keyCloner, _valueCloner, tree.RootKey,
                                                             tree.RootValue, RotateLeft(tree.Left), tree.Right);
            return RotateRight(rotatedLeftChild);
        }

        private static int TreeLeaning(IImmutableTree<TKey, TValue> tree)
        {
            if (tree.IsEmpty)
                return 0;
            return TreeHeight(tree.Right) - TreeHeight(tree.Left);
        }

        private static bool RightHeavy(IImmutableTree<TKey, TValue> tree)
        {
            return TreeLeaning(tree) >= _heavy;
        }

        private static bool LeansLeft(IImmutableTree<TKey, TValue> tree)
        {
            return TreeLeaning(tree) <= _negHeavy;
        }

        private IImmutableTree<TKey, TValue> BalanceTree(IImmutableTree<TKey, TValue> tree)
        {
            IImmutableTree<TKey, TValue> result;
            if (RightHeavy(tree))
            {
                result = LeansLeft(tree.Right) ? RotateLeftLeft(tree) : RotateLeft(tree);
            }
            else if (LeansLeft(tree))
            {
                result = RightHeavy(tree.Left) ? RotateRightRight(tree) : RotateRight(tree);
            }
            else
                result = tree;
            return result;
        }
    }
}