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

using System.Diagnostics.Contracts;

namespace AppComponents
{
    public enum ImageStorageLocalConfig
    {
        ContainerName,
        CacheSize,
        CacheHitRenewsExpiration,
        OptionalExpirationTimeMinutes
    }

    [RequiresConfiguration]
    [ContractClass(typeof (IImageStorageContract))]
    public interface IImageStorage
    {
        byte[] RetrieveKey(string key, bool fromCache = true);
        void UploadImage(string key, byte[] image);
    }


    [ContractClassFor(typeof (IImageStorage))]
    internal abstract class IImageStorageContract : IImageStorage
    {
        #region IImageStorage Members

        public byte[] RetrieveKey(string key, bool fromCache = true)
        {
            Contract.Requires(!string.IsNullOrEmpty(key));
            return default(byte[]);
        }

        public void UploadImage(string key, byte[] image)
        {
            Contract.Requires(!string.IsNullOrEmpty(key));
            Contract.Requires(null != image);
            Contract.Requires(image.Length != 0);
        }

        #endregion
    }
}