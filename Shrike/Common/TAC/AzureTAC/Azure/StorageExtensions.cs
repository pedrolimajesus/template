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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using log4net;
using smarx.WazStorageExtensions;

namespace AppComponents.Azure
{
    /// <summary>
    ///   Helper functions for cleaning up storage messes.
    /// </summary>
    public static class AzureStorageAssistant
    {
        public static readonly string BlobMetaPropertyExpired = "Expired";

        /// <summary>
        ///   groom out blobs that are too old
        /// </summary>
        /// <param name="containerName"> </param>
        /// <param name="old"> </param>
        /// <param name="ct"> </param>
        /// <param name="removeEmptyContainer"> </param>
        public static void GroomOldBlobsFrom(string containerName, TimeSpan old, CancellationToken? ct = null,
                                             bool removeEmptyContainer = false)
        {
            ILog log = ClassLogger.Create(typeof (AzureStorageAssistant));
            DebugOnlyLogger dblog = DebugOnlyLogger.Create(log);

            try
            {
                log.InfoFormat("Grooming blobs from {0} older than {1}", containerName, old.ToString());

                var account =
                    CloudStorageAccount.FromConfigurationSetting(CommonConfiguration.DefaultStorageConnection.ToString());
                account.Ensure(containers: new[] {containerName});

                var bc = account.CreateCloudBlobClient();
                var container = bc.GetContainerReference(containerName);


                BlobRequestOptions blobQuery = new BlobRequestOptions();
                blobQuery.BlobListingDetails = BlobListingDetails.None;
                blobQuery.UseFlatBlobListing = true;

                blobQuery.AccessCondition = AccessCondition.IfNotModifiedSince(DateTime.UtcNow.AddDays(-1*old.Days));

                if (ct.HasValue) ct.Value.ThrowIfCancellationRequested();


                IEnumerable<IListBlobItem> blobs;
                blobs = container.ListBlobs(blobQuery).ToArray();


                foreach (IListBlobItem blob in blobs)
                {
                    if (ct.HasValue) ct.Value.ThrowIfCancellationRequested();

                    CloudBlob cloudBlob;

                    cloudBlob = container.GetBlobReference(blob.Uri.ToString());

                    dblog.InfoFormat("Grooming blob {0}", cloudBlob.Uri);
                    cloudBlob.DeleteIfExists(blobQuery);
                }

                if (removeEmptyContainer)
                {
                    if (!container.ListBlobs().Any())
                        container.Delete();
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex.Message);
                log.Warn(ex.ToString());
                throw;
            }
        }

        /// <summary>
        ///   groom using the expired metadata attribute
        /// </summary>
        /// <param name="containerName"> </param>
        /// <param name="ct"> </param>
        /// <param name="removeEmptyContainer"> </param>
        public static void CleanExpiredBlobsFrom(string containerName, CancellationToken? ct = null,
                                                 bool removeEmptyContainer = false)
        {
            ILog log = ClassLogger.Create(typeof (AzureStorageAssistant));
            DebugOnlyLogger dblog = DebugOnlyLogger.Create(log);

            try
            {
                var account =
                    CloudStorageAccount.FromConfigurationSetting(CommonConfiguration.DefaultStorageConnection.ToString());
                account.Ensure(containers: new[] {containerName});

                var bc = account.CreateCloudBlobClient();
                var container = bc.GetContainerReference(containerName);


                BlobRequestOptions blobQuery = new BlobRequestOptions();
                blobQuery.BlobListingDetails = BlobListingDetails.Metadata;
                blobQuery.UseFlatBlobListing = true;

                if (ct.HasValue) ct.Value.ThrowIfCancellationRequested();


                IEnumerable<IListBlobItem> blobs;
                blobs = container.ListBlobs(blobQuery).ToArray();


                foreach (IListBlobItem blob in blobs)
                {
                    if (ct.HasValue) ct.Value.ThrowIfCancellationRequested();

                    CloudBlob cloudBlob;

                    cloudBlob = container.GetBlobReference(blob.Uri.ToString());

                    var md = cloudBlob.Metadata[BlobMetaPropertyExpired];
                    if (!string.IsNullOrWhiteSpace(md))
                    {
                        DateTime expirationDate = DateTime.Parse(md, CultureInfo.InvariantCulture);
                        if (DateTime.UtcNow > expirationDate)
                        {
                            dblog.InfoFormat("Grooming blob {0}", cloudBlob.Uri);
                            cloudBlob.DeleteIfExists(blobQuery);
                        }
                    }
                }

                if (removeEmptyContainer)
                {
                    if (!container.ListBlobs().Any())
                        container.Delete();
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex.Message);
                log.Warn(ex.ToString());
                throw;
            }
        }

        /// <summary>
        ///   set the expiration metadata attribute on a blob
        /// </summary>
        /// <param name="that"> </param>
        /// <param name="age"> </param>
        public static void SetExpiration(this CloudBlob that, TimeSpan age)
        {
            that.FetchAttributes();
            that.Metadata.Remove(BlobMetaPropertyExpired);
            DateTime expiration = DateTime.UtcNow + age;
            that.Metadata.Add(BlobMetaPropertyExpired, expiration.ToString(CultureInfo.InvariantCulture));
            that.SetMetadata();
        }

