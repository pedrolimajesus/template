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
using System.Threading;
using AppComponents.Extensions.EnumEx;

namespace AppComponents.ControlFlow
{
    using AppComponents.Extensions.EnumerableEx;

    public static class ContextRegistry
    {
        public const string Kind = "ContextResourceKind";
        public const string Tenancy = "Tenancy";
        public const string Name = "ContextResourceName";
        public const string Path = "ContextResourcePath";
        private static readonly ThreadLocal<List<Uri>> NamedContexts = new ThreadLocal<List<Uri>>( () => new List<Uri>());

        public static IEnumerable<Uri> Contexts
        {
            get
            {
                var cps = Catalog.Factory.ResolveAll<IContextProvider>();

                var uris = new List<Uri>();
                foreach (var contextProvider in cps)
                {
                    var pctx = contextProvider.ProvideContexts();
                    uris.AddRange(pctx);
                }

                if (NamedContexts.Value.EmptyIfNull().Any())
                    uris.AddRange(NamedContexts.Value);

                return uris;
                //return
                //    _namedContexts.Value.Concat(
                //        Catalog.Factory.ResolveAll<IContextProvider>().SelectMany(provider => provider.ProvideContexts()))
                //        .Distinct();
            }
        }

        public static IEnumerable<Uri> ContextsOf(string contextMatch)
        {
            var uris = Contexts;
            var retVal = uris.Where(c => c.Host.Equals(contextMatch, StringComparison.OrdinalIgnoreCase));
            //var count = retVal.Count();
            return retVal.ToArray();
        }

        public static IDisposable NamedContextsFor(Type type, params Uri[] given)
        {
            var atts = type.GetCustomAttributes(typeof(NamedContextAttribute), true).Select(
                o => (NamedContextAttribute)o)
                .Concat(
                    type.Assembly.GetCustomAttributes(typeof(NamedContextAttribute), true).Select(
                        o => (NamedContextAttribute)o));

            var named = atts.Select(a => a.Context).Concat(given);


            NamedContexts.Value.AddRange(named);

            return Disposable.Create(() => { foreach (var nu in named) NamedContexts.Value.Remove(nu);  });
        }

        public static IDisposable NamedContextsFor(params Uri[] given)
        {
            NamedContexts.Value.AddRange(given);

            return Disposable.Create(() => NamedContexts.Value.Clear());
        }

        public static Uri CreateNamed(string name, string value)
        {
            return new Uri(string.Format("context://{0}/{1}", name, value));
        }

        public static Uri CreatedNamed(Enum name, Enum value)
        {
            return CreateNamed(name.EnumName(), value.EnumName());
        }

        public static Uri CreateNamed(Enum name, string value)
        {
            return CreateNamed(name.EnumName(), value);
        }
    }
}