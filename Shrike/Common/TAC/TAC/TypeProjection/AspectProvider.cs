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

namespace AppComponents.Dynamic
{

    #region Classes

    public abstract class AbstractAspectProvider
    {
        private readonly Guid _instanceId = Guid.NewGuid();


        public Guid InstanceId
        {
            get { return _instanceId; }
        }


        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var that = (AbstractAspectProvider) obj;
            return that.InstanceId == InstanceId;
        }

        public override int GetHashCode()
        {
            return _instanceId.GetHashCode();
        }

        public abstract IEnumerable<Aspect> ProvideAspects(Aspect.InterceptMode mode);
    }

    public class AspectProvider : AbstractAspectProvider
    {
        private readonly List<Aspect> _afterCache = new List<Aspect>();
        private readonly List<Aspect> _beforeCache = new List<Aspect>();
        private readonly List<Aspect> _insteadOfCache = new List<Aspect>();


        public AspectProvider(params Aspect[] aspects)
            : this(aspects.AsEnumerable())
        {
        }

        public AspectProvider(IEnumerable<Aspect> aspects)
        {
            Provide(aspects);
        }

        public AspectProvider(Enum configKey)
        {
            var config = Catalog.Factory.Resolve<IConfig>();
            var aspectsSpec = config[configKey].Split(';');

            var aspects =
                aspectsSpec.Select(catalogName => Catalog.Factory.Resolve<Aspect>(catalogName)).Where(
                    aspect => null != aspect).ToList();

            Provide(aspects);
        }

        private void Provide(IEnumerable<Aspect> aspects)
        {
            _beforeCache.AddRange(aspects.Where(a => a.Mode.HasFlag(Aspect.InterceptMode.Before)).Distinct());
            _insteadOfCache.AddRange(aspects.Where(a => a.Mode.HasFlag(Aspect.InterceptMode.Instead)).Distinct());
            _afterCache.AddRange(aspects.Where(a => a.Mode.HasFlag(Aspect.InterceptMode.After)).Distinct());
        }

        public override IEnumerable<Aspect> ProvideAspects(Aspect.InterceptMode mode)
        {
            IEnumerable<Aspect> retval = Enumerable.Empty<Aspect>();

            if (mode.HasFlag(Aspect.InterceptMode.Before))
            {
                retval = retval.Concat(_beforeCache);
            }

            if (mode.HasFlag(Aspect.InterceptMode.Instead))
            {
                retval = retval.Concat(_insteadOfCache);
            }

            if (mode.HasFlag(Aspect.InterceptMode.After))
            {
                retval = retval.Concat(_afterCache);
            }

            return retval;
        }
    }

    #endregion Classes
}