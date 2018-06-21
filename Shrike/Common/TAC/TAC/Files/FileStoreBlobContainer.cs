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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AppComponents.Extensions.SerializationEx;
using Newtonsoft.Json;

namespace AppComponents.Files
{
    public abstract class FileStoreBlobContainerBase
    {
        protected EntityAccess _access;
        protected string _container;
        protected string _containerName;
        protected string _host;

        private const int DefaultTime = 30;//seconds

        protected virtual string FilePath(string objId)
        {
            return Path.Combine(_host, _container, objId);
        }

        public void Delete(string objId)
        {
            try
            {
                var fi = new FileInfo(FilePath(objId));
                if (fi.Exists)
                    fi.Delete();
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }
        }

        public bool Exists(string objId)
        {
            var fi = new FileInfo(FilePath(objId));
            return fi.Exists;
        }

        public void DeleteContainer()
        {
            try
            {
                var di = new DirectoryInfo(_container);
                if (di.Exists)
                    di.Delete();
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }
        }

        public virtual IEnumerable<string> GetAllIds()
        {
            var di = new DirectoryInfo(_container);

            try
            {
                return di.GetFiles().Select(f => f.Name);
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }
        }

        public void SetExpire(TimeSpan ts)
        {
            var expFile = _containerName + "-container-expiration.json";
            InternalSaveObject(expFile, DateTime.UtcNow + ts);
        }

        protected void SetBlobExpire(string objId, TimeSpan ts)
        {
            var expFile = objId + "-expiration.json";
            InternalSaveObject(expFile, DateTime.UtcNow + ts);
        }

