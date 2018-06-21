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
using System.Web;
using System.Web.Caching;

namespace AppComponents.InstanceFactories
{
    public class WebCacheInstanceFactory : IInstanceCreationStrategy, IDisposable
    {
        private readonly object _syncRoot = new object();
        private CacheItemPriority _cachePriority = CacheItemPriority.Default;
        private CacheDependency _dependencies;
        private CachedItemExpirationBehavior _expirationBehavior = CachedItemExpirationBehavior.NeverExpires;
        private TimeSpan _expirationDuration = Cache.NoSlidingExpiration;
        private DateTime _expirationTime = Cache.NoAbsoluteExpiration;
        private CacheItemRemovedCallback _onRemoveCallback;

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IInstanceCreationStrategy Members

        public object ActivateInstance(IObjectAssemblySpecification registration)
        {
            Cache cache = HttpRuntime.Cache;

            string key = registration.Key;
            object instance = cache[key];
            if (instance == null)
            {
                lock (_syncRoot)
                {
                    instance = cache[key];
                    if (instance == null)
                    {
                        instance = registration.CreateInstance();

                        if (_expirationTime == Cache.NoAbsoluteExpiration &&
                            _expirationDuration == Cache.NoSlidingExpiration)
                            _expirationBehavior = CachedItemExpirationBehavior.NeverExpires;

                        switch (_expirationBehavior)
                        {
                            case CachedItemExpirationBehavior.NeverExpires:
                                cache.Insert(key, instance, _dependencies, Cache.NoAbsoluteExpiration,
                                             Cache.NoSlidingExpiration, _cachePriority, _onRemoveCallback);
                                break;
                            case CachedItemExpirationBehavior.AtScheduledDate:
                                cache.Insert(key, instance, _dependencies, _expirationTime,
                                             Cache.NoSlidingExpiration, _cachePriority, _onRemoveCallback);
                                break;
                            case CachedItemExpirationBehavior.AfterTimeSpan:
                                cache.Insert(key, instance, _dependencies, DateTime.UtcNow.Add(_expirationDuration),
                                             Cache.NoSlidingExpiration, _cachePriority, _onRemoveCallback);
                                break;
                            case CachedItemExpirationBehavior.AfterNotUsedInTimeSpan:
                                cache.Insert(key, instance, _dependencies, Cache.NoAbsoluteExpiration,
                                             _expirationDuration, _cachePriority, _onRemoveCallback);
                                break;
                        }
                    }
                }
            }

            return instance;
        }

        public void FlushCache(IObjectAssemblySpecification registration)
        {
            Cache cache = HttpRuntime.Cache;
            cache.Remove(registration.Key);
        }

        #endregion

        public WebCacheInstanceFactory IsDependentOn(CacheDependency dependencies)
        {
            _dependencies = dependencies;
            return this;
        }

        public WebCacheInstanceFactory ExpiresOn(DateTime absoluteExpiration)
        {
            if (absoluteExpiration != Cache.NoAbsoluteExpiration)
            {
                _expirationDuration = Cache.NoSlidingExpiration;
                _expirationBehavior = CachedItemExpirationBehavior.AtScheduledDate;
            }

            _expirationTime = absoluteExpiration;
            return this;
        }

        public WebCacheInstanceFactory ExpiresAfterNotAccessedFor(TimeSpan duration)
        {
            if (duration != Cache.NoSlidingExpiration)
            {
                _expirationTime = Cache.NoAbsoluteExpiration;
                _expirationBehavior = CachedItemExpirationBehavior.AfterNotUsedInTimeSpan;
            }

            _expirationDuration = duration;
            return this;
        }

        public WebCacheInstanceFactory ExpiresAfter(TimeSpan duration)
        {
            if (duration != Cache.NoSlidingExpiration)
            {
                _expirationTime = Cache.NoAbsoluteExpiration;
                _expirationBehavior = CachedItemExpirationBehavior.AfterTimeSpan;
            }

            _expirationDuration = duration;
            return this;
        }

        public WebCacheInstanceFactory WithPriority(CacheItemPriority priority)
        {
            _cachePriority = priority;
            return this;
        }

        public WebCacheInstanceFactory CallbackOnRemoval(CacheItemRemovedCallback onRemoveCallback)
        {
            _onRemoveCallback = onRemoveCallback;
            return this;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                if (_dependencies != null)
                {
                    _dependencies.Dispose();
                    _dependencies = null;
                }
        }

        ~WebCacheInstanceFactory()
        {
            Dispose(false);
        }

        #region Nested type: CachedItemExpirationBehavior

        private enum CachedItemExpirationBehavior
        {
            NeverExpires,
            AtScheduledDate,
            AfterTimeSpan,
            AfterNotUsedInTimeSpan
        };

        #endregion
    }
}