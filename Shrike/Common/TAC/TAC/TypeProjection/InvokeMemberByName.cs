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
using AppComponents.Dynamic.Lambdas;

namespace AppComponents.Dynamic
{

    #region Classes

    public sealed class InvokeMemberByName : MemberInvocationMoniker
    {
        public static readonly Func<string, Type[], InvokeMemberByName> Create =
            Return<InvokeMemberByName>
                .Arguments<string, Type[]>((n, a) => new InvokeMemberByName(n, a));

        public static readonly Func<string, InvokeMemberByName> CreateSpecialName =
            Return<InvokeMemberByName>
                .Arguments<string>(n => new InvokeMemberByName(n, true));

        public InvokeMemberByName(string name, params Type[] genericArguments)
        {
            Name = name;
            GenericArguments = genericArguments;
        }

        public InvokeMemberByName(string name, bool isNameSpecial)
        {
            Name = name;
            GenericArguments = new Type[] {};
            IsNameSpecial = isNameSpecial;
        }


        public bool Equals(InvokeMemberByName other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return EqualsHelper(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (!(obj is InvokeMemberByName))
                return false;
            return EqualsHelper((InvokeMemberByName) obj);
        }

        private bool EqualsHelper(InvokeMemberByName other)
        {
            var genericArguments = GenericArguments;
            var otherGenericArguments = other.GenericArguments;

            return Equals(other.Name, Name) &&
                   !(other.IsNameSpecial ^ IsNameSpecial) &&
                   !(otherGenericArguments == null ^ genericArguments == null) &&
                   (genericArguments == null ||
                    genericArguments.SequenceEqual(otherGenericArguments));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (GenericArguments != null ? GenericArguments.GetHashCode()*397 : 0) ^ (Name.GetHashCode());
            }
        }

        public static implicit operator InvokeMemberByName(string name)
        {
            return new InvokeMemberByName(name, null);
        }
    }

    #endregion Classes
}