        protected bool CheckExpiration(string objId)
        {
            var expFile = objId + "-expiration.json";
            var retval = false;
            try
            {
                var mutex = Catalog.Preconfigure().Add(DistributedMutexLocalConfig.Name, objId).ConfiguredResolve<IDistributedMutex>();

                if (!mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
                {
                    mutex.Release();
                    throw new TimeoutException(string.Format("mutex timed out checking {0}", objId));
                }

                var fi = new FileInfo(expFile);
                if (fi.Exists)
                {
                    string data;
                    using (var stream = fi.OpenText())
                    {
                        data = stream.ReadToEnd();
                    }

                    var expireTime = JsonConvert.DeserializeObject<DateTime>(data);
                    retval = expireTime > DateTime.UtcNow;
                }

                mutex.Release();
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }

            return retval;
        }

        protected void MaybeDeleteExpired(string objId)
        {
            if (CheckExpiration(objId))
                Delete(objId);
        }

        protected void InitializeConfiguration(IConfig config)
        {

            var hostKey = config[BlobContainerLocalConfig.ContainerHost];
            _containerName = config[BlobContainerLocalConfig.ContainerName];


            _host = hostKey;

            //_container =_containerName;//it should combine hostkey before

            _container = Path.Combine(hostKey, _containerName);

            //if (!Directory.Exists(_container))
            //{
            //    Directory.CreateDirectory(_container);
            //}

            _access =
                (EntityAccess)
                Enum.Parse(
                    typeof(EntityAccess),
                    config.Get(BlobContainerLocalConfig.OptionalAccess, EntityAccess.Private.ToString()));
        }

        public Uri GetUri(string objId)
        {
            return new Uri(FilePath(objId));
        }

        protected bool MaybeCreateContainer()
        {
            //var di = new DirectoryInfo(Path.Combine(_host,_container));
            var di = new DirectoryInfo(_container);

            if (!di.Exists)
            {
                try
                {
                    di.Create();
                }
                catch (Exception ex)
                {
                    throw new BlobAccessException(ex);
                }

                return true;
            }

            return false;
        }

        protected void InternalSaveObject<T>(string objId, T obj)
        {
            try
            {
                var mutex = Catalog.Preconfigure().Add(DistributedMutexLocalConfig.Name, objId).ConfiguredResolve<IDistributedMutex>();

                if (!mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
                {
                    mutex.Release();
                    throw new TimeoutException(string.Format("mutex timed out saving {0}", objId));
                }

                var data = JsonConvert.SerializeObject(obj);
                File.WriteAllText(this.FilePath(objId), data);

                mutex.Release();
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }
        }

        protected T InternalReadObject<T>(string objId)
        {
            try
            {
                var mutex = Catalog.Preconfigure().Add(DistributedMutexLocalConfig.Name, objId).ConfiguredResolve<IDistributedMutex>();

                if (!mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
                {
                    mutex.Release();
                    throw new TimeoutException(string.Format("mutex timed out reading {0}", objId));
                }

                var fu = FilePath(objId);
                var fi = new FileInfo(fu);
                
                string data;
                using (var stream = fi.OpenText())
                {
                    data = stream.ReadToEnd();
                }

                var result = JsonConvert.DeserializeObject<T>(data);

                mutex.Release();
                
                return result;
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }
        }
    }


    public class FileStoreBlobContainer<T> : FileStoreBlobContainerBase, IBlobContainer<T>
    {
        public FileStoreBlobContainer()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            InitializeConfiguration(cf);
            MaybeCreateContainer();
        }

        #region IBlobContainer<T> Members

        public T Get(string objId)
        {
            MaybeDeleteExpired(objId);
            return InternalReadObject<T>(objId);
        }

        public IEnumerable<T> GetAll()
        {
            var items = GetAllIds();
            return items.Select(Get);
        }


        public override IEnumerable<string> GetAllIds()
        {
            return base.GetAllIds().Select(id => id.EndsWith(".json") ? id.Remove(id.LastIndexOf(".json")) : id);
        }

        public void Save(string objId, T obj, TimeSpan? expiration = null)
        {
            InternalSaveObject(objId, obj);

            if (expiration.HasValue)
                SetBlobExpire(objId, expiration.Value);
        }

        public void SaveAsync(string objId, T obj, TimeSpan? expiration = null)
        {
            Task.Factory.StartNew(() => Save(objId, obj, expiration));
        }

        #endregion

        protected override string FilePath(string objId)
        {
            var id = objId.EndsWith(".json") ? objId : objId + ".json";
            return Path.Combine(_host,_container, id);
        }
    }


    public class FileStoreBlobFileContainer : FileStoreBlobContainerBase, IFilesContainer
    {
        private const int DefaultTime = 30;//seconds

        public FileStoreBlobFileContainer()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            InitializeConfiguration(cf);
            MaybeCreateContainer();
        }

        #region  Members

        public byte[] Get(string objId)
        {
            MaybeDeleteExpired(objId);
            try
            {
                var fi = new FileInfo(FilePath(objId));
                return fi.OpenRead().ToBytes();
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }
        }

        public IEnumerable<byte[]> GetAll()
        {
            var items = GetAllIds();
            try
            {
                return items.Select(Get);
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }
        }

        public void Save(string objId, byte[] obj, TimeSpan? expiration = null)
        {
            try
            {
                File.WriteAllBytes(FilePath(objId), obj);
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }


            if (expiration.HasValue)
                SetBlobExpire(objId, expiration.Value);
        }

        public void SaveAsync(string objId, byte[] obj, TimeSpan? expiration = null)
        {
            Task.Factory.StartNew(() => Save(objId, obj, expiration));
        }

        #endregion

        public void SaveStream(string objId, Stream data, TimeSpan? expiration = null)
        {
            try
            {
                var mutex = Catalog.Preconfigure().Add(DistributedMutexLocalConfig.Name, objId).ConfiguredResolve<IDistributedMutex>();
                if (!mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
                {
                    mutex.Release();
                    throw new TimeoutException(string.Format("mutex timed out saving stream {0}", objId));
                }

                using (var file = File.OpenWrite(FilePath(objId)))
                {
                    data.CopyTo(file);
                    file.Flush();
                }

                mutex.Release();
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }


            if (expiration.HasValue)
                SetBlobExpire(objId, expiration.Value);
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }


        public Stream ReadStream(string objId)
        {
            var mutex = Catalog.Preconfigure().Add(DistributedMutexLocalConfig.Name, objId).ConfiguredResolve<IDistributedMutex>();
            if (!mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
            {
                mutex.Release();
                throw new TimeoutException(string.Format("mutex timed out saving stream {0}", objId));
            }

            var file = File.OpenRead(FilePath(objId));

            mutex.Release();

            return file;
        }


        public void ReadRange(string objId, long? from, long? to, Stream outStream)
        {
            var mutex = Catalog.Preconfigure().Add(DistributedMutexLocalConfig.Name, objId).ConfiguredResolve<IDistributedMutex>();
            if (!mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
            {
                mutex.Release();
                throw new TimeoutException(string.Format("mutex timed out saving stream {0}", objId));
            }

            using (var file = File.OpenRead(FilePath(objId)))
            {
                if (from != null)
                {
                    file.Seek(from.Value, SeekOrigin.Begin);


                    if (from == 0 && (to == null || to >= file.Length))
                    {
                        file.CopyTo(outStream);

                    }
                }
                if (to != null)
                {

                    if (from != null)
                    {
                        long? rangeLength = to - from;
                        var length = (int)Math.Min(rangeLength.Value, file.Length - from.Value);
                        var buffer = new byte[length];
                        file.Read(buffer, 0, length);
                        outStream.Write(buffer, 0, length);
                    }
                    else
                    {
                        var length = (int)Math.Min(to.Value, file.Length);
                        var buffer = new byte[length];
                        file.Read(buffer, 0, length);
                        outStream.Write(buffer, 0, length);
                    }
                }
                else
                {

                    if (from != null)
                    {
                        if (from < file.Length)
                        {
                            var length = (int)(file.Length - from.Value);
                            var buffer = new byte[length];
                            file.Read(buffer, 0, length);
                            outStream.Write(buffer, 0, length);
                        }
                    }
                    else
                    {
                        file.CopyTo(outStream);
                    }
                }

            }
            mutex.Release();
        }
    }
}