        /// <summary>
        ///   set the expiration metadata attribute on a container
        /// </summary>
        /// <param name="that"> </param>
        /// <param name="age"> </param>
        public static void SetExpiration(this CloudBlobContainer that, TimeSpan age)
        {
            that.FetchAttributes();
            that.Metadata.Remove(BlobMetaPropertyExpired);
            DateTime expiration = DateTime.UtcNow + age;
            that.Metadata.Add(BlobMetaPropertyExpired, expiration.ToString(CultureInfo.InvariantCulture));
            that.SetMetadata();
        }

        /// <summary>
        ///   using the expiration metadata attribute, delete the container of all contents are expired.
        /// </summary>
        /// <param name="containerName"> </param>
        /// <param name="ct"> </param>
        public static void GroomContainerIfAllExpired(string containerName, CancellationToken? ct = null)
        {
            ILog log = ClassLogger.Create(typeof (AzureStorageAssistant));
            DebugOnlyLogger dblog = DebugOnlyLogger.Create(log);

            try
            {
                var account =
                    CloudStorageAccount.FromConfigurationSetting(CommonConfiguration.DefaultStorageConnection.ToString());
                account.Ensure(containers: new[] {containerName});

                var bc = account.CreateCloudBlobClient();
                var container = bc.GetContainerReference(containerName);


                BlobRequestOptions blobQuery = new BlobRequestOptions();
                blobQuery.BlobListingDetails = BlobListingDetails.Metadata;
                blobQuery.UseFlatBlobListing = true;


                if (ct.HasValue) ct.Value.ThrowIfCancellationRequested();


                ResultSegment<IListBlobItem> blobs;
                blobs = container.ListBlobsSegmented(blobQuery);


                bool more = false;
                long freshContent = 0;

                do
                {
                    Parallel.ForEach(blobs.Results,
                                     blob =>
                                         {
                                             if (Interlocked.Read(ref freshContent) > 0)
                                                 return;

                                             if (ct.HasValue) ct.Value.ThrowIfCancellationRequested();


                                             try
                                             {
                                                 CloudBlob cloudBlob;

                                                 cloudBlob = container.GetBlobReference(blob.Uri.ToString());
                                                 cloudBlob.FetchAttributes();
                                                 var md = cloudBlob.Metadata[BlobMetaPropertyExpired];

                                                 if (Interlocked.Read(ref freshContent) > 0)
                                                     return;

                                                 if (!string.IsNullOrWhiteSpace(md))
                                                 {
                                                     DateTime expirationDate = DateTime.Parse(md,
                                                                                              CultureInfo.
                                                                                                  InvariantCulture);
                                                     if (DateTime.UtcNow < expirationDate)
                                                     {
                                                         Interlocked.Increment(ref freshContent);

                                                         return;
                                                     }
                                                 }
                                             }
                                             catch
                                             {
                                                 Interlocked.Increment(ref freshContent);
                                                 // make no assumptions at this point
                                             }
                                         });


                    more = blobs.HasMoreResults;
                    if (more)
                        blobs = blobs.GetNext();
                } while (more && freshContent == 0);

                if (freshContent == 0)
                {
                    container.Delete();
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex.Message);
                log.Warn(ex.ToString());
                throw;
            }
        }

        /// <summary>
        ///   use the expiration metadata attribute to remove expired containers.
        /// </summary>
        /// <param name="prefix"> </param>
        /// <param name="ct"> </param>
        public static void GroomExpiredContainers(string prefix = null, CancellationToken? ct = null)
        {
            ILog log = ClassLogger.Create(typeof (AzureStorageAssistant));
            DebugOnlyLogger dblog = DebugOnlyLogger.Create(log);

            try
            {
                var account =
                    CloudStorageAccount.FromConfigurationSetting(CommonConfiguration.DefaultStorageConnection.ToString());
                var bc = account.CreateCloudBlobClient();

                if (ct.HasValue) ct.Value.ThrowIfCancellationRequested();

                IEnumerable<CloudBlobContainer> containers;
                if (!string.IsNullOrEmpty(prefix))
                    containers = bc.ListContainers(prefix);
                else
                    containers = bc.ListContainers();

                if (ct.HasValue) ct.Value.ThrowIfCancellationRequested();
                Parallel.ForEach(containers,
                                 c =>
                                     {
                                         c.FetchAttributes();
                                         if (ct.HasValue) ct.Value.ThrowIfCancellationRequested();
                                         if (c.Metadata.AllKeys.Contains(BlobMetaPropertyExpired))
                                         {
                                             DateTime expirationTime =
                                                 DateTime.Parse(c.Metadata[BlobMetaPropertyExpired],
                                                                CultureInfo.InvariantCulture);
                                             if (DateTime.UtcNow > expirationTime)
                                             {
                                                 c.Delete();
                                             }
                                         }
                                     });
            }
            catch (Exception ex)
            {
                log.Warn(ex.Message);
                log.Warn(ex.ToString());
                throw;
            }
        }
    }
}