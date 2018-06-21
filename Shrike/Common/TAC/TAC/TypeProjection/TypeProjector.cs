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
using AppComponents.Dynamic.Lambdas;
using AppComponents.Extensions.EnumEx;
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents.Dynamic.Projection
{
    public class TypeProjector
    {
        private ShapeableSpecified _prototype = new ShapeableSpecified();

        public ShapeableSpecified Prototype
        {
            get { return _prototype; }
            private set { _prototype = value; }
        }

        public static TypeProjector Create()
        {
            return new TypeProjector();
        }


        public TypeProjector Construct(Func<dynamic, dynamic> buildSpec)
        {
            ChainingShapeableExpando specification =
                (ChainingShapeableExpando) buildSpec(Builder.New<ChainingShapeableExpando>());
            foreach (var item in specification)
                _prototype.AddMember(item.Key, item.Value);
            return this;
        }

        public TypeProjector Initializer(ThisAction initializer)
        {
            _prototype.Initializer = initializer;
            return this;
        }


        public TypeProjector SelectFrom<T>(params Func<FromProjectionOf<T>, IEnumerable<MemberProjection>>[] selections)
        {
            var fp = new FromProjectionOf<T>();
            ConsumeMemberProjections(selections.SelectMany(sf => sf(fp)));
            return this;
        }


        public TypeProjector EmbedInstanceOf<T>(Enum itemName, object initialValue)
        {
            _prototype.AddMember(itemName.EnumName(), initialValue);
            return this;
        }

        public TypeProjector EmbedInstanceOf<T>(Enum itemName)
        {
            _prototype.AddMember(itemName.EnumName(), typeof (T));
            return this;
        }

        public TypeProjector EmbedAndExpose<T>(Enum itemName,
                                               params Func<FromClass<T>, IEnumerable<MemberProjection>>[] selections)
        {
            EmbedInstanceOf<T>(itemName);

            var fc = new FromClass<T>();
            var members = selections.SelectMany(s => s(fc));

            foreach (var mp in members)
            {
                _prototype.ExposeMemberInner(itemName.EnumName(), typeof (T), mp);
            }

            return this;
        }

        private void ConsumeMemberProjections(IEnumerable<MemberProjection> projections)
        {
            projections.ForEach(mp => ConsumeMemberProjection(mp));
        }

        private void ConsumeMemberProjection(MemberProjection mp)
        {
            _prototype.AddMember(mp);
        }


        public TypeProjection Declare(string name = null)
        {
            return new TypeProjection(_prototype, name);
        }
    }

    public class TypeProjection
    {
        private ShapeableSpecified _cso;

        public string Name { get; set; }

        public TypeProjection(ShapeableSpecified cso, string staticName)
        {
            SetPrototype(cso);
            Name = staticName;
        }

        public IEnumerable<MemberProjection> Specification
        {
            get { return _cso.Specification; }
        }

        private void SetPrototype(ShapeableSpecified cso)
        {
            _cso = cso;
        }

        public TypeProjection Transform(Func<ShapeableSpecified, ShapeableSpecified> transformer)
        {
            SetPrototype(transformer(_cso));
            return this;
        }

        public Func<T> CreateFactoryDressedAs<T>(params Type[] otherInterfaces) where T : class
        {
            return () => _cso.Clone().DressedAs<T>(otherInterfaces);
        }

        public Func<dynamic> CreateFactoryWithInterfaces(params Type[] interfaces)
        {
            return Return<dynamic>.Arguments(CreateInstanceWithInterfaces(interfaces));
        }

        public Func<T> CreateFactoryDressedAs<T>() where T : class
        {
            return CreateDressedAs<T>;
        }

        private T CreateDressedAs<T>() where T : class
        {
            return _cso.Clone().DressedAs<T>();
        }

        private T CreateDressedAs<T>(params Type[] otherInterfaces) where T : class
        {
            return _cso.Clone().DressedAs<T>(otherInterfaces);
        }

        private dynamic CreateInstanceWithInterfaces(params Type[] otherInterfaces)
        {
            return _cso.Clone().DressedAs(otherInterfaces);
        }

        public dynamic CreateInstance()
        {
            return _cso.Clone();
        }
    }

    public static class TypeProjectionExtension
    {
        public static IEnumerable<MemberProjection> Transform(this IEnumerable<MemberProjection> that,
                                                              Func<MemberProjection, MemberProjection> xform)
        {
            return that.Select(mp => xform(mp));
        }
    }


    

}