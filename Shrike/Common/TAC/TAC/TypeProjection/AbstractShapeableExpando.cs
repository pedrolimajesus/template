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
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CSharp.RuntimeBinder;

namespace AppComponents.Dynamic
{

    #region Classes

    [Serializable]
    public abstract class AbstractShapeableExpando : ShapeableObject, INotifyPropertyChanged
    {
        protected IDictionary<string, object> _dictionary;

        protected AbstractShapeableExpando(SerializationInfo info,
                                           StreamingContext context)
            : base(info, context)
        {
            _dictionary = info.GetValue<IDictionary<string, object>>("_dictionary");
        }

        protected AbstractShapeableExpando(IEnumerable<KeyValuePair<string, object>> dict = null)
        {
            if (dict == null)
            {
                _dictionary = new Dictionary<string, object>();
                return;
            }

            if (dict is IDictionary<string, object>)
                _dictionary = (IDictionary<string, object>) dict;
            else
                _dictionary = dict.ToDictionary(k => k.Key, v => v.Value);
        }


        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        public ICollection<string> Keys
        {
            get { return _dictionary.Keys; }
        }

        public ICollection<object> Values
        {
            get { return _dictionary.Values; }
        }

        public IDictionary<string, object> Members
        {
            get { return _dictionary; }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void Add(KeyValuePair<string, object> item)
        {
            SetProperty(item.Key, item.Value);
        }

        public void Add(string key, object value)
        {
            SetProperty(key, value);
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _dictionary.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
        }

        public bool Equals(ShapeableExpando other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other._dictionary, _dictionary);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ShapeableExpando)) return _dictionary.Equals(obj);
            return Equals((((ShapeableExpando) obj)));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return base.GetDynamicMemberNames().Concat(_dictionary.Keys).Distinct();
        }

        public override int GetHashCode()
        {
            return _dictionary.GetHashCode();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (_dictionary.Values.OfType<Delegate>().Any())
            {
                throw new SerializationException("Cannot serialize prototype objects containing delegates");
            }
            base.GetObjectData(info, context);
            info.AddValue("_dictionary", _dictionary);
        }

        protected virtual void OnPropertyChanged(string key)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(key));
                PropertyChanged(this, new PropertyChangedEventArgs("Item[]"));
            }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            object tValue;
            if (TryGetValue(item.Key, out tValue))
            {
                if (item.Value == tValue)
                {
                    Remove(item.Key);
                }
            }
            return false;
        }

        public bool Remove(string key)
        {
            var tReturn = _dictionary.Remove(key);
            OnPropertyChanged(key);
            return tReturn;
        }

        protected void SetProperty(string key, object value)
        {
            object tOldValue;
            if (!_dictionary.TryGetValue(key, out tOldValue) || value != tOldValue)
            {
                _dictionary[key] = value;
                OnPropertyChanged(key);
            }
        }

        public override string ToString()
        {
            return _dictionary.ToString();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (_dictionary.TryGetValue(binder.Name, out result))
            {
                return this.WireUpForInterface(binder.Name, true, ref result);
            }

            result = null;
            return this.WireUpForInterface(binder.Name, false, ref result);
        }

        public bool TryGetValue(string key, out object value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (_dictionary.TryGetValue(binder.Name, out result))
            {
                var functor = result as Delegate;
                if (result == null)
                    return false;
                if (!binder.CallInfo.ArgumentNames.Any() && functor != null)
                {
                    try
                    {
                        result = this.InvokeMethodDelegate(functor, args);
                    }
                    catch (RuntimeBinderException)
                    {
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        result = InvocationBinding.Invoke(result,
                                                          TypeFactorization.MaybeRenameArguments(binder.CallInfo, args));
                    }
                    catch (RuntimeBinderException)
                    {
                        return false;
                    }
                }
                return this.WireUpForInterface(binder.Name, true, ref result);
            }
            return this.WireUpForInterface(binder.Name, false, ref result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            SetProperty(binder.Name, value);
            return true;
        }
    }

    #endregion Classes
}