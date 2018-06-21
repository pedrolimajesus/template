using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AppComponents.Extensions.EnumerableEx;
using log4net;

namespace AppComponents.Files
{
    public class BlobContainerImageStorage: IImageStorage
    {
        private ICachedData<string, byte[]> _cache;
        private string _containerName;
        private DebugOnlyLogger _dblog;
        private ILog _log;
        private IFilesContainer _filesContainer; 

        public BlobContainerImageStorage()
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

            _filesContainer = Catalog.Preconfigure()
                .Add(BlobContainerLocalConfig.ContainerName, _containerName)
                .ConfiguredResolve<IFilesContainer>();
        }





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
                return _filesContainer.Get(key);
            }

            return retval;
        }

        public void UploadImage(string key, byte[] image)
        {
            Debug.Assert(image.EmptyIfNull().Any());


            _log.InfoFormat("Uploading image {0}", key);

            _filesContainer.Save(key, image);

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
    }
}
