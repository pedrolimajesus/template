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
using log4net;

namespace AppComponents
{
    public class InMemoryCachedData<TKeyType, TDataType> : ICachedData<TKeyType, TDataType>
    {
        private ConcurrentDictionary<TKeyType, CacheValue<TDataType>> _cacheTable =
            new ConcurrentDictionary<TKeyType, CacheValue<TDataType>>();

        private DebugOnlyLogger _dblog;

        private TimeSpan _groomScheduleTimeOut = new TimeSpan(0, 15, 0);

        private ILog _log;
        private DateTime _nextGroomSchedule;

        public InMemoryCachedData()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);

            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);

            ExpireItems = config.Get(CachedDataLocalConfig.OptionalGroomExpiredData, false);
            RenewOnCacheHit = config.Get(CachedDataLocalConfig.OptionalCacheHitRenewsExpiration, false);
            DisposeOfData = config.Get(CachedDataLocalConfig.OptionalDisposeData, false);
            DefaultExpireTime =
                TimeSpan.FromSeconds(config.Get(CachedDataLocalConfig.OptionalDefaultExpirationTimeSeconds, 3600));
            MaximumCacheItemsCount = config.Get(CachedDataLocalConfig.OptionalMaximumCacheSize, 0);
            NotifyExpiredWithData = config.Get(CachedDataLocalConfig.OptionalNotifyExpiredWithData, true);

            _nextGroomSchedule = DateTime.UtcNow + _groomScheduleTimeOut;
        }

        #region ICachedData<TKeyType,TDataType> Members

        public int CachedItemsCount
        {
            get { return _cacheTable.Count; }
        }

        public bool ExpireItems { get; set; }

        public bool DisposeOfData { get; set; }

        public bool RenewOnCacheHit { get; set; }

        public TimeSpan DefaultExpireTime { get; set; }

        public int MaximumCacheItemsCount { get; set; }

        public void Clear()
        {
            MaybeDisposeAllData();
            _cacheTable.Clear();
        }

        public bool NotifyExpiredWithData { get; set; }

        public bool MaybeGetItem(TKeyType key, out TDataType value)
        {
            GroomAllExpiredItems();
            bool found = false;
            value = default(TDataType);

            CacheValue<TDataType> item;
            found = _cacheTable.TryGetValue(key, out item);

            if (found)
            {
                _dblog.InfoFormat("Cache hit for {0}", key);
                value = item.Value;
                if (RenewOnCacheHit)
                {
                    item.RenewExpiration();
                    _dblog.InfoFormat("Renewed expiration for {0} to {1}", key, item.ExpirationTime);
                }
            }

            return found;
        }

        public bool ContainsKey(TKeyType key)
        {
            GroomAllExpiredItems();

            return _cacheTable.ContainsKey(key);
        }

        public void RemoveItem(TKeyType key)
        {
            _log.InfoFormat("Removing {0} from cache", key);

            MaybeDisposeData(key);
            CacheValue<TDataType> _;
            _cacheTable.TryRemove(key, out _);
        }

        public void Add(TKeyType key, TDataType value, TimeSpan? expiration = null)
        {
            if (expiration == null)
                expiration = DefaultExpireTime;

            GroomAllExpiredItems();

            if (_cacheTable.ContainsKey(key))
            {
                _log.WarnFormat("Trying to add item {0} into cache, but it already exists. Removing first", key);
                CacheValue<TDataType> _;
                _cacheTable.TryRemove(key, out _);
            }

            if (MaximumCacheItemsCount != 0 && MaximumCacheItemsCount <= _cacheTable.Count)
            {
                if (ExpireItems)
                {
                    _log.Warn("Cache full, expiring oldest items");
                    while (MaximumCacheItemsCount <= _cacheTable.Count)
                        MaybeExpireOldestItem();
                }
                else
                {
                    string err =
                        string.Format("Cache limit of {0} exceeded, cannot add more items. Automatic grooming OFF.",
                                      _cacheTable.Count);
                    _log.ErrorFormat(err);
                    throw new CacheLimitException(err);
                }
            }

            var cv = new CacheValue<TDataType>
                         {
                             ExpirationLife = expiration.Value,
                             ExpirationTime = DateTime.UtcNow + expiration.Value,
                             Value = value
                         };

            _cacheTable.AddOrUpdate(key, _ => cv, (k, v) => cv);

            //_log.InfoFormat("Added item {0} to cache", key);
        }

        public event EventHandler<ExpiredEventArgs<TKeyType, TDataType>> DataExpired;

        public IEnumerable<TKeyType> GetCacheKeys()
        {
            return _cacheTable.Keys.ToArray();
        }

        #endregion

        private void DoNotifyDataExpired(TKeyType key, TDataType data, ExpirationReason reason)
        {
            EventHandler<ExpiredEventArgs<TKeyType, TDataType>> ev = DataExpired;
            if (null != ev)
            {
                if (!NotifyExpiredWithData)
                    data = default(TDataType);
                ev(this, new ExpiredEventArgs<TKeyType, TDataType>(key, data, reason));
            }
        }

        private void MaybeExpireOldestItem()
        {
            if (ExpireItems)
            {
                var oldestItemSearch = (from item in _cacheTable
                                        orderby item.Value.ExpirationTime descending
                                        select item.Key).ToArray();

                if (oldestItemSearch.Any())
                {
                    var oldestItem = oldestItemSearch.First();
                    _log.InfoFormat("Grooming oldest cache item {0}", oldestItem);
                    MaybeDisposeData(oldestItem);
                    CacheValue<TDataType> val;

                    if (_cacheTable.TryRemove(oldestItem, out val))
                        DoNotifyDataExpired(oldestItem, val.Value, ExpirationReason.RemoveOldestForSpace);
                }
            }
        }

        private void GroomAllExpiredItems(bool now = false)
        {
            bool timeToGroom = now ||
                               DateTime.UtcNow > _nextGroomSchedule;

            if (ExpireItems && timeToGroom)
            {
                var currentTime = DateTime.UtcNow;

                TKeyType[] expiredItems =
                    _cacheTable.Keys.Where(k => _cacheTable[k].ExpirationTime < currentTime).ToArray();
                _log.InfoFormat("Grooming all items expired since {0}, {1} items of {2}", currentTime,
                                expiredItems.Length, _cacheTable.Count);

                Array.ForEach(expiredItems, k =>
                                                {
                                                    MaybeDisposeData(k);
                                                    CacheValue<TDataType> val;
                                                    _dblog.InfoFormat("Grooming expired cache item {0}", k);
                                                    if (_cacheTable.TryRemove(k, out val))
                                                        DoNotifyDataExpired(k, val.Value,
                                                                            ExpirationReason.LifeTimeExceeded);
                                                });
                _nextGroomSchedule = DateTime.UtcNow + _groomScheduleTimeOut;
            }
        }

        private void MaybeDisposeData(TKeyType key)
        {
            if (DisposeOfData && !NotifyExpiredWithData)
            {
                var idisp = _cacheTable[key] as IDisposable;
                if (idisp != null)
                {
                    _dblog.InfoFormat("Disposing data for cached item {0}", key);
                    idisp.Dispose();
                }
            }
        }

        private void MaybeDisposeAllData()
        {
            if (DisposeOfData)
            {
                foreach (TKeyType k in _cacheTable.Keys)
                    MaybeDisposeData(k);
            }
        }

        #region Nested type: CacheValue

        internal class CacheValue<TDt>
        {
            public TDt Value { get; set; }
            public DateTime ExpirationTime { get; set; }
            public TimeSpan ExpirationLife { get; set; }

            public void RenewExpiration()
            {
                ExpirationTime = DateTime.UtcNow + ExpirationLife;
            }
        }

        #endregion
    }
}