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
using System.Linq;

namespace AppComponents
{
    public class InstanceCacheFactory : IInstanceCreationStrategy, IDisposable
    {
        private ConcurrentDictionary<string, CachedInstance> _cache = new ConcurrentDictionary<string, CachedInstance>();
        private TimeSpan _expirationDuration = TimeSpan.FromHours(1.0);

        private DateTime _lastGroom = DateTime.UtcNow;
        private object _syncLock = new object();

        #region IDisposable Members

        public void Dispose()
        {
            _cache.Clear();
        }

        #endregion

        #region IInstanceCreationStrategy Members

        public object ActivateInstance(IObjectAssemblySpecification registration)
        {
            string key = registration.Key;

            CachedInstance ci = null;
            if (_cache.TryGetValue(key, out ci))
            {
                lock (_syncLock) ci.LastCacheHit = DateTime.UtcNow;
            }
            else
            {
                ci = new CachedInstance {Instance = registration.CreateInstance(), LastCacheHit = DateTime.UtcNow};
                _cache.TryAdd(key, ci);
            }

            MaybeGroomCache();
            return ci.Instance;
        }

        public void FlushCache(IObjectAssemblySpecification registration)
        {
            CachedInstance _ = null;
            _cache.TryRemove(registration.Key, out _);
        }

        #endregion

        public InstanceCacheFactory ExpiresAfterNotAccessedFor(TimeSpan duration)
        {
            lock (_syncLock)
            {
                _expirationDuration = duration;
            }

            return this;
        }

        private void GroomCache()
        {
            DateTime oldestPermitted = DateTime.UtcNow - _expirationDuration;
            foreach (var item in _cache.AsEnumerable())
            {
                if (item.Value.LastCacheHit < oldestPermitted)
                {
                    CachedInstance _;
                    _cache.TryRemove(item.Key, out _);
                }
            }
        }

        private void MaybeGroomCache()
        {
            TimeSpan howLongSince;
            lock (_syncLock) howLongSince = DateTime.UtcNow - _lastGroom;
            if (howLongSince > TimeSpan.FromMinutes(10.0))
            {
                GroomCache();
                lock (_syncLock) _lastGroom = DateTime.UtcNow;
            }
        }

        #region Nested type: CachedInstance

        private class CachedInstance
        {
            public object Instance { get; set; }
            public DateTime LastCacheHit { get; set; }
        }

        #endregion
    }
}