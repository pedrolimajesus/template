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
using System.Net;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Newtonsoft.Json;
using log4net;
using smarx.WazStorageExtensions;

namespace AppComponents.Azure
{
    internal static class AzureEntityAccessTranslator
    {
        public static BlobContainerPublicAccessType Translate(EntityAccess access)
        {
            switch (access)
            {
                case EntityAccess.Private:
                    return BlobContainerPublicAccessType.Off;


                case EntityAccess.Public:
                    return BlobContainerPublicAccessType.Blob;


                case EntityAccess.ContainerPublic:
                    return BlobContainerPublicAccessType.Container;

                default:
                    return BlobContainerPublicAccessType.Off;
            }
        }
    }


    /// <summary>
    ///   Implements the <see
    ///    cref="IBlobContainer{T}
    ///                    
    ///                    
    ///                    
    ///                    
    ///                    
    ///                    
    ///                    <T>"/> interface using azure blob storage as a backing store.
    ///                      Uses the
    ///                      <see cref="BlobContainerLocalConfig" />
    ///                      preconfiguration.
    /// </summary>
    /// <typeparam name="T"> </typeparam>
    public class AzureBlobBlobContainer<T> : IBlobContainer<T>
    {
        private CloudStorageAccount _account;
        private BlobContainerPermissions _blobContainerPermissions;
        private CloudBlobClient _client;
        private CloudBlobContainer _container;
        private string _containerName;
        private string _contentType;


        private DebugOnlyLogger _dblog;
        private ILog _log;

        public AzureBlobBlobContainer()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);

            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _containerName = config.Get(BlobContainerLocalConfig.ContainerName, string.Empty);
            EntityAccess access = (EntityAccess) Enum.Parse(typeof (EntityAccess),
                                                            config.Get(BlobContainerLocalConfig.OptionalAccess,
                                                                       EntityAccess.Private.ToString()));
            _contentType = config.Get(BlobContainerLocalConfig.OptionalContentType, "application/json");

            _account = Client.FromConfig();

            if (String.IsNullOrEmpty(_containerName))
            {
                var type = typeof (T);
                if (!type.IsClass)
                {
                    throw new ArgumentNullException("You must specify a container for containers that use value types.");
                }
                _containerName = type.Name.ToLowerInvariant();
                if ((_containerName.EndsWith("blob") || _containerName.EndsWith("view")) && _containerName.Length > 4)
                {
                    _containerName = _containerName.Substring(0, _containerName.Length - 4);
                }
            }

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

        #region IBlobContainer<T> Members

        public void SetExpire(TimeSpan ts)
        {
            _container.SetExpiration(ts);
        }


        public void Save(string objId, T obj, TimeSpan? expire = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));
            Debug.Assert(null != obj);

            CloudBlob blob = _container.GetBlobReference(String.Concat(objId, ".json"));
            blob.Properties.ContentType = _contentType;
            var data = JsonConvert.SerializeObject(obj);

            _log.InfoFormat("Saving {0}: {1}", objId, data);

            blob.UploadText(data);
            if (expire.HasValue)
                blob.SetExpiration(expire.Value);
        }


        public void SaveAsync(string objId, T obj, TimeSpan? expire = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));
            Debug.Assert(null != obj);

            CloudBlob blob = _container.GetBlobReference(String.Concat(objId, ".json"));
            blob.Properties.ContentType = _contentType;
            string json = JsonConvert.SerializeObject(obj);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                _log.InfoFormat("Saving {0}: {1}", objId, json);

                blob.BeginUploadFromStream(stream, (ar) =>
                                                       {
                                                           var resultBlob = ar.AsyncState as CloudBlob;
                                                           resultBlob.EndUploadFromStream(ar);
                                                           if (expire.HasValue)
                                                               resultBlob.SetExpiration(expire.Value);
                                                       }, blob);
            }
        }

        public T Get(string objId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));

            CloudBlob blob = _container.GetBlobReference(String.Concat(objId, ".json"));
            try
            {
                var json = blob.DownloadText();
                _dblog.InfoFormat("Retrieved {0}: {1}", objId, json);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (StorageClientException)
            {
                return default(T);
            }
        }

        public IEnumerable<T> GetAll()
        {
            IEnumerable<IListBlobItem> blobItems = _container.ListBlobs();
            _log.WarnFormat("Getting ALL entities from {0}, {1} items", _container.Name, blobItems.Count());
            return blobItems.Select(blobItem => Get(blobItem.Uri.ToString())).ToList();
        }

        public Uri GetUri(string objId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));
            return new Uri(String.Concat(objId, ".json"));
        }

        public void Delete(string objId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));
            _log.InfoFormat("Deleting item {0} from {1}", objId, _container.Name);
            CloudBlob blob = _container.GetBlobReference(String.Concat(objId, ".json"));
            blob.DeleteIfExists();
        }

        public void DeleteContainer()
        {
            try
            {
                _log.WarnFormat("Deleting all entities from {0}", _container.Name);
                if (_container.Exists())
                    _container.Delete();
            }
            catch (StorageClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return;
                }

                throw;
            }
        }


        public bool Exists(string objId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));

            CloudBlob blob = _container.GetBlobReference(String.Concat(objId, ".json"));
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
            Debug.Assert(!string.IsNullOrWhiteSpace(objId));

            CloudBlob blob = _container.GetBlobReference(String.Concat(objId, ".json"));
            var json = blob.DownloadText();
            _dblog.InfoFormat("Retrieved {0}: {1}", objId, json);
            return json;
        }
    }
}