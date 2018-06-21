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
using System.Reflection;

namespace AppComponents.Dynamic
{
    public class TypeHasher
    {
        public readonly IDictionary<string, Type> InformalInterface;
        public readonly MemberInfo[] Types;

        public TypeHasher(IEnumerable<Type> moreTypes)
            : this(false, moreTypes.ToArray())
        {
        }

        private TypeHasher()
        {
        }

        public TypeHasher(Type type1, params Type[] moreTypes)
            : this()
        {
            Types = new[] {type1}.Concat(moreTypes.OrderBy(it => it.Name)).ToArray();
            InformalInterface = null;
        }

        public TypeHasher(Type type1, IDictionary<string, Type> informalInterface)
            : this()
        {
            Types = new[] {type1};
            InformalInterface = informalInterface;
        }

        public TypeHasher(bool strictOrder, params MemberInfo[] moreTypes)
            : this()
        {
            Types = strictOrder
                        ? moreTypes
                        : moreTypes.OrderBy(it => it.Name).ToArray();
        }

        public bool Equals(TypeHasher other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            if (InformalInterface != null || other.InformalInterface != null)
            {
                if (InformalInterface == null || other.InformalInterface == null)
                    return false;

                if (Types.Length != other.Types.Length)
                    return false;

                var tTypes = Types.SequenceEqual(other.Types);

                if (!tTypes)
                    return false;

                return InformalInterface.SequenceEqual(other.InformalInterface);
            }

            return Types.SequenceEqual(other.Types);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof (TypeHasher))
                return false;
            return Equals((TypeHasher) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                if (Types.Length > 16)
                {
                    return Types.Length.GetHashCode();
                }

                var tReturn = Types.Aggregate(1, (current, type) => (current*397) ^ type.GetHashCode());

                if (InformalInterface != null)
                {
                    tReturn = InformalInterface.Aggregate(tReturn, (current, type) => (current*397) ^ type.GetHashCode());
                }
                return tReturn;
            }
        }

        public static bool operator ==(TypeHasher left, TypeHasher right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TypeHasher left, TypeHasher right)
        {
            return !Equals(left, right);
        }

        public static TypeHasher Create(IEnumerable<Type> moreTypes)
        {
#pragma warning disable 612,618
            return new TypeHasher(moreTypes);
#pragma warning restore 612,618
        }

        public static TypeHasher Create(Type type1, params Type[] moreTypes)
        {
#pragma warning disable 612,618
            return new TypeHasher(type1, moreTypes);
#pragma warning restore 612,618
        }

        public static TypeHasher Create(Type type1, IDictionary<string, Type> informalInterface)
        {
#pragma warning disable 612,618
            return new TypeHasher(type1, informalInterface);
#pragma warning restore 612,618
        }

        public static TypeHasher Create(bool strictOrder, params MemberInfo[] moreTypes)
        {
#pragma warning disable 612,618
            return new TypeHasher(strictOrder, moreTypes);
#pragma warning restore 612,618
        }
    }
}