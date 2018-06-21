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
using AppComponents.Dynamic.Lambdas;

namespace AppComponents.Dynamic
{
    public class StaticContext : InvocationContext
    {
        public StaticContext(Type target)
            : base(target, true, null)
        {
        }

        public static explicit operator StaticContext(Type type)
        {
            return new StaticContext(type);
        }
    }

    [Serializable]
    public class InvocationContext
    {
        public static readonly Func<object, object, InvocationContext> CreateContext =
            Return<InvocationContext>.Arguments<object, object>((t, c) => new InvocationContext(t, c));

        public static readonly Func<Type, InvocationContext> CreateStatic =
            Return<InvocationContext>.Arguments<Type>((t) => new InvocationContext(t, true, null));

        public static readonly Func<Type, object, InvocationContext> CreateStaticWithContext =
            Return<InvocationContext>.Arguments<Type, object>((t, c) => new InvocationContext(t, true, c));

        public InvocationContext(Type target, bool staticContext, object context)
        {
            if (context != null && !(context is Type))
            {
                context = context.GetType();
            }
            Target = target;
            Context = ((Type) context) ?? target;
            StaticContext = staticContext;
        }

        public InvocationContext(object Target, object context)
        {
            this.Target = Target;

            if (context != null && !(context is Type))
            {
                context = context.GetType();
            }

            Context = (Type) context;
        }

        public object Target { get; protected set; }

        public Type Context { get; protected set; }

        public bool StaticContext { get; protected set; }
    }
}