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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace AppComponents.Dynamic
{

    #region Classes

    [Serializable]
    public class ShapeableExpandoList : AbstractShapeableExpando, IList<object>, IDictionary<string, object>,
                                        INotifyCollectionChanged, ITypedList, IList
    {
        protected static readonly object ListLock = new object();
        private readonly object _syncRoot = new object();
        protected IList<object> _list;

        public ShapeableExpandoList(
            IEnumerable<object> contents = null,
            IEnumerable<KeyValuePair<string, object>> members = null)
            : base(members)
        {
            if (contents == null)
            {
                _list = new List<object>();
                return;
            }
            if (contents is IList<object>)
            {
                _list = contents as IList<object>;
            }
            else
            {
                _list = contents.ToList();
            }
        }

        protected ShapeableExpandoList(SerializationInfo info,
                                       StreamingContext context)
            : base(info, context)
        {
            _list = info.GetValue<IList<object>>("_list");
        }

        public Func<IEnumerable<object>, IEnumerable<string>> OverrideGettingItemMethodNames { get; set; }

        #region IDictionary<string,object> Members

        dynamic IDictionary<string, object>.this[string key]
        {
            get { return _dictionary[key]; }
            set { SetProperty(key, value); }
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        #endregion

        #region IList Members

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return _syncRoot; }
        }

        public void CopyTo(Array array, int index)
        {
            ((IList) _list).CopyTo(array, index);
        }

        int IList.Add(object value)
        {
            Add(value);
            return Count - 1;
        }

        void IList.Remove(object value)
        {
            Remove(value);
        }

        #endregion

        #region IList<object> Members

        public int Count
        {
            get { return _list.Count; }
        }

        public dynamic this[int index]
        {
            get { return _list[index]; }
            set
            {
                object tOld;
                lock (ListLock)
                {
                    tOld = _list[index];
                    _list[index] = value;
                }

                OnCollectionChanged(NotifyCollectionChangedAction.Replace, tOld, value, index);
            }
        }


        public void Add(dynamic item)
        {
            InsertHelper(item);
        }

        public void Clear()
        {
            lock (ListLock)
            {
                _list.Clear();
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public bool Contains(dynamic item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<dynamic> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(dynamic item)
        {
            lock (ListLock)
            {
                return _list.IndexOf(item);
            }
        }

        public void Insert(int index, dynamic item)
        {
            InsertHelper(item, index);
        }

        public bool Remove(dynamic item)
        {
            return RemoveHelper(item);
        }

        public void RemoveAt(int index)
        {
            RemoveHelper(index: index);
        }

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region ITypedList Members

        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            IEnumerable<string> methodNames = new string[] {};
            if (OverrideGettingItemMethodNames != null)
            {
                methodNames = OverrideGettingItemMethodNames(this);
            }
            else
            {
                methodNames = InvocationBinding.GetMemberNames(GetRepresentedItem(), dynamicOnly: true);
            }

            return new PropertyDescriptorCollection(methodNames.Select(it => new MetaProperty(it)).ToArray());
        }

        /// <summary>
        ///   Returns the name of the list.
        /// </summary>
        /// <param name="listAccessors"> An array of <see cref="T:System.ComponentModel.PropertyDescriptor" /> objects, for which the list name is returned. This can be null. </param>
        /// <returns> The name of the list. </returns>
        public string GetListName(PropertyDescriptor[] listAccessors)
        {
            return null;
        }

        #endregion

        public bool Equals(ShapeableExpandoList other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return base.Equals(other) && Equals(other._list, _list);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return Equals(obj as ShapeableExpandoList);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode()*397) ^ _list.GetHashCode();
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (_list.OfType<Delegate>().Any())
            {
                throw new SerializationException("Cannot serialize prototype objects containing delegates");
            }
            base.GetObjectData(info, context);
            info.AddValue("_list", _list);
        }

        protected virtual dynamic GetRepresentedItem()
        {
            var item = ((IEnumerable<object>) this).FirstOrDefault();
            return item;
        }

        private void InsertHelper(object item, int? index = null)
        {
            lock (ListLock)
            {
                if (!index.HasValue)
                {
                    index = _list.Count;
                    _list.Add(item);
                }
                else
                {
                    _list.Insert(index.Value, item);
                }
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Add, newItem: item, newIndex: index);
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem = null,
                                                   object newItem = null, int? oldIndex = null, int? newIndex = null)
        {
            if (CollectionChanged != null)
            {
                switch (action)
                {
                    case NotifyCollectionChangedAction.Add:
                        CollectionChanged(this,
                                          new NotifyCollectionChangedEventArgs(action, newItem,
                                                                               newIndex.GetValueOrDefault()));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        CollectionChanged(this,
                                          new NotifyCollectionChangedEventArgs(action, oldItem,
                                                                               oldIndex.GetValueOrDefault()));
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        CollectionChanged(this,
                                          new NotifyCollectionChangedEventArgs(action, oldItem, newItem,
                                                                               oldIndex.GetValueOrDefault()));
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        CollectionChanged(this, new NotifyCollectionChangedEventArgs(action));
                        break;
                }
            }

            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                    OnPropertyChanged("Count");
                    break;
                case NotifyCollectionChangedAction.Remove:
                    OnPropertyChanged("Count");
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    OnPropertyChanged("Count");
                    break;
            }
        }

        private bool RemoveHelper(object item = null, int? index = null)
        {
            lock (ListLock)
            {
                if (item != null)
                {
                    index = _list.IndexOf(item);
                    if (index < 0)
                        return false;
                }

                item = item ?? _list[index.GetValueOrDefault()];
                _list.RemoveAt(index.GetValueOrDefault());
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, oldItem: item, oldIndex: index);

            return true;
        }
    }

    #endregion Classes
}