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
using System.Threading;

namespace AppComponents.Data
{
    public class ObjectPool<T> : IDisposable
        where T : class
    {
        private readonly Func<T> _factory;

        private readonly ConcurrentDictionary<int, ConcurrentQueue<T>> _objectPool =
            new ConcurrentDictionary<int, ConcurrentQueue<T>>();

        private int _version;

        public ObjectPool(Func<T> factory = null)
        {
            _factory = factory ?? Activator.CreateInstance<T>;
            _objectPool.TryAdd(0, new ConcurrentQueue<T>());
        }

        public int Count
        {
            get { return _objectPool.Sum(p => p.Value.Count); }
        }

        #region IDisposable Members

        public void Dispose()
        {
            GenVersion();
            CleanPools(_objectPool.Keys.ToArray());
        }

        #endregion

        public IDisposable Resolve(out T obj)
        {
            var cv = Thread.VolatileRead(ref _version);
            ConcurrentQueue<T> cp;
            _objectPool.TryGetValue(cv, out cp);

            T val = (cp != null && cp.TryDequeue(out val)) ? val : _factory();
            obj = val;

            return Disposable.Create(() =>
                                         {
                                             ConcurrentQueue<T> that;
                                             if (cv == Thread.VolatileRead(ref cv) &&
                                                 _objectPool.TryGetValue(cv, out that))
                                                 that.Enqueue(val);
                                             else
                                             {
                                                 if (val is IDisposable)
                                                     (val as IDisposable).Dispose();
                                             }
                                         });
        }

        public void Clear()
        {
            var current = GenVersion();

            var old = _objectPool.Keys.Where(v => v < current).ToArray();

            CleanPools(old);
        }

        private void CleanPools(IEnumerable<int> old)
        {
            foreach (var each in old)
            {
                ConcurrentQueue<T> val;
                if (_objectPool.TryRemove(each, out val) == false)
                    continue;

                T that;
                while (val.TryDequeue(out that))
                {
                    if (that is IDisposable)
                        (that as IDisposable).Dispose();
                }
            }
        }

        private int GenVersion()
        {
            var current = Interlocked.Increment(ref _version);
            _objectPool.TryAdd(current, new ConcurrentQueue<T>());
            return current;
        }
    }
}