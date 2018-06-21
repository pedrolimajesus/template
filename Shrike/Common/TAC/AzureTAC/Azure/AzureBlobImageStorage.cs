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
using System.Diagnostics;
using System.Linq;
using AppComponents.Extensions.EnumerableEx;
using Microsoft.WindowsAzure.StorageClient;
using log4net;
using smarx.WazStorageExtensions;

namespace AppComponents.Azure
{
    /// <summary>
    ///   Implements the <see cref="IImageStorage" /> interface using Azure blob storage as a backing store
    /// </summary>
    public class AzureBlobImageStorage : IImageStorage
    {
        private CloudBlobClient _bc;
        private ICachedData<string, byte[]> _cache;
        private string _containerName;
        private DebugOnlyLogger _dblog;
        private ILog _log;


        public AzureBlobImageStorage()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);

            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var maxCacheSize = config.Get<int>(ImageStorageLocalConfig.CacheSize);
            var hitRenewsExpiration = config.Get<bool>(ImageStorageLocalConfig.CacheHitRenewsExpiration);
            var minutes = config.Get(ImageStorageLocalConfig.OptionalExpirationTimeMinutes, 60);
            var expirationLife = TimeSpan.FromMinutes(minutes);
            _containerName = config[ImageStorageLocalConfig.ContainerName];

            _cache = Catalog.Preconfigure()
                .Add(CachedDataLocalConfig.OptionalCacheHitRenewsExpiration, hitRenewsExpiration)
                .Add(CachedDataLocalConfig.OptionalDefaultExpirationTimeSeconds, expirationLife)
                .Add(CachedDataLocalConfig.OptionalGroomExpiredData, true)
                .Add(CachedDataLocalConfig.OptionalMaximumCacheSize, maxCacheSize)
                .ConfiguredResolve<ICachedData<string, byte[]>>(DurabilityFactoryContexts.Volatile);

            _bc = Client.FromConfig().ForBlobs();
        }

        #region IImageStorage Members

        public byte[] RetrieveKey(string key, bool fromCache = true)
        {
            byte[] retval = null;

            bool found = false;
            if (fromCache)
            {
                _dblog.InfoFormat("Found image {0} in cache", key);
                lock (_cache)
                {
                    found = _cache.MaybeGetItem(key, out retval);
                }
            }

            if (!found)
            {
                _log.InfoFormat("Retrieving image {0} from blob storage into cache", key);
                return RetrieveFromBlobStorage(key);
            }

            return retval;
        }

        public void UploadImage(string key, byte[] image)
        {
            Debug.Assert(image.EmptyIfNull().Any());


            _log.InfoFormat("Uploading image {0}", key);

            var container = _bc.GetContainerReference(_containerName);
            container.CreateIfNotExist();

            string blobUri = key;
            var blob = container.GetBlobReference(blobUri);
            blob.UploadByteArray(image);

            lock (_cache)
            {
                if (_cache.ContainsKey(key))
                {
                    _dblog.InfoFormat("Upload replaces image in cache for {0}", key);
                    _cache.RemoveItem(key);
                }

                _cache.Add(key, image);
            }
        }

        #endregion

        public byte[] RetrieveFromBlobStorage(string key)
        {
            var container = _bc.GetContainerReference(_containerName);

            container.CreateIfNotExist();


            string blobUri = key;
            var blob = container.GetBlobReference(blobUri);
            if (!blob.Exists())
            {
                var es = string.Format("Image {0} cannot be found in blob storage", key);
                _log.Error(es);
                IApplicationAlert on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.Unknown, es);
                return null;
            }


            byte[] bytes = blob.DownloadByteArray();

            lock (_cache)
            {
                if (!_cache.ContainsKey(key))
                {
                    _dblog.InfoFormat("Image {0} retrieved from blob storage, adding to cache", key);
                    _cache.Add(key, bytes);
                }
            }
            return bytes;
        }
    }
}