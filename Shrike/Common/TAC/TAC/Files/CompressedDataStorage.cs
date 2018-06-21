using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zip;
using Newtonsoft.Json;

namespace AppComponents.Files
{
    public enum CompressedDataStorageLocalConfig
    {
        Host,
        Container,

        
    }

    public class CompressedBlobContainer<T>: IBlobContainer<T>
    {
        

        private JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        private string _container;
        private string _path1;
        

        private IFilesContainer _fc;

        public CompressedBlobContainer()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);

            _container = cf[CompressedDataStorageLocalConfig.Container];
            _path1 = cf[CompressedDataStorageLocalConfig.Host];
            
        }

        private IFilesContainer GetStorage()
        {
            if(null == _fc)
                _fc = Catalog.Preconfigure()
                    .Add(BlobContainerLocalConfig.ContainerHost, _container)
                    .Add(BlobContainerLocalConfig.ContainerName, _path1)
                    .ConfiguredResolve<IFilesContainer>();

            return _fc;
        }

        private byte[] Compress(T fo)
        {
            using (var ms = new MemoryStream())
            {
                using (var zf = new ZipFile())
                {
                    zf.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    var dat = JsonConvert.SerializeObject(fo, _serializerSettings);
                    zf.AddEntry("DataObject", dat);
                    zf.Save(ms);
                }

                return ms.ToArray();
            }
        }

        private T Decompress(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var zf = ZipFile.Read(ms))
                {
                    using (var str = zf["DataObject"].OpenReader())
                    {
                        using (var tr = new StreamReader(str))
                        {
                            var dat = tr.ReadToEnd();
                            var retval = JsonConvert.DeserializeObject<T>(dat, _serializerSettings);
                            return retval;
                        }
                    }

                }
            }

        }

     

        
        public void Delete(string objId)
        {
            GetStorage().Delete(objId);
        }

        public void DeleteContainer()
        {
            GetStorage().DeleteContainer();
        }

        public T Get(string objId)
        {
            var raw = GetStorage().Get(objId);
            var ro = Decompress(raw);
            return ro;
        }

        public bool Exists(string objId)
        {
            return GetStorage().Exists(objId);
        }

        public IEnumerable<T> GetAll()
        {
            var raw = GetStorage().GetAll();
            return raw.Select(it => Decompress(it));
        }

        public IEnumerable<string> GetAllIds()
        {
            return GetStorage().GetAllIds();
        }

        public Uri GetUri(string objId)
        {
            return GetStorage().GetUri(objId);
        }

        public void Save(string objId, T obj, TimeSpan? expiration = null)
        {
            var raw = Compress(obj);
            GetStorage().Save(objId,raw,expiration);
        }

        public void SaveAsync(string objId, T obj, TimeSpan? expiration = null)
        {
            var raw = Compress(obj);
            GetStorage().SaveAsync(objId, raw, expiration);
        }

        public void SetExpire(TimeSpan ts)
        {
            GetStorage().SetExpire(ts);
        }
    }

    public class CompressedDataStorage<T>
    {
        private JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        private string _container;
        private string _path1;
        private string _containerHost;

        public CompressedDataStorage()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);

            _container = cf[CompressedDataStorageLocalConfig.Container];
            _path1 = cf[CompressedDataStorageLocalConfig.Host];
            _containerHost = Path.Combine(_path1, _container);
        }

        private IFilesContainer GetStorage(string subStorage)
        {
            var fc = Catalog.Preconfigure()
                .Add(BlobContainerLocalConfig.ContainerHost, _containerHost)
                .Add(BlobContainerLocalConfig.ContainerName, subStorage)
                .ConfiguredResolve<IFilesContainer>();

            return fc;
        }

        private byte[] Compress(T fo)
        {
            using (var ms = new MemoryStream())
            {
                using (var zf = new ZipFile())
                {
                    zf.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    var dat = JsonConvert.SerializeObject(fo, _serializerSettings);
                    zf.AddEntry("DataObject", dat);
                    zf.Save(ms);
                }

                return ms.ToArray();
            }
        }

        private T Decompress(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var zf = ZipFile.Read(ms))
                {
                    using (var str = zf["DataObject"].OpenReader())
                    {
                        using (var tr = new StreamReader(str))
                        {
                            var dat = tr.ReadToEnd();
                            var retval = JsonConvert.DeserializeObject<T>(dat, _serializerSettings);
                            return retval;
                        }
                    }

                }
            }

        }

        public void StoreData(string subStorage, string id, T dataObject)
        {
            var fc = GetStorage(subStorage);
            
            var compressed = Compress(dataObject);
            fc.Save(id, compressed);
        }

        public T LoadData(string subStorage, string id)
        {
            var fc = GetStorage(subStorage);
            var raw = fc.Get(id);
            var ro = Decompress(raw);

            return ro;
        }
    }

   
}
