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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using AppComponents.Extensions.EnumerableEx;
using log4net;

namespace AppComponents.Data
{
    public class BlobDataSpooler<TData> : IDataSpooler<TData>
        where TData : class
    {
        public static readonly string TrackingContainer = "spooltracking";
        private readonly DebugOnlyLogger _dblog;


        private TimeSpan? _lifeTime = TimeSpan.FromMinutes(10.0);
        private readonly ILog _log;


        private IBlobContainer<IEnumerable<TData>> _pageData;
        private int _pageSize = 200;
        private string _spoolId;
        private IBlobContainer<SpoolTracking> _spoolTrackingData;


        private SpoolTracking _tracking = new SpoolTracking {PageBookmark = null, LastPageNumber = 1, Completed = false};

        public BlobDataSpooler()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);

            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var spoolId = config[DataSpoolerLocalConfig.SpoolId];
            var pageSize = config.Get<int>(DataSpoolerLocalConfig.PageSize);
            var lifeTimeMinutes = config.Get(DataSpoolerLocalConfig.OptionalLifeTimeMinutes, -1);
            TimeSpan? lifeTime = null;
            if (lifeTimeMinutes != -1)
                lifeTime = TimeSpan.FromMinutes(lifeTimeMinutes);

            Initialize(spoolId, pageSize, lifeTime);
        }

        public bool IsInitialized { get; private set; }

        public bool IsStarted { get; private set; }

        #region IDataSpooler<X> Members

        public SpoolTracking BeginSpooling()
        {
            Debug.Assert(IsInitialized);
            IsStarted = true;
            _tracking = _spoolTrackingData.Get(_spoolId) ?? _tracking;
            return _tracking;
        }

        public void EndSpooling(IPageBookmark continuation)
        {
            Debug.Assert(IsInitialized);
            Debug.Assert(IsStarted);

            _tracking.PageBookmark = continuation;
            _tracking.Completed = true;
            _spoolTrackingData.Save(_spoolId, _tracking, _lifeTime);
            _pageData.Save("complete", new TData[] {}, _lifeTime);
        }

        public void SpoolBatch(IEnumerable<TData> data)
        {
            Debug.Assert(IsInitialized);
            Debug.Assert(IsStarted);
            Debug.Assert(null != data);

            if (data.Any())
            {
                var pages = data.Partition(_pageSize);
                foreach (var page in pages)
                {
                    var pageNumber = _tracking.LastPageNumber.ToString("d19", CultureInfo.InvariantCulture);
                    _log.InfoFormat("Spooling page {0} of {1}", pageNumber, _spoolId);
                    string pageName = pageNumber + "page";
                    _pageData.SaveAsync(pageName, page, _lifeTime);
                    _tracking.LastPageNumber++;
                }
            }
            else
            {
                _dblog.WarnFormat("Tried to spool an empty batch of data on {0}", _spoolId);
            }
        }

        #endregion

        private void Initialize(string spoolId, int pageSize, TimeSpan? lifeTime = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(spoolId));
            Debug.Assert(pageSize > 0);

            IsInitialized = true;

            _spoolId = "spooldata" + spoolId;
            _pageSize = pageSize;

            if (lifeTime.HasValue)
                _lifeTime = lifeTime;

            _pageData = Catalog.Preconfigure()
                .Add(BlobContainerLocalConfig.ContainerName, _spoolId)
                .Add(BlobContainerLocalConfig.OptionalAccess, EntityAccess.ContainerPublic.ToString())
                .ConfiguredResolve<IBlobContainer<IEnumerable<TData>>>();

            if(_lifeTime.HasValue)
                _pageData.SetExpire(_lifeTime.Value);

            _spoolTrackingData = Catalog.Preconfigure()
                .Add(BlobContainerLocalConfig.ContainerName, TrackingContainer)
                .Add(BlobContainerLocalConfig.OptionalAccess, EntityAccess.Private.ToString())
                .ConfiguredResolve<IBlobContainer<SpoolTracking>>();

            _spoolTrackingData.SetExpire(_lifeTime.Value);
        }
    }
}