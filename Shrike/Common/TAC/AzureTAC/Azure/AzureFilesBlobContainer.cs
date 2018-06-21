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
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Newtonsoft.Json;
using log4net;
using smarx.WazStorageExtensions;

namespace AppComponents.Azure
{
    /// <summary>
    ///   Implements the <see cref="IFilesContainer" /> interface using azure blob storage as the backing store. Uses the <see
    ///    cref="BlobContainerLocalConfig" /> preconfiguration.
    /// </summary>
    public class AzureFilesBlobContainer : IFilesContainer
    {
        private CloudStorageAccount _account;
        private BlobContainerPermissions _blobContainerPermissions;
        private CloudBlobClient _client;
        private CloudBlobContainer _container;
        private string _containerName;
        private string _contentType;


        private DebugOnlyLogger _dblog;
        private ILog _log;


        public AzureFilesBlobContainer()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);

            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _containerName = config[BlobContainerLocalConfig.ContainerName];
            EntityAccess access = (EntityAccess) Enum.Parse(typeof (EntityAccess),
                                                            config.Get(BlobContainerLocalConfig.OptionalAccess,
                                                                       EntityAccess.Private.ToString()));
            _contentType = config.Get(BlobContainerLocalConfig.OptionalContentType, "application/raw");

            _account = Client.FromConfig();
            _client = _account.CreateCloudBlobClient();

            _client.RetryPolicy = RetryPolicies.Retry(3, TimeSpan.FromSeconds(5));

            var blobContainerPermissions = new BlobContainerPermissions
                                               {PublicAccess = AzureEntityAccessTranslator.Translate(access)};
            _blobContainerPermissions = blobContainerPermissions;
            _container = _client.GetContainerReference(_containerName.ToLowerInvariant());

            if (_container.CreateIfNotExist())
            {
                _container.SetPermissions(_blobContainerPermissions);
            }
        }

        #region IFilesContainer Members

        public void SetExpire(TimeSpan ts)
        {
            _container.SetExpiration(ts);
        }

        public void Save(string objId, byte[] obj, TimeSpan? expire = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));
            Debug.Assert(null != obj);

            CloudBlob blob = _container.GetBlobReference(objId);
            _log.InfoFormat("Saving file {0} to {1}", objId, _container.Name);
            blob.Properties.ContentType = _contentType;
            blob.UploadByteArray(obj);
            if (expire.HasValue)
                blob.SetExpiration(expire.Value);
        }

        public void SaveAsync(string objId, byte[] obj, TimeSpan? expire = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));
            Debug.Assert(null != obj);

            CloudBlob blob = _container.GetBlobReference(objId);
            _log.InfoFormat("Saving file {0} to {1}", objId, _container.Name);

            blob.Properties.ContentType = _contentType;
            using (MemoryStream stream = new MemoryStream(obj))
            {
                blob.BeginUploadFromStream(stream, (ar) =>
                                                       {
                                                           var resultBlob = ar.AsyncState as CloudBlob;
                                                           resultBlob.EndUploadFromStream(ar);
                                                           if (expire.HasValue)
                                                               resultBlob.SetExpiration(expire.Value);
                                                       }, blob);
            }
        }

        public byte[] Get(string objId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));

            CloudBlob blob = _container.GetBlobReference(objId);
            try
            {
                return blob.DownloadByteArray();
            }
            catch (StorageClientException)
            {
                return null;
            }
        }

        public IEnumerable<byte[]> GetAll()
        {
            IEnumerable<IListBlobItem> blobItems = _container.ListBlobs();

            var data =
                blobItems.Select(blobItem => ((IBlobContainer<byte[]>) this).Get(blobItem.Uri.ToString())).ToList();
            _log.WarnFormat("Getting ALL files from {0}, {1} items, {2} bytes", _container.Name, data.Count(),
                            data.Sum(d => d.Length));
            return data;
        }

        public Uri GetUri(string objId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));

            CloudBlob blob = _container.GetBlobReference(objId);
            return blob.Uri;
        }

        public void Delete(string objId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));
            _log.InfoFormat("Deleting item {0} from {1}", objId, _container.Name);

            CloudBlob blob = _container.GetBlobReference(objId);
            blob.DeleteIfExists();
        }

        public void DeleteContainer()
        {
            _log.WarnFormat("Deleting all files from {0}", _container.Name);
            _container.Delete();
        }


        public bool Exists(string objId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));

            CloudBlob blob = _container.GetBlobReference(objId);
            return blob.Exists();
        }

        public IEnumerable<string> GetAllIds()
        {
            IEnumerable<IListBlobItem> blobItems = _container.ListBlobs();
            return blobItems.Select(bi => Path.GetFileNameWithoutExtension(bi.Uri.LocalPath));
        }

        #endregion

        public string GetAsJson(string objId)
        {
            return JsonConvert.SerializeObject(Get(objId));
        }

        public void SaveStream(string objId, Stream data, TimeSpan? expiration = null)
        {
            throw new NotImplementedException();
        }

        public void ReadRange(string objId, long? from, long? to, Stream outStream)
        {
            throw new NotImplementedException();
        }

        public Stream ReadStream(string objId)
        {
            throw new NotImplementedException();
        }
    }
}