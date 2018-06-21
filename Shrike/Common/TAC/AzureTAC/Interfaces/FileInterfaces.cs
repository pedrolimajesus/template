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
using System.Diagnostics.Contracts;
using System.IO;

namespace AppComponents
{
    [ContractClass(typeof (IFileUploadContract))]
    public interface IFileUpload
    {
        Guid BeginUpload(string fileName, Guid owner);
        void UploadPart(Guid identifier, byte[] data, int sequence);
        void GetUploadMetadata(Guid id, out string fileName, out Guid owner, out DateTime creationTime);
        Guid[] GetUploads();
        void CancelUpload(Guid id);
        void CompleteUpload(Guid identifier, int partCount, Action<Guid, Stream> completionCallback);
    }

    [ContractClass(typeof (IFileDownloadContract))]
    public interface IFileDownload
    {
        void DeleteData(string file);
        byte[] DownloadData(string file, int start, int length);
    }

    [ContractClassFor(typeof (IFileUpload))]
    internal abstract class IFileUploadContract : IFileUpload
    {
        #region IFileUpload Members

        public Guid BeginUpload(string fileName, Guid owner)
        {
            Contract.Requires(!string.IsNullOrEmpty(fileName));
            Contract.Requires(owner != Guid.Empty);
            return default(Guid);
        }

        public void UploadPart(Guid identifier, byte[] data, int sequence)
        {
            Contract.Requires(identifier != Guid.Empty);
            Contract.Requires(null != data);
            Contract.Requires(data.Length != 0);
            Contract.Requires(sequence >= 0);
        }

        public void GetUploadMetadata(Guid id, out string fileName, out Guid owner, out DateTime creationTime)
        {
            Contract.Requires(id != Guid.Empty);


            fileName = default(string);
            owner = default(Guid);
            creationTime = default(DateTime);
        }

        public Guid[] GetUploads()
        {
            return default(Guid[]);
        }

        public void CancelUpload(Guid id)
        {
        }

        public void CompleteUpload(Guid identifier, int partCount, Action<Guid, Stream> completionCallback)
        {
            Contract.Requires(identifier != Guid.Empty);
            Contract.Requires(partCount >= 0);
            Contract.Requires(completionCallback != null);
        }

        #endregion
    }

    [ContractClassFor(typeof (IFileDownload))]
    internal abstract class IFileDownloadContract : IFileDownload
    {
        #region IFileDownload Members

        public void DeleteData(string file)
        {
            Contract.Requires(!string.IsNullOrEmpty(file));
        }

        public byte[] DownloadData(string file, int start, int length)
        {
            Contract.Requires(!string.IsNullOrEmpty(file));
            Contract.Requires(start >= 0);
            Contract.Requires(length >= 0);

            return default(byte[]);
        }

        #endregion
    }
}