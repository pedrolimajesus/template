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

    public class Activate
    {
        public Activate(Type type, params object[] args)
        {
            Type = type;

            var tArg = args.OfType<Func<object[]>>().SingleOrDefault();
            if (tArg != null)
                Arguments = tArg;
            else
                Arguments = () => args;
        }

        public Activate(Type type, Func<object[]> args)
        {
            Type = type;
            Arguments = args;
        }

        public virtual Func<object[]> Arguments { get; private set; }

        public virtual Type Type { get; private set; }

        public virtual dynamic Create()
        {
            object[] tArgs = Arguments();
            return InvocationBinding.CreateInstance(Type, tArgs);
        }
    }

    public class Activate<TObjectPrototype> : Activate
    {
        public Activate(params object[] args)
            : base(typeof (TObjectPrototype), args)
        {
        }

        public Activate(Func<object[]> args)
            : base(typeof (TObjectPrototype), args)
        {
        }

        public override dynamic Create()
        {
            var theArguments = Arguments();

            if (theArguments.Any())
                return base.Create();


            TObjectPrototype objectPrototype;
            try
            {
                objectPrototype = Activator.CreateInstance<TObjectPrototype>();
            }
            catch (MissingMethodException)
            {
                objectPrototype = InvocationBinding.CreateInstance(typeof (TObjectPrototype));
            }
            return objectPrototype;
        }
    }

    public static class Build
    {
        private static readonly dynamic _listBuilder =
            InvocationBinding.Curry(new Builder<ChainingShapeableExpando>().ListSetup<ShapeableExpandoList>()).
                List();

        private static readonly dynamic _objectBuilder = new Builder<ChainingShapeableExpando>().Object;


        public static dynamic NewList
        {
            get { return _listBuilder; }
        }

        public static dynamic NewObject
        {
            get { return _objectBuilder; }
        }
    }

    public static class Build<TObjectPrototype> where TObjectPrototype : new()
    {
        private static readonly dynamic _typedBuilder = new Builder<TObjectPrototype>().Object;

        private static readonly dynamic _typedListBuilder =
            InvocationBinding.Curry(new Builder<TObjectPrototype>().ListSetup<TObjectPrototype>()).List();


        public static dynamic NewList
        {
            get { return _typedListBuilder; }
        }

        public static dynamic NewObject
        {
            get { return _typedBuilder; }
        }
    }

    public static class Builder
    {
        public static IBuilder New()
        {
            return new Builder<ChainingShapeableExpando>();
        }

        public static IBuilder New<TObjectPrototype>() where TObjectPrototype : new()
        {
            return new Builder<TObjectPrototype>();
        }
    }

    #endregion Classes
}