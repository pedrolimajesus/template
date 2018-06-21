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
using Microsoft.CSharp.RuntimeBinder;

namespace AppComponents.Dynamic
{
    [Serializable]
    public enum InvocationKind
    {
        NotSet = 0,

        Convert,

        Get,

        Set,

        GetIndex,

        SetIndex,

        InvokeMember,

        InvokeMemberAction,

        InvokeMemberUnknown,

        Constructor,

        AddAssign,

        SubtractAssign,

        IsEvent,

        Invoke,

        InvokeAction,

        InvokeUnknown,
    }

    [Serializable]
    public class Invocation
    {
        public static readonly string ExplicitConvertBinderName = "(Explicit)";

        public static readonly string ImplicitConvertBinderName = "(Implicit)";

        public static readonly string IndexBinderName = "Item";

        public static readonly string ConstructorBinderName = "new()";

        public Invocation(InvocationKind kind, MemberInvocationMoniker name, params object[] storedArgs)
        {
            Kind = kind;
            Name = name;
            Arguments = storedArgs;
        }

        public InvocationKind Kind { get; protected set; }

        public MemberInvocationMoniker Name { get; protected set; }

        public object[] Arguments { get; protected set; }

        public static Invocation Create(InvocationKind kind, MemberInvocationMoniker name, params object[] storedArgs)
        {
            return new Invocation(kind, name, storedArgs);
        }


        public bool Equals(Invocation other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Equals(other.Kind, Kind) && Equals(other.Name, Name) &&
                   (Equals(other.Arguments, Arguments) || other.Arguments.SequenceEqual(Arguments));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof (Invocation))
                return false;
            return Equals((Invocation) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = Kind.GetHashCode();
                result = (result*397) ^ (Name != null ? Name.GetHashCode() : 0);
                result = (result*397) ^ (Arguments != null ? Arguments.GetHashCode() : 0);
                return result;
            }
        }

        public virtual object Invoke(object target, params object[] args)
        {
            switch (Kind)
            {
                case InvocationKind.Constructor:
                    return InvocationBinding.CreateInstance((Type) target, args);
                case InvocationKind.Convert:
                    bool tExplict = false;
                    if (Arguments.Length == 2)
                        tExplict = (bool) args[1];
                    return InvocationBinding.Conversion(target, (Type) args[0], tExplict);
                case InvocationKind.Get:
                    return InvocationBinding.InvokeGet(target, Name.Name);
                case InvocationKind.Set:
                    InvocationBinding.InvokeSet(target, Name.Name, args.FirstOrDefault());
                    return null;
                case InvocationKind.GetIndex:
                    return InvocationBinding.InvokeGetIndex(target, args);
                case InvocationKind.SetIndex:
                    InvocationBinding.InvokeSetIndex(target, args);
                    return null;
                case InvocationKind.InvokeMember:
                    return InvocationBinding.InvokeMember(target, Name, args);
                case InvocationKind.InvokeMemberAction:
                    InvocationBinding.InvokeMemberAction(target, Name, args);
                    return null;
                case InvocationKind.InvokeMemberUnknown:
                    {
                        try
                        {
                            return InvocationBinding.InvokeMember(target, Name, args);
                        }
                        catch (RuntimeBinderException)
                        {
                            InvocationBinding.InvokeMemberAction(target, Name, args);
                            return null;
                        }
                    }
                case InvocationKind.Invoke:
                    return InvocationBinding.Invoke(target, args);
                case InvocationKind.InvokeAction:
                    InvocationBinding.InvokeAction(target, args);
                    return null;
                case InvocationKind.InvokeUnknown:
                    {
                        try
                        {
                            return InvocationBinding.Invoke(target, args);
                        }
                        catch (RuntimeBinderException)
                        {
                            InvocationBinding.InvokeAction(target, args);
                            return null;
                        }
                    }
                case InvocationKind.AddAssign:
                    InvocationBinding.InvokeAddAssign(target, Name.Name, args.FirstOrDefault());
                    return null;
                case InvocationKind.SubtractAssign:
                    InvocationBinding.InvokeSubtractAssign(target, Name.Name, args.FirstOrDefault());
                    return null;
                case InvocationKind.IsEvent:
                    return InvocationBinding.InvokeIsEvent(target, Name.Name);
                default:
                    throw new InvalidOperationException("Unknown Invocation Kind: " + Kind);
            }
        }

        public virtual object InvokeWithStoredArgs(object target)
        {
            return Invoke(target, Arguments);
        }
    }
}