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
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace AppComponents.Azure
{
    /// <summary>
    ///   Performs regular maintenance and grooming of azure storage entities
    /// </summary>
    public static class AzureCleanUp
    {
        /// <summary>
        ///   Registers the work in the catalog, to be used by the stochastic recurring worker.
        /// </summary>
        public static void Register()
        {
            Catalog.Services.RegisterInstance
                (new StochasticRecurringWorker.RecurringWork
                     {
                         Work = CleanExpiredContainers,
                         StochasticBoundaryMin = TimeSpan.FromHours(1.0),
                         StochasticBoundaryMax = TimeSpan.FromHours(12.0),
                         Schedule = DateTime.UtcNow
                     });

            Catalog.Services.RegisterInstance
                (new StochasticRecurringWorker.RecurringWork
                     {
                         Work = CleanExpiredBlobs,
                         StochasticBoundaryMin = TimeSpan.FromHours(1.0),
                         StochasticBoundaryMax = TimeSpan.FromHours(6.0),
                         Schedule = DateTime.UtcNow
                     });


            Catalog.Services.RegisterInstance
                (new StochasticRecurringWorker.RecurringWork
                     {
                         Work = CleanUploads,
                         StochasticBoundaryMin = TimeSpan.FromHours(4.0),
                         StochasticBoundaryMax = TimeSpan.FromHours(8.0),
                         Schedule = DateTime.UtcNow
                     });
        }


        /// <summary>
        ///   Deletes any blob containers marked as expired
        /// </summary>
        /// <param name="ct"> </param>
        private static void CleanExpiredContainers(CancellationToken ct)
        {
            AzureStorageAssistant.GroomExpiredContainers();
        }

        /// <summary>
        ///   searches for and deletes all blobs marked as expired
        /// </summary>
        /// <param name="ct"> </param>
        private static void CleanExpiredBlobs(CancellationToken ct)
        {
            try
            {
                var account =
                    CloudStorageAccount.FromConfigurationSetting(CommonConfiguration.DefaultStorageConnection.ToString());
                var bc = account.CreateCloudBlobClient();

                ct.ThrowIfCancellationRequested();
                var containers = bc.ListContainers();

                foreach (var c in containers)
                {
                    ct.ThrowIfCancellationRequested();
                    AzureStorageAssistant.CleanExpiredBlobsFrom(c.Name, ct);
                }
            }
            catch
            {
            }
        }


        /// <summary>
        ///   Searches the blob upload folder for uploads that are too old to complete, and deletes them.
        /// </summary>
        /// <param name="ct"> </param>
        private static void CleanUploads(CancellationToken ct)
        {
            AzureStorageAssistant.GroomOldBlobsFrom(BlobBufferedFileUpload.UploadBufferContainer,
                                                    TimeSpan.FromDays(30.0), ct);
        }
    }
}