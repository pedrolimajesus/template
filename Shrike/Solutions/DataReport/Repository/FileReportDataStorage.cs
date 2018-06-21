

namespace Shrike.Data.Reports.Repository
{
    using System;
    using System.IO;
    using System.Linq;

    using AppComponents;

    using Ionic.Zip;

    using Raven.Imports.Newtonsoft.Json;

    using Shrike.Data.Reports.Base;

    public enum ReportDataStorageLocalConfig
    {
        HostName,
        OptionalContainerName
    }

    /// <summary>
    /// Class to store and recover ReportObject objects from the local file system.
    /// </summary>
    public class FileReportDataStorage : IReportDataStorage
    {
        public static readonly string DefaultBaseContainer = @"C:/LoK/AppData";
        public static readonly string DefaultContainer = @"ReportData";

        private JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.Indented};

        /// <summary>
        /// Returns a IFilesContainer that best fits the metadata object requirements.
        /// </summary>
        /// <param name="metadata">ReportLog object used to recover the IFilesContainer</param>
        /// <returns>The IFilesContainer object</returns>
        private IFilesContainer GetStorage(ReportLog metadata)
        {
            var cf = Catalog.Factory.Resolve<IConfig>();
            string tenant = string.Empty;
            if (!string.IsNullOrWhiteSpace(metadata.TenantRoute))
            {
                var tenantUri = new Uri(metadata.TenantRoute);
                tenant = tenantUri.Segments.Count() > 1 ? tenantUri.Segments[1] : string.Empty;
            }

            if (string.IsNullOrWhiteSpace(tenant))
                tenant = "global";
           
            var containerBase = cf.Get(CommonConfiguration.DistributedFileShare, DefaultBaseContainer);
            
            var container = cf.Get(ReportDataStorageLocalConfig.OptionalContainerName, DefaultContainer);

            var containerHost = Path.Combine(containerBase, container);
            
            var fc = Catalog.Preconfigure()
                .Add(BlobContainerLocalConfig.ContainerHost, containerHost)
                .Add(BlobContainerLocalConfig.ContainerName, tenant)
                .ConfiguredResolve<IFilesContainer>();

            return fc;
        }

        /// <summary>
        /// Compress a ReportObject and return its array of bytes.
        /// </summary>
        /// <param name="ro">ReportObject to be compressed</param>
        /// <returns>Array of bytes representing the compressed ReportObject</returns>
        private byte[] Compress(ReportObject ro)
        {
            using (var ms = new MemoryStream())
            {
                using (var zf = new ZipFile())
                {
                    zf.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    var dat = JsonConvert.SerializeObject(ro, this._serializerSettings);
                    zf.AddEntry("ReportObject", dat);
                    zf.Save(ms);
                }

                return ms.ToArray();
            }
        }


        /// <summary>
        /// Method to decompress an array of bytes in to a ReportObject
        /// </summary>
        /// <param name="data">Array of compressed bytes </param>
        /// <returns>ReportObject contained into the array of bytes</returns>
        private ReportObject Decompress(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var zf = ZipFile.Read(ms))
                {
                    using (var str = zf["ReportObject"].OpenReader())
                    {
                        using (var tr = new StreamReader(str))
                        {
                            var dat = tr.ReadToEnd();
                            var retval = JsonConvert.DeserializeObject<ReportObject>(dat, this._serializerSettings);
                            return retval;
                        }
                    }

                }
            }

        }

        /// <summary>
        /// Stores a ReportLog object and its object data into a IFilesContainer container.
        /// The ReportLog object and its data is stored into a ReportObject object.
        /// 
        /// The Report object is compressed before it is saved.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="metadata">ReportLog object to saved</param>
        /// <param name="dataObject">Data associated to the ReportLog</param>
        public void StoreReportData<T>(ReportLog metadata, T dataObject)
        {
            if (string.IsNullOrEmpty(metadata.Id))
                metadata.Id = Guid.NewGuid().ToString();
            //fc is a local storage FileStoreBlobFileContainer
            var fc = this.GetStorage(metadata);
            var ro = new ReportObject
            {
                Metadata = metadata,
                ReportData = dataObject
            };
            var compressed = this.Compress(ro);

            //save into local disk
            fc.Save(metadata.Id, compressed);
        }

        /// <summary>
        /// Returns the ReportObject associated to ReportLog that was saved previously.
        /// </summary>
        /// <param name="metadata">Metadata object used to recover the ReportObject</param>
        /// <returns>ReportObject associated to the metadata</returns>
        public ReportObject LoadReportData(ReportLog metadata)
        {
            var fc = this.GetStorage(metadata);
            var raw = fc.Get(metadata.Id);
            var ro = this.Decompress(raw);
            return ro;
        }
    }
}
