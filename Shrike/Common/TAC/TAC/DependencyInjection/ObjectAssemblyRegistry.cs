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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AppComponents
{
    internal class ObjectAssemblyRegistry : IDisposable
    {
        private const int InitialSize = 512;

        private readonly IDictionary<IObjectAssemblerRegistrationKey, ObjectAssemblySpecification> typeRegistrations =
            new ConcurrentDictionary<IObjectAssemblerRegistrationKey, ObjectAssemblySpecification>(
                Environment.ProcessorCount*2,
                InitialSize);

        private bool _isDisposed;

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public void Add(ObjectAssemblySpecification reg)
        {
            IObjectAssemblerRegistrationKey key = MakeKey(reg.Name, reg.ResolvesTo);
            typeRegistrations[key] = reg;
        }

        public ObjectAssemblySpecification Get(string name, Type type)
        {
            IObjectAssemblerRegistrationKey key = MakeKey(name, type);

            //var hc00 = key.GetHashCode();
            //var type00 = key.GetInstanceType();

            //foreach (var objectAssemblySpecification in typeRegistrations)
            //{
            //    var hc = objectAssemblySpecification.Key.GetHashCode();
            //    var type1 = objectAssemblySpecification.Key.GetInstanceType();
            //}

            return typeRegistrations[key];
        }

        public IEnumerable<ObjectAssemblySpecification> GetDerived(string name, Type type)
        {
            var regs = typeRegistrations.Values
                .Where(r => String.Compare(r.Name, name, true) == 0 &&
                            type.IsAssignableFrom(r.ResolvesTo));
            return regs;
        }

        public IEnumerable<ObjectAssemblySpecification> GetDerived(Type type)
        {
            var regs = typeRegistrations.Values
                .Where(r => type.IsAssignableFrom(r.ResolvesTo));
            return regs;
        }

        public bool ContainsKey(string name, Type type)
        {
            IObjectAssemblerRegistrationKey key = MakeKey(name, type);
            return typeRegistrations.Keys.Contains(key);
        }

        public IEnumerable<ObjectAssemblySpecification> All(Type type)
        {
            return typeRegistrations.Values.Where(reg => reg.ResolvesTo == type);
        }

        public IEnumerable<ObjectAssemblySpecification> All()
        {
            return typeRegistrations.Values;
        }

        public void Remove(IObjectAssemblySpecification ireg)
        {
            IObjectAssemblerRegistrationKey key = MakeKey(ireg.Name, ireg.ResolvesTo);
            typeRegistrations.Remove(key);
            ireg.FlushCache();
        }

        private static IObjectAssemblerRegistrationKey MakeKey(string name, Type type)
        {
            return (name == null
                        ? new NamelessObjectAssemblerRegistrationKey(type)
                        : (IObjectAssemblerRegistrationKey) new NamedObjectAssemblerRegistrationKey(name, type));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    foreach (ObjectAssemblySpecification reg in typeRegistrations.Values)
                    {
                        var instance = reg._instance as IDisposable;
                        if (instance != null)
                        {
                            instance.Dispose();
                            reg._instance = null;
                        }
                        reg.FlushCache();
                    }
                }
            }
            _isDisposed = true;
        }

        ~ObjectAssemblyRegistry()
        {
            Dispose(false);
        }
    }
}