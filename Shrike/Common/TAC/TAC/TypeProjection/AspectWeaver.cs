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
using System.Dynamic;
using System.Linq;
using System.Reflection;
using AppComponents.Dynamic.Projection;
using AppComponents.Extensions.EnumEx;

namespace AppComponents.Dynamic
{

    #region Classes

    public class AspectWeaver<T> : IAspectWeaver
        where T : class

    {
        private static Dictionary<string, int> _defaultAspectCategoryOrder;

        private readonly Dictionary<string, Dictionary<MemberProjection, HashSet<AbstractAspectProvider>>> _mappings =
            new Dictionary<string, Dictionary<MemberProjection, HashSet<AbstractAspectProvider>>>();

        private Dictionary<string, int> _aspectCategoryOrdering = new Dictionary<string, int>();
        private bool _factoryReady;

        private Func<T> _targetFactory;

        static AspectWeaver()
        {
            _defaultAspectCategoryOrder = new Dictionary<string, int>();
            foreach (var item in Enum.GetNames(typeof (Aspect.CommonCategories)))
            {
                Aspect.CommonCategories ac;
                if (Enum.TryParse(item, out ac))
                    _defaultAspectCategoryOrder.Add(item, (int) ac);
            }
        }

        public AspectWeaver(Func<T> targetFactory)
        {
            AspectCategoryOrdering = new Dictionary<string, int>();
            _targetFactory = targetFactory;
        }


        public Dictionary<string, int> AspectCategoryOrdering
        {
            get { return _aspectCategoryOrdering; }

            set { _aspectCategoryOrdering = value; }
        }

        public Dictionary<string, int> DefaultAspectCategoryOrder
        {
            get { return _defaultAspectCategoryOrder; }
        }

        #region IAspectWeaver Members

        public IEnumerable<Aspect> After(GetMemberBinder binder)
        {
            return Find(binder, Aspect.InterceptMode.After);
        }

        public IEnumerable<Aspect> After(InvokeMemberBinder binder)
        {
            return Find(binder, Aspect.InterceptMode.After);
        }

        public IEnumerable<Aspect> After(SetMemberBinder binder)
        {
            return Find(binder, Aspect.InterceptMode.After);
        }

        public IEnumerable<Aspect> Before(GetMemberBinder binder)
        {
            return Find(binder, Aspect.InterceptMode.Before);
        }

        public IEnumerable<Aspect> Before(InvokeMemberBinder binder)
        {
            return Find(binder, Aspect.InterceptMode.Before);
        }

        public IEnumerable<Aspect> Before(SetMemberBinder binder)
        {
            return Find(binder, Aspect.InterceptMode.Before);
        }

        public IEnumerable<Aspect> InsteadOf(GetMemberBinder binder)
        {
            return Find(binder, Aspect.InterceptMode.Instead);
        }

        public IEnumerable<Aspect> InsteadOf(InvokeMemberBinder binder)
        {
            return Find(binder, Aspect.InterceptMode.Instead);
        }

        public IEnumerable<Aspect> InsteadOf(SetMemberBinder binder)
        {
            return Find(binder, Aspect.InterceptMode.Instead);
        }

        #endregion

        public Func<TItf> CreateFactory<TItf>()
            where TItf : class
        {
            MaybeSetCategoryOrderToDefault();
            _factoryReady = true;
            return CreateInstance<TItf>;
        }


        private TInstance CreateInstance<TInstance>()
            where TInstance : class
        {
            return new AspectInterceptor<TInstance>(_targetFactory(), this).DressedAs<TInstance>();
        }

        private IEnumerable<Aspect> Find(string name, Type returnType, MemberTypes kind, Aspect.InterceptMode mode)
        {
            if (!_mappings.ContainsKey(name))
                return null;

            var lt = _mappings[name];
            var mp = lt.Keys.FirstOrDefault(p => p.ReturnType == returnType && p.MemberType == kind);

            if (mp == null)
                return Enumerable.Empty<Aspect>();

            var aspects = (from a in lt[mp].SelectMany(ap => ap.ProvideAspects(mode))
                           orderby _aspectCategoryOrdering[a.Category.EnumName()]
                           select a).Distinct();

            return aspects;
        }

        private IEnumerable<Aspect> Find(
            string name,
            Type returnType,
            MemberTypes kind,
            int argCount,
            IEnumerable<string> argumentNames,
            Aspect.InterceptMode mode)
        {
            if (!_mappings.ContainsKey(name))
                return null;

            var lt = _mappings[name];
            var mp = lt.Keys
                .FirstOrDefault(
                    p => p.ReturnType == returnType &&
                         p.MemberType == kind &&
                         p.ArgumentCount == argCount &&
                         p.GetArgumentNamesHashCode() == MemberProjection.HashArgumentNames(argumentNames));

            if (mp == null)
                return Enumerable.Empty<Aspect>();

            var aspects = (from a in lt[mp].SelectMany(ap => ap.ProvideAspects(mode))
                           orderby _aspectCategoryOrdering[a.Category.EnumName()]
                           select a).Distinct();

            return aspects;
        }

        private IEnumerable<Aspect> Find(GetMemberBinder binder, Aspect.InterceptMode mode)
        {
            return Find(binder.Name, binder.ReturnType, MemberTypes.Property, mode);
        }

        private IEnumerable<Aspect> Find(InvokeMemberBinder binder, Aspect.InterceptMode mode)
        {
            return Find(
                binder.Name,
                binder.ReturnType,
                MemberTypes.Method,
                binder.CallInfo.ArgumentCount,
                binder.CallInfo.ArgumentNames,
                mode);
        }

        private IEnumerable<Aspect> Find(SetMemberBinder binder, Aspect.InterceptMode mode)
        {
            return Find(binder.Name, binder.ReturnType, MemberTypes.Property, mode);
        }

        private void MaybeSetCategoryOrderToDefault()
        {
            if (!AspectCategoryOrdering.Any())
                _aspectCategoryOrdering = _defaultAspectCategoryOrder;
        }

        public AspectWeaver<T> WeaveAspects(
            AbstractAspectProvider aspectProvider,
            params IEnumerable<MemberProjection>[] pointCuts)
        {
            if (_factoryReady)
                throw new InvalidOperationException(
                    "Cannot change an aspect weaver once it provides a factory. Bad stuff would happen.");

            var namedGroups = from v in pointCuts.SelectMany(x => x)
                              group v by v.Name
                              into nameGroup
                              select new
                                         {
                                             Name = nameGroup.Key,
                                             PointCuts = nameGroup
                                         };

            foreach (var ng in namedGroups)
            {
                if (!_mappings.ContainsKey(ng.Name))
                {
                    _mappings.Add(ng.Name, new Dictionary<MemberProjection, HashSet<AbstractAspectProvider>>());
                }

                var lookupTable = _mappings[ng.Name];

                foreach (var pc in ng.PointCuts)
                {
                    if (!lookupTable.ContainsKey(pc))
                    {
                        lookupTable.Add(pc, new HashSet<AbstractAspectProvider>());
                    }

                    lookupTable[pc].Add(aspectProvider);
                }
            }

            return this;
        }

        public AspectWeaver<T> WeaveAspects(
            AbstractAspectProvider aspectProvider,
            params Func<FromClass<T>, IEnumerable<MemberProjection>>[] pointCutsDelegates)
        {
            return WeaveAspects(aspectProvider, pointCutsDelegates.SelectMany(pcd => pcd((new FromClass<T>()))));
        }
    }


    public static class AspectWeaver
    {
        public static AspectWeaver<T> Of<T>(Func<T> targetFactory) where T : class
        {
            return new AspectWeaver<T>(targetFactory);
        }
    }

    #endregion Classes
}