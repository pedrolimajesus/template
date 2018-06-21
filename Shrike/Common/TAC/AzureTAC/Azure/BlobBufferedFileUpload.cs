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
using System.IO;
using System.Text;
using AppComponents.Extensions.ExceptionEx;
using Microsoft.WindowsAzure.StorageClient;
using log4net;
using smarx.WazStorageExtensions;

namespace AppComponents.Azure
{
    public class BlobBufferedFileUpload : IFileUpload
    {
        public static readonly string UploadBufferContainer = "uploadbuffer";

        private DebugOnlyLogger _dblog;
        private ILog _log;

        public BlobBufferedFileUpload()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);
        }

        #region IFileUpload Members

        public Guid[] GetUploads()
        {
            List<Guid> retval = new List<Guid>();
            var blobs = Client.FromConfig().ForBlobs();
            var buffer = blobs.GetContainerReference(UploadBufferContainer);
            buffer.CreateIfNotExist();

            var theBlobs = buffer.ListBlobs();
            foreach (IListBlobItem b in theBlobs)
            {
                string uri = b.Uri.ToString();
                int pos = uri.IndexOf(UploadBufferContainer);
                string id = uri.Substring(pos + UploadBufferContainer.Length + 1, Guid.Empty.ToString().Length);
                retval.Add(new Guid(id));
            }


            return retval.ToArray();
        }

        public Guid BeginUpload(string fileName, Guid owner)
        {
            _dblog.InfoFormat("Begin uploading file {0} for owner {1}", fileName, owner);

            Guid id = Guid.NewGuid();

            var blobs = Client.FromConfig().ForBlobs();
            var buffer = blobs.GetContainerReference(UploadBufferContainer);
            buffer.CreateIfNotExist();

            string metaDataUri = string.Format("{0}/metadata.txt", id);
            var metadataInfo = buffer.GetBlobReference(metaDataUri);

            StringBuilder manifestContent = new StringBuilder();
            manifestContent.AppendLine(fileName);
            manifestContent.AppendLine(owner.ToString());
            manifestContent.AppendLine(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

            metadataInfo.UploadText(manifestContent.ToString());

            return id;
        }

        public void GetUploadMetadata(Guid id, out string fileName, out Guid owner, out DateTime creationTime)
        {
            var blobs = Client.FromConfig().ForBlobs();
            var buffer = blobs.GetContainerReference(UploadBufferContainer);
            buffer.CreateIfNotExist();

            string metaDataUri = string.Format("{0}/metadata.txt", id);
            var metadataInfo = buffer.GetBlobReference(metaDataUri);

            if (!metadataInfo.Exists())
            {
                var msg = string.Format("No metadata exists for buffered upload {0}", id);
                _log.ErrorFormat(msg);
                throw new FileNotFoundException(msg);
            }

            fileName = string.Empty;
            owner = Guid.Empty;
            creationTime = DateTime.MinValue;

            try
            {
                string metadata = metadataInfo.DownloadText();
                string[] items = metadata.Split('\n');

                fileName = items[0];
                owner = new Guid(items[1]);
                creationTime = DateTime.Parse(items[2], CultureInfo.InvariantCulture);

                _dblog.InfoFormat("Buffered upload: file: {0}, owner: {0}, creation time {0}", fileName, owner,
                                  creationTime);
            }
            catch (Exception ex)
            {
                CriticalLog.Always.ErrorFormat(string.Format("upload metadata corruption for {0}", id));
                CriticalLog.Always.TraceException(ex);
            }
        }

        public void UploadPart(Guid identifier, byte[] data, int sequence)
        {
            _log.InfoFormat("Uploading {0} bytes of data to {1} as sequence {2}", data.Length, identifier, sequence);

            var blobs = Client.FromConfig().ForBlobs();
            var buffer = blobs.GetContainerReference(UploadBufferContainer);
            buffer.CreateIfNotExist();

            string dataUri = string.Format("{0}/data.bin", identifier);

            var blob = buffer.GetBlockBlobReference(dataUri);
            string blockId = Convert.ToBase64String(BitConverter.GetBytes(sequence));
            using (MemoryStream ms = new MemoryStream(data))
            {
                ms.Seek(0, SeekOrigin.Begin);

                blob.PutBlock(blockId, ms, null);
            }
        }

        public void CancelUpload(Guid identifier)
        {
            _log.InfoFormat("Cancelling file upload {0}", identifier);

            var blobs = Client.FromConfig().ForBlobs();
            var buffer = blobs.GetContainerReference(UploadBufferContainer);
            buffer.CreateIfNotExist();

            string dataUri = string.Format("{0}/data.bin", identifier);
            var blob = buffer.GetBlockBlobReference(dataUri);

            blob.DeleteIfExists();

            string metaDataUri = string.Format("{0}/metadata.txt", identifier);
            var metadataInfo = buffer.GetBlobReference(metaDataUri);
            metadataInfo.DeleteIfExists();
        }

        public void CompleteUpload(Guid identifier, int partCount, Action<Guid, Stream> completionCallback)
        {
            var blobs = Client.FromConfig().ForBlobs();
            var buffer = blobs.GetContainerReference(UploadBufferContainer);
            buffer.CreateIfNotExist();

            string dataUri = string.Format("{0}/data.bin", identifier);

            var blob = buffer.GetBlockBlobReference(dataUri);
            string[] blockIds = new string[partCount + 1];
            for (int i = 0; i <= partCount; i++)
                blockIds[i] = Convert.ToBase64String(BitConverter.GetBytes(i));

            blob.PutBlockList(blockIds);

            using (MemoryStream ms = new MemoryStream())
            {
                blob.DownloadToStream(ms);
                ms.Seek(0, SeekOrigin.Begin);

                _log.InfoFormat("Completed upload of {0}", identifier);

                completionCallback(identifier, ms);
            }

            blob.DeleteIfExists();

            string metaDataUri = string.Format("{0}/metadata.txt", identifier);
            var metadataInfo = buffer.GetBlobReference(metaDataUri);
            metadataInfo.DeleteIfExists();
        }

        #endregion
    }
}