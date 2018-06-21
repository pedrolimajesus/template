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
using System.Threading;

using AppComponents.Extensions.EnumerableEx;

namespace AppComponents
{
    using System.Collections;

    public static class Catalog
    {
        private static volatile InstanceAssembler _assembler;

        private static object syncRoot = new Object();

        private static ThreadLocal<Stack<ConstructConfiguration>> _localFactory =
            new ThreadLocal<Stack<ConstructConfiguration>>(() => new Stack<ConstructConfiguration>());

        private static InstanceAssembler Instance
        {
            get
            {
                if (_assembler == null)
                {
                    lock (syncRoot)
                    {
                        if (_assembler == null)
                        {
                            _assembler = new InstanceAssembler();
                            _assembler.Register(
                                SpecialFactoryContexts.Routed,
                                _ =>
                                    {
                                        var exists = _localFactory.Value.Any();
                                        var lc = exists ? _localFactory.Value.Peek() : null;
                                        return lc ?? _assembler.Resolve<IConfig>();
                                    });

                            var itype = typeof(IObjectAssemblySpecifier);
                            var types = GetImplementerTypesOfInterface(itype);

                            foreach (Type type in types)
                            {
                                try
                                {
                                    ((IObjectAssemblySpecifier)Activator.CreateInstance(type)).RegisterIn(_assembler);
                                }
                                catch (Exception)
                                {
                                    
                                   
                                }
                            }
                        }
                    }
                }

                return _assembler;
            }
        }

        private static IEnumerable<Type> GetImplementerTypesOfInterface(Type interfaceType)
        {
            Assembly[] assemblies;


            try
            {
                assemblies = AppDomain.CurrentDomain
                                    .GetAssemblies()
                                    .Where(
                                            a =>
                                            !a.IsDynamic && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("Microsoft")
                                            && !a.FullName.StartsWith("DotNet") && !a.FullName.StartsWith("mscorlib")
                                            ).ToArray();
            }
            catch (Exception)
            {

                assemblies = new[]
                    {Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly(), Assembly.GetEntryAssembly()};
            }


            

            var types =
                assemblies.SelectMany
                    (delegate(Assembly s)
                    {
                        try
                        {
                            return s.GetTypes();
                        }
                        catch (Exception ex)
                        {
                            return Enumerable.Empty<Type>();
                        }
                        
                    })
                            .Where(p => p.IsClass && interfaceType.IsAssignableFrom(p))
                            .Distinct((a, b) => a.FullName == b.FullName);
            return types;
        }

        public static List<Type> GetSubclassesOf(this Type type, bool excludeSystemTypes)
        {
            var list = new List<Type>();
            IEnumerator enumerator = Thread.GetDomain().GetAssemblies().GetEnumerator();
            while (enumerator.MoveNext())
            {
                try
                {
                    var types = ((Assembly)enumerator.Current).GetTypes();
                    if (excludeSystemTypes && (((Assembly)enumerator.Current).FullName.StartsWith("System.")))
                    {
                        var enumerator2 = types.GetEnumerator();
                        while (enumerator2.MoveNext())
                        {
                            var current = (Type)enumerator2.Current;
                            if (type.IsInterface)
                            {
                                if (current.GetInterface(type.FullName) != null)
                                {
                                    list.Add(current);
                                }
                            }
                            else if (current.IsSubclassOf(type))
                            {
                                list.Add(current);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    //throw ex;
                }
            }
            return list;
        }

        public static IAssembleObject Factory
        {
            get
            {
                return Instance;
            }
        }

        public static IObjectAssemblyRegistry Services
        {
            get
            {
                return Instance;
            }
        }

        public static ConstructConfiguration Preconfigure()
        {
            var newConfig = new ConstructConfiguration();
            var lf = _localFactory.Value;
            lf.Push(newConfig);
            return newConfig;
        }

        public static ConstructConfiguration Preconfigure(ConstructConfiguration cached)
        {
            _localFactory.Value.Push(cached);
            return cached;
        }

        public static void ResolveBoundConfiguration()
        {
            _localFactory.Value.Pop();
        }

        public static void LoadFromAssembly(Assembly assembly)
        {
            LoadFromAssemblyInternal(assembly, Instance);
        }

        private static void LoadFromAssemblyInternal(Assembly assembly, InstanceAssembler container)
        {
            Type[] exportedTypes = assembly.GetExportedTypes();

            IEnumerable<Type> types =
                exportedTypes.Where(type => type.GetInterface(typeof(IObjectAssemblySpecifier).ToString()) != null);

            foreach (Type type in types)
            {
                try
                {
                    ((IObjectAssemblySpecifier)Activator.CreateInstance(type)).RegisterIn(container);
                }
                catch (Exception)
                {
                    
       
                }
            }
        }
    }
}