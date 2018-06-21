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
using System.IO;
using log4net;

namespace AppComponents.Azure
{
    public class BlobFileDownload : IFileDownload
    {
        private const int _memoryHog = 1024*1024*2;
        private static ICachedData<string, Stream> _cache;
        private string _container = string.Empty;
        private DebugOnlyLogger _dblog;
        private ILog _log;

        static BlobFileDownload()
        {
            _cache = new InMemoryCachedData<string, Stream>();
            _cache.MaximumCacheItemsCount = 500;
            _cache.DefaultExpireTime = new TimeSpan(0, 1, 0);
            _cache.RenewOnCacheHit = true;
            _cache.DisposeOfData = true;
        }

        public BlobFileDownload(string container = null)
        {
            if (string.IsNullOrWhiteSpace(container))
            {
                IConfig config = Catalog.Factory.Resolve<IConfig>();
                _container = config.Get(CommonConfiguration.DefaultDownloadContainer, "downloads");
            }
            else
            {
                _container = container;
            }

            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);
        }

        #region IFileDownload Members

        public byte[] DownloadData(string file, int startRange, int length)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(file));
            Debug.Assert(startRange >= 0);
            Debug.Assert(length < _memoryHog);

            Stream data;
            if (!_cache.MaybeGetItem(file, out data))
            {
                _log.InfoFormat("Downloading {0} from blob storage and caching", file);

                var blobs = Client.FromConfig().ForBlobs();
                var c = blobs.GetContainerReference(_container);
                var b = c.GetBlobReference(file);
                using (MemoryStream ms = new MemoryStream())
                {
                    b.DownloadToStream(ms);
                    _cache.Add(file, ms);
                    data = ms;
                }
            }
            else
            {
                _dblog.InfoFormat("Cache hit for file download {0}", file);
            }

            if (startRange >= data.Length)
            {
                return null;
            }

            byte[] retval = new byte[length];
            data.Seek(startRange, SeekOrigin.Begin);
            int read = data.Read(retval, 0, length);

            if (read < length)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(retval, 0, read);
                    retval = ms.ToArray();
                }
            }

            return retval;
        }

        public void DeleteData(string file)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(file));

            _log.InfoFormat("Deleting downloadable file {0}", file);
            var blobs = Client.FromConfig().ForBlobs();
            var c = blobs.GetContainerReference(_container);
            var b = c.GetBlobReference(file);
            b.DeleteIfExists();
        }

        #endregion
    }
}