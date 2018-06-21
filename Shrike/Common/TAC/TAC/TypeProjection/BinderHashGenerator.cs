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
using System.Linq;

namespace AppComponents.Dynamic
{

    #region Classes

    internal class AbstractGenericBinderHashGenerator : BinderHashGenerator
    {
        protected AbstractGenericBinderHashGenerator(Type delegateType, MemberInvocationMoniker name, Type context,
                                                     string[] argumentNames, Type binderType, bool staticContext,
                                                     bool isEvent)
            : base(delegateType, name, context, argumentNames, binderType, staticContext, isEvent)
        {
        }
    }

    internal class BinderHashGenerator
    {
        protected BinderHashGenerator(Type delegateType, MemberInvocationMoniker name, Type context,
                                      string[] argumentNames, Type binderType, bool staticContext, bool isEvent)
        {
            BinderType = binderType;
            StaticContext = staticContext;
            DelegateType = delegateType;
            Name = name;
            Context = context;
            ArgumentNames = argumentNames;
            IsEvent = isEvent;
        }

        public string[] ArgumentNames { get; protected set; }

        public Type BinderType { get; protected set; }

        public Type Context { get; protected set; }

        public Type DelegateType { get; protected set; }

        public bool IsEvent { get; protected set; }

        public MemberInvocationMoniker Name { get; protected set; }

        public bool StaticContext { get; protected set; }


        public static BinderHashGenerator Create(Type delType, MemberInvocationMoniker name, Type context,
                                                 string[] argumentNames, Type binderType, bool staticContext,
                                                 bool isEvent)
        {
            return new BinderHashGenerator(delType, name, context, argumentNames, binderType, staticContext, isEvent);
        }

        public virtual bool Equals(BinderHashGenerator other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            var tArgNames = ArgumentNames;
            var tOtherArgNames = other.ArgumentNames;

            return
                !(tOtherArgNames == null ^ tArgNames == null) &&
                other.IsEvent == IsEvent &&
                other.StaticContext == StaticContext &&
                Equals(other.Context, Context) &&
                Equals(other.BinderType, BinderType) &&
                Equals(other.DelegateType, DelegateType) &&
                Equals(other.Name, Name) &&
                (null == tArgNames || tOtherArgNames.SequenceEqual(tArgNames));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (!(obj is BinderHashGenerator))
                return false;
            return Equals((BinderHashGenerator) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var argumentNames = ArgumentNames;
                return Hash.GetCombinedHashCodeForHashes(Hash.GetCombinedHashCodeForCollection(argumentNames),
                                                         StaticContext.GetHashCode(), DelegateType.GetHashCode(),
                                                         Context.GetHashCode(), Name.GetHashCode());
            }
        }
    }

    internal class BinderHashGenerator<T> : AbstractGenericBinderHashGenerator where T : class
    {
        protected BinderHashGenerator(MemberInvocationMoniker name, Type context, string[] argumentNames,
                                      Type binderType, bool staticContext, bool isEvent)
            : base(typeof (T), name, context, argumentNames, binderType, staticContext, isEvent)
        {
        }

        public static BinderHashGenerator<T> Create(MemberInvocationMoniker name, Type context, string[] argumentNames,
                                                    Type binderType, bool staticContext, bool isEvent)
        {
            return new BinderHashGenerator<T>(name, context, argumentNames, binderType, staticContext, isEvent);
        }

        public override bool Equals(BinderHashGenerator other)
        {
            if (other is AbstractGenericBinderHashGenerator)
            {
                if (other is BinderHashGenerator<T>)
                {
                    return
                        !(other.ArgumentNames == null ^ ArgumentNames == null) &&
                        other.IsEvent == IsEvent &&
                        other.StaticContext == StaticContext &&
                        Equals(other.BinderType, BinderType) &&
                        Equals(other.Context, Context) &&
                        Equals(other.Name, Name) &&
                        (null == ArgumentNames || other.ArgumentNames.SequenceEqual(ArgumentNames));
                }
                return false;
            }
            return base.Equals(other);
        }
    }

    #endregion Classes
}