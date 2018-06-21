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
using System.Runtime.Serialization;

namespace AppComponents.Dynamic
{

    #region Classes

    [Serializable]
    public abstract class AbstractLazyInterceptor : AbstractInterceptor
    {
        protected AbstractLazyInterceptor(object target)
            : base(target)
        {
        }

        protected AbstractLazyInterceptor(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public static dynamic Create<T>(Func<T> valuefactory)
        {
            return new LazyInterceptor<T>(valuefactory);
        }

        public static dynamic Create<T>(Lazy<T> target)
        {
            return new LazyInterceptor<T>(target);
        }
    }

    public class LazyInterceptor<T> : AbstractLazyInterceptor
    {
        public LazyInterceptor(Lazy<T> target)
            : base(target)
        {
        }

        public LazyInterceptor(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public LazyInterceptor(Func<T> valueFactory)
            : base(new Lazy<T>(valueFactory))
        {
        }

        protected override object CallTarget
        {
            get { return ((Lazy<T>) OriginalTarget).Value; }
        }


        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ((Lazy<T>) OriginalTarget).IsValueCreated
                       ? base.GetDynamicMemberNames()
                       : Enumerable.Empty<string>();
        }
    }

    #endregion Classes
}