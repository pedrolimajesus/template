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
using System.Diagnostics.Contracts;
using System.IO;

namespace AppComponents
{
    public enum EntityAccess
    {
        Private,
        Public,
        ContainerPublic
    }

    public enum BlobContainerLocalConfig
    {
        ContainerName,
        ContainerHost,
        OptionalCredentials,
        OptionalAccess,
        OptionalContentType,
        DistributeMutexLocalConfig
    }


    public enum BlobContainerSvcReqs
    {
        Compressed
    }




    [RequiresConfiguration]
    [ContractClass(typeof (BlobContainerContract<>))]
    public interface IBlobContainer<T>
    {
        void Delete(string objId);
        void DeleteContainer();
        T Get(string objId);
        bool Exists(string objId);

        IEnumerable<T> GetAll();
        IEnumerable<string> GetAllIds();

        Uri GetUri(string objId);

        void Save(string objId, T obj, TimeSpan? expiration = null);

        void SaveAsync(string objId, T obj, TimeSpan? expiration = null);
        void SetExpire(TimeSpan ts);
    }


    public interface IFilesContainer : IBlobContainer<byte[]>
    {
        void SaveStream(string objId, Stream data, TimeSpan? expiration = null);
        void ReadRange(string objId, long? from, long? to, Stream outStream);
        Stream ReadStream(string objId);
    }


    [ContractClassFor(typeof (IBlobContainer<>))]
    internal abstract class BlobContainerContract<T> : IBlobContainer<T>
    {
        #region IBlobContainer<T> Members

        public void Delete(string objId)
        {
            Contract.Requires(!string.IsNullOrEmpty(objId));
        }

        public void DeleteContainer()
        {
        }

        public T Get(string objId)
        {
            Contract.Requires(!string.IsNullOrEmpty(objId));
            return default(T);
        }

        public bool Exists(string objId)
        {
            Contract.Requires(null != objId);
            return default(bool);
        }

        public IEnumerable<T> GetAll()
        {
            return default(IEnumerable<T>);
        }

        public IEnumerable<string> GetAllIds()
        {
            return default(IEnumerable<string>);
        }

        public Uri GetUri(string objId)
        {
            Contract.Requires(!string.IsNullOrEmpty(objId));
            return default(Uri);
        }

        public void Save(string objId, T obj, TimeSpan? expiration = null)
        {
            Contract.Requires(!string.IsNullOrEmpty(objId));
            Contract.Requires(null != obj);
        }

        public void SaveAsync(string objId, T obj, TimeSpan? expiration = null)
        {
            Contract.Requires(!string.IsNullOrEmpty(objId));
            Contract.Requires(null != obj);
        }

        public void SetExpire(TimeSpan ts)
        {
        }

        #endregion

        public string GetAsJson(string objId)
        {
            Contract.Requires(!string.IsNullOrEmpty(objId));
            return default(string);
        }
    }

    public class BlobAccessException : ApplicationException
    {
        public BlobAccessException()
        {
        }

        public BlobAccessException(string msg) : base(msg)
        {
        }

        public BlobAccessException(Exception ex): base(ex.Message,ex)
        {
        }
    }
}