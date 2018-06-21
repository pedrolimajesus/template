using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AppComponents.Data
{
    using Microsoft.Isam.Esent.Collections.Generic;

    public class EsentBlobContainer<T>: IBlobContainer<T>, IDisposable
    {
        private string _host, _container, _path;
        private PersistentDictionary<string, string> _persistentDictionary;

        private readonly IDistributedMutex mutex;

        private const int DefaultTime = 30;//seconds

        public EsentBlobContainer()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _host = cf[BlobContainerLocalConfig.ContainerHost];
            _container = cf[BlobContainerLocalConfig.ContainerName];

            _path = new Uri(string.Format("file:///{0}", Path.Combine(_host, _container))).LocalPath;

            mutex = Catalog.Preconfigure().Add(DistributedMutexLocalConfig.Name, "ApplicationDeployments").ConfiguredResolve<IDistributedMutex>();

            if (mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
            {
                _persistentDictionary = new Microsoft.Isam.Esent.Collections.Generic.PersistentDictionary<string, string>(_path);
                mutex.Release();
            }
            else
            {
                throw new TimeoutException("ApplicationDeployments Mutex Timeout");
            }
        }

        //protected void InitializeConfiguration(IConfig config)
        //{

        //    var hostKey = config[BlobContainerLocalConfig.ContainerHost];
        //    _containerName = config[BlobContainerLocalConfig.ContainerName];


        //    _host = hostKey;

        //    _container = _containerName;
        //    _access = (EntityAccess)Enum.Parse(typeof(EntityAccess),
        //                                        config.Get(BlobContainerLocalConfig.OptionalAccess,
        //                                                   EntityAccess.Private.ToString()));
        //}

        protected void InternalSaveObject<TData>(string objId, TData obj)
        {
            try
            {
                if (mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
                {
                    var data = JsonConvert.SerializeObject(obj);
                    if (_persistentDictionary.ContainsKey(objId))
                    {
                        _persistentDictionary[objId] = data;
                    }
                    else
                    {
                        _persistentDictionary.Add(objId, data);

                    }

                    _persistentDictionary.Flush();
                    mutex.Release();
                }
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }
        }

        protected TData InternalReadObject<TData>(string objId)
        {
            try
            {
                string data = null;
                if (mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
                {
                    data = _persistentDictionary[objId];
                    mutex.Release();
                }
                return JsonConvert.DeserializeObject<TData>(data);
            }
            catch (Exception ex)
            {
                throw new BlobAccessException(ex);
            }
        }

        public void Delete(string objId)
        {
            if (mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
            {
                if (!_persistentDictionary.ContainsKey(objId)) return;

                _persistentDictionary.Remove(objId);
                var expFile = objId + "-expiration.json";
                if (_persistentDictionary.ContainsKey(expFile)) _persistentDictionary.Remove(expFile);
                _persistentDictionary.Flush();
                mutex.Release();
            }
        }

        public void DeleteContainer()
        {
            if (mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
            {
                _persistentDictionary.Dispose();
                _persistentDictionary = null;
                PersistentDictionaryFile.DeleteFiles(_path);
                mutex.Release();
            }
        }

        public T Get(string objId)
        {
            MaybeDeleteExpired(objId);
            return InternalReadObject<T>(objId);
        }

        public bool Exists(string objId)
        {
            var defaultR = false;
            if (mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
            {
                defaultR = _persistentDictionary.ContainsKey(objId);
                mutex.Release();
            }
            return defaultR;
        }

        public IEnumerable<T> GetAll()
        {
            var items = GetAllIds();
            return items.Select(Get);
        }

        public IEnumerable<string> GetAllIds()
        {
            IEnumerable<string> ids = null;
            if (mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
            {
                ids = _persistentDictionary.Keys;
                mutex.Release();
            }
            return ids;
        }

        public Uri GetUri(string objId)
        {
            return null;
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

       

        public void SetExpire(TimeSpan ts)
        {
            var expFile =  "container-expiration.json";
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
                if (mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
                {
                    if (_persistentDictionary.ContainsKey(expFile))
                    {
                        var data = _persistentDictionary[expFile];
                        var expireTime = JsonConvert.DeserializeObject<DateTime>(data);
                        retval = expireTime > DateTime.UtcNow;
                    }
                    mutex.Release();
                }
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
            {
                Delete(objId);
            }
        }

        private bool _isDisposed = false;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (mutex.Wait(TimeSpan.FromSeconds(DefaultTime)))
                {
                    if (null != _persistentDictionary) _persistentDictionary.Dispose();
                    mutex.Release();
                }
                if (null!= mutex) mutex.Dispose();
            }
        }
    }
}
