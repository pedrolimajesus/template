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
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;

namespace AppComponents.Dynamic
{
    [Serializable]
    public class ShapeableExpando : AbstractShapeableExpando, IDictionary<string, object>
    {
        public ShapeableExpando()
        {
        }

        public ShapeableExpando(IEnumerable<KeyValuePair<string, object>> dict)
            : base(dict)
        {
        }

        protected ShapeableExpando(SerializationInfo info,
                                   StreamingContext context)
            : base(info, context)
        {
        }

        #region IDictionary<string,object> Members

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            var tKeys = Keys;

            _dictionary.Clear();

            foreach (var tKey in tKeys)
            {
                OnPropertyChanged(tKey);
            }
        }

        public object this[string key]
        {
            get { return _dictionary[key]; }
            set { SetProperty(key, value); }
        }

        #endregion

        public static T Create<T>(IEnumerable<KeyValuePair<string, object>> dict = null) where T : class
        {
            return dict == null
                       ? new ShapeableExpando().DressedAs<T>()
                       : new ShapeableExpando(dict).DressedAs<T>();
        }
    }

    [Serializable]
    public class ChainingShapeableExpando : ShapeableExpando
    {
        public ChainingShapeableExpando()
        {
        }

        public ChainingShapeableExpando(IEnumerable<KeyValuePair<string, object>> dict)
            : base(dict)
        {
        }

        protected ChainingShapeableExpando(SerializationInfo info,
                                           StreamingContext context)
            : base(info, context)
        {
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (base.TryInvokeMember(binder, args, out result))
            {
                return true;
            }
            if (binder.CallInfo.ArgumentCount == 1)
            {
                SetProperty(binder.Name, args.FirstOrDefault());
                result = this;
                return true;
            }
            if (binder.CallInfo.ArgumentCount > 1)
            {
                SetProperty(binder.Name, new ShapeableExpandoList(args));
                result = this;
                return true;
            }

            return false;
        }
    }
}