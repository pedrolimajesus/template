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
using System.Diagnostics.Contracts;

namespace AppComponents
{
    public enum ExpirationReason
    {
        LifeTimeExceeded,
        RemoveOldestForSpace,
    };

    public class ExpiredEventArgs<KeyType, DataType> : EventArgs
    {
        private DataType _data;
        private KeyType _key;
        private ExpirationReason _reason;

        public ExpiredEventArgs(KeyType key, DataType data, ExpirationReason reason)
        {
            _key = key;
            _data = data;
            _reason = reason;
        }

        public KeyType Key
        {
            get { return _key; }
        }

        public DataType Data
        {
            get { return _data; }
        }

        public ExpirationReason Reason
        {
            get { return _reason; }
        }
    }


    public enum CachedDataLocalConfig
    {
        OptionalGroomExpiredData,
        OptionalCacheHitRenewsExpiration,
        OptionalDisposeData,
        OptionalDefaultExpirationTimeSeconds,
        OptionalMaximumCacheSize,
        OptionalNotifyExpiredWithData
    }

    [RequiresConfiguration]
    [ContractClass(typeof (ICachedDataContract<,>))]
    public interface ICachedData<KT, DT>
    {
        int CachedItemsCount { get; }
        TimeSpan DefaultExpireTime { get; set; }
        bool DisposeOfData { get; set; }
        bool ExpireItems { get; set; }
        int MaximumCacheItemsCount { get; set; }
        bool NotifyExpiredWithData { get; set; }
        bool RenewOnCacheHit { get; set; }

        IEnumerable<KT> GetCacheKeys();

        bool MaybeGetItem(KT key, out DT value);
        bool ContainsKey(KT key);
        void RemoveItem(KT key);

        void Add(KT key, DT value, TimeSpan? expiration = null);
        void Clear();

        event EventHandler<ExpiredEventArgs<KT, DT>> DataExpired;
    }

    public class CacheLimitException : Exception
    {
        public CacheLimitException()
        {
        }

        public CacheLimitException(string msg)
            : base(msg)
        {
        }
    }

    [ContractClassFor(typeof (ICachedData<,>))]
    internal abstract class ICachedDataContract<KeyType, DataType> : ICachedData<KeyType, DataType>
    {
        #region ICachedData<KeyType,DataType> Members

        public int CachedItemsCount
        {
            get { return default(int); }
        }

        public bool ExpireItems
        {
            get { return default(bool); }
            set { }
        }

        public bool RenewOnCacheHit
        {
            get { return default(bool); }
            set { }
        }

        public bool DisposeOfData
        {
            get { return default(bool); }
            set { }
        }

        public TimeSpan DefaultExpireTime
        {
            get { return default(TimeSpan); }
            set { }
        }

        public int MaximumCacheItemsCount
        {
            get { return default(int); }
            set { Contract.Requires(value > 0); }
        }

        public bool NotifyExpiredWithData
        {
            get { return default(bool); }
            set { }
        }

        public void Clear()
        {
        }

        public bool MaybeGetItem(KeyType key, out DataType value)
        {
            Contract.Requires(null != key);
            value = default(DataType);
            return default(bool);
        }

        public bool ContainsKey(KeyType key)
        {
            Contract.Requires(null != key);

            return default(bool);
        }

        public void RemoveItem(KeyType key)
        {
            Contract.Requires(key != null);
        }

        public void Add(KeyType key, DataType value, TimeSpan? expiration = null)
        {
            Contract.Requires(key != null);
        }

        public IEnumerable<KeyType> GetCacheKeys()
        {
            return default(IEnumerable<KeyType>);
        }

        public event EventHandler<ExpiredEventArgs<KeyType, DataType>> DataExpired;

        #endregion
    }
}