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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AppComponents.Extensions.SerializationEx;
using AppComponents.Extensions.StringEx;
using Newtonsoft.Json;

namespace AppComponents.Files
{
    internal struct FtpDirectoryItem
    {
        public Uri BaseUri;

        public DateTime DateCreated;
        public bool IsDirectory;
        //public List<FtpDirectoryItem> Items;
        public string Name;

        public string AbsolutePath
        {
            get { return string.Format("{0}/{1}", BaseUri, Name); }
        }
    }

    public abstract class FtpBlobContainerBase
    {
        protected EntityAccess _access;
        protected Uri _container;
        protected string _containerName;
        protected NetworkCredential _credential;
        protected Uri _host;

        protected virtual FtpWebRequest RequestForFile(string objId)
        {
            var file = new Uri(_container, objId);
            return RequestForUri(file);
        }

        protected FtpWebRequest RequestForUri(Uri file)
        {
            var wr = (FtpWebRequest) WebRequest.Create(file);
            if (null != _credential)
                wr.Credentials = _credential;
            wr.UsePassive = true;
            wr.KeepAlive = false;
            wr.UseBinary = true;
            return wr;
        }

        protected FtpWebResponse Process(FtpWebRequest wr)
        {
            FtpWebResponse resp;


            try
            {
                resp = RequestFtp(wr);
                if (resp.StatusCode != FtpStatusCode.CommandOK)
                    throw new BlobAccessException(resp.StatusDescription);
            }
            catch (WebException ex)
            {
                var response = (FtpWebResponse) ex.Response;
                throw new BlobAccessException(response.StatusDescription);
            }

            return resp;
        }

        protected static FtpWebResponse RequestFtp(FtpWebRequest wr)
        {
            var resp = (FtpWebResponse) wr.GetResponse();
            return resp;
        }

        protected void CheckResponse(FtpWebRequest wr)
        {
            using (Process(wr))
            {
            }
        }

        public void Delete(string objId)
        {
            var wr = RequestForFile(objId);
            wr.Method = WebRequestMethods.Ftp.DeleteFile;
            CheckResponse(wr);

            try
            {
                var ewr = RequestForFile(objId + "-expiration");
                wr.Method = WebRequestMethods.Ftp.DeleteFile;
                CheckResponse(ewr);
            }
            catch
            {
            }
        }

        public void DeleteContainer()
        {
            var wr = RequestForUri(_container);
            wr.Method = WebRequestMethods.Ftp.RemoveDirectory;
            CheckResponse(wr);
        }

        public bool Exists(string objId)
        {
            var wr = RequestForFile(objId);
            wr.Method = WebRequestMethods.Ftp.GetDateTimestamp;

            try
            {
                wr.GetResponse();
            }
            catch (WebException ex)
            {
                var response = (FtpWebResponse) ex.Response;
                if (response.StatusCode ==
                    FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    response.Close();
                    return false;
                }
            }

            return true;
        }

        public virtual IEnumerable<string> GetAllIds()
        {
            var wr = RequestForUri(_container);
            wr.Method = WebRequestMethods.Ftp.ListDirectory;

            var returnValue = new List<FtpDirectoryItem>();

            using (var resp = Process(wr))
            {
                string[] list;

                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    list = reader.ReadToEnd().Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
                }

                foreach (string line in list)
                {
                    string data = line;


                    string date = data.Substring(0, 17);
                    DateTime dateTime = DateTime.Parse(date);
                    data = data.Remove(0, 24);


                    string dir = data.Substring(0, 5);
                    bool isDirectory = dir.Equals("<dir>", StringComparison.InvariantCultureIgnoreCase);
                    data = data.Remove(0, 5);
                    data = data.Remove(0, 10);


                    string name = data;


                    var item = new FtpDirectoryItem
                                   {
                                       BaseUri = _container,
                                       DateCreated = dateTime,
                                       IsDirectory = isDirectory,
                                       Name = name
                                   };

                    if (!isDirectory)
                        MaybeDeleteExpired(name);

                    returnValue.Add(item);
                }
            }

            return
                returnValue.Where(di => di.IsDirectory == false && !di.Name.Contains("-expiration.json")).Select(
                    di => di.Name);
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
            bool retval = false;
            try
            {
                var exp = InternalReadObject<DateTime>(expFile);
                if (exp < DateTime.UtcNow)
                    retval = true;
            }
            catch (WebException ex)
            {
                var response = (FtpWebResponse) ex.Response;
                if (response.StatusCode ==
                    FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    retval = false;
                }
                else
                {
                    throw new BlobAccessException(response.StatusDescription);
                }
                response.Close();
            }

            return retval;
        }

        protected void MaybeDeleteExpired(string objId)
        {
            if (CheckExpiration(objId))
                Delete(objId);
        }

        protected void GetCredentials(IConfig config)
        {
            var credString = config.Get(BlobContainerLocalConfig.OptionalCredentials, string.Empty);
            if (!string.IsNullOrEmpty(credString))
            {
                var init = credString.ParseInitialization();
                if (init.ContainsKey("user") && init.ContainsKey("password"))
                {
                    _credential = new NetworkCredential(init["user"], init["password"]);
                }
            }
        }

        protected void InitializeConfiguration(IConfig config)
        {
            _host = new Uri(config[BlobContainerLocalConfig.ContainerHost]);
            _containerName = config[BlobContainerLocalConfig.ContainerName];
            _container = new Uri(_host, _containerName);
            _access = (EntityAccess) Enum.Parse(typeof (EntityAccess),
                                                config.Get(BlobContainerLocalConfig.OptionalAccess,
                                                           EntityAccess.Private.ToString()));
        }

        public Uri GetUri(string objId)
        {
            return new Uri(_container, objId);
        }

        protected bool MaybeCreateContainer()
        {
            var wr = RequestForUri(_container);
            wr.Method = WebRequestMethods.Ftp.MakeDirectory;
            var retval = false;

            try
            {
                wr.GetResponse();
            }
            catch (WebException ex)
            {
                var response = (FtpWebResponse) ex.Response;
                retval = response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable;

                response.Close();
            }

            return retval;
        }

        protected void InternalSaveObject<T>(string objId, T obj)
        {
            var data = JsonConvert.SerializeObject(obj);
            var wr = RequestForFile(objId);
            wr.Method = WebRequestMethods.Ftp.UploadFile;

            using (var sourceStream = new StreamReader(data.ToStream()))
            {
                var fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
                sourceStream.Close();
                wr.ContentLength = fileContents.Length;

                using (var requestStream = wr.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                    requestStream.Close();
                }
            }
            CheckResponse(wr);
        }

        protected T InternalReadObject<T>(string objId)
        {
            T retval;
            var wr = RequestForFile(objId);
            wr.Method = WebRequestMethods.Ftp.DownloadFile;
            using (var resp = Process(wr))
            {
                using (var respStream = resp.GetResponseStream())
                {
                    using (var strReader = new StreamReader(respStream))
                    {
                        var text = strReader.ReadToEnd();
                        retval = JsonConvert.DeserializeObject<T>(text);
                    }
                }
            }

            return retval;
        }

        
    }

    public class FtpBlobContainer<T> : FtpBlobContainerBase, IBlobContainer<T>
    {
        public FtpBlobContainer()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            InitializeConfiguration(config);

            GetCredentials(config);
            MaybeCreateContainer();
        }

        #region IBlobContainer<T> Members

        public override IEnumerable<string> GetAllIds()
        {
            return base.GetAllIds().Select(id => id.EndsWith(".json") ? id.Remove(id.LastIndexOf(".json")) : id);
        }

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


        public void Save(string objId, T obj, TimeSpan? expiration = new TimeSpan?())
        {
            InternalSaveObject(objId, obj);

            if (expiration.HasValue)
                SetBlobExpire(objId, expiration.Value);
        }

        public void SaveAsync(string objId, T obj, TimeSpan? expiration = new TimeSpan?())
        {
            Task.Factory.StartNew(() => Save(objId, obj, expiration));
        }

        #endregion

        protected override FtpWebRequest RequestForFile(string objId)
        {
            var id = objId.EndsWith(".json") ? objId : objId + ".json";
            var file = new Uri(_container, id);
            return RequestForUri(file);
        }
    }

    public class FtpBlobFileContainer : FtpBlobContainerBase, IFilesContainer
    {
        public FtpBlobFileContainer()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            InitializeConfiguration(config);

            GetCredentials(config);
            MaybeCreateContainer();
        }

        #region IFilesContainer Members

        public byte[] Get(string objId)
        {
            MaybeDeleteExpired(objId);

            byte[] retval;
            var wr = RequestForFile(objId);
            wr.Method = WebRequestMethods.Ftp.DownloadFile;
            using (var resp = Process(wr))
            {
                using (var respStream = resp.GetResponseStream())
                {
                    retval = respStream.ToBytes();
                }
            }

            return retval;
        }

        public IEnumerable<byte[]> GetAll()
        {
            var items = GetAllIds();
            return items.Select(Get);
        }

        public void Save(string objId, byte[] obj, TimeSpan? expiration = null)
        {
            var wr = RequestForFile(objId);
            wr.Method = WebRequestMethods.Ftp.UploadFile;


            wr.ContentLength = obj.GetLength(0);

            using (var requestStream = wr.GetRequestStream())
            {
                requestStream.Write(obj, 0, obj.GetLength(0));
                requestStream.Close();
            }

            CheckResponse(wr);

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
            var wr = RequestForFile(objId);
            wr.Method = WebRequestMethods.Ftp.UploadFile;


            wr.ContentLength = data.Length;

            using (var requestStream = wr.GetRequestStream())
            {

                data.CopyTo(requestStream);
                requestStream.Close();
            }

            CheckResponse(wr);

            if (expiration.HasValue)
                SetBlobExpire(objId, expiration.Value);
        }

        public void ReadRange(string objId, long? from, long? to, Stream outStream)
        {
            var wr = RequestForFile(objId);
            wr.Method = WebRequestMethods.Ftp.DownloadFile;
            using (var resp = Process(wr))
            {
                using (var file = resp.GetResponseStream())
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
                            var length = (int) Math.Min(rangeLength.Value, file.Length - from.Value);
                            var buffer = new byte[length];
                            file.Read(buffer, 0, length);
                            outStream.Write(buffer, 0, length);
                        }
                        else
                        {
                            var length = (int) Math.Min(to.Value, file.Length);
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
                                var length = (int) (file.Length - from.Value);
                                var buffer = new byte[length];
                                file.Read(buffer, 0, length);
                                outStream.Write(buffer, 0, length);
                            }
                        }
                    }

                }
            }
        }


        public Stream ReadStream(string objId)
        {
            var wr = RequestForFile(objId);
            wr.Method = WebRequestMethods.Ftp.DownloadFile;
            using (var resp = Process(wr))
            {
                return resp.GetResponseStream();

            }
        }
    }
}