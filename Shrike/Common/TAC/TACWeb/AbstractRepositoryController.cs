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
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Threading;

using AppComponents.ControlFlow;
using AppComponents.Data;


namespace AppComponents.Web
{
    using System.Net.Http;
    using System.Web.Http;

    using global::Raven.Abstractions.Exceptions;

    public enum RepositoryControllerLocalConfig
    {
        DataRepository
    }

    /// <summary>
    /// Creates a web api rest interface from a data repository
    /// </summary>
    /// <typeparam name="TDataType">Type of data provided</typeparam>
    /// <typeparam name="TSummaryType">A summary data type exposing fewer fields of data type for grids. May be same as TDataType.</typeparam>
    /// <typeparam name="TSummaryPackageType">Usually just DataEnvelope[TSummaryType, TSummaryMetadataType]</typeparam>
    /// <typeparam name="TSummaryMetadataType">Typically MetadataModel, provided by ContextualDataAnnotationsModelMetadataProvider</typeparam>
    /// <typeparam name="TItemPackageType">Usually just DatumEnvelope[TDataType, TItemMetadataType]</typeparam>
    /// <typeparam name="TItemMetadataType">Typically MetadataModel, provided by ContextualDataAnnotationsModelMetadataProvider</typeparam>
    /// <typeparam name="TSummarizerType">Usually one of the helpers found in AppComponents.Data</typeparam>
    public abstract class AbstractRepositoryController<
        TDataType,
        TSummaryType,
        TSummaryPackageType,
        TSummaryMetadataType,
        TItemPackageType,
        TItemMetadataType> : ApiController
        where TDataType : class, new()
        where TSummaryType : class
        where TSummaryPackageType : DataEnvelope<TSummaryType, TSummaryMetadataType>, new()
        where TItemPackageType : DatumEnvelope<TDataType, TItemMetadataType>, new()
        where TItemMetadataType : class
        where TSummaryMetadataType : class
    {
        private readonly IDataRepositoryService
            <TDataType, TSummaryType, TSummaryPackageType, TSummaryMetadataType, TItemPackageType, TItemMetadataType>
            _dataRepository;

        protected AbstractRepositoryController(IConfig cf)
        {
            _dataRepository = cf.Get<IDataRepositoryService
                <TDataType, TSummaryType, TSummaryPackageType, TSummaryMetadataType, TItemPackageType, TItemMetadataType
                    >>(RepositoryControllerLocalConfig.DataRepository);
        }

        protected AbstractRepositoryController(IDataRepositoryService
                                                   <TDataType, TSummaryType, TSummaryPackageType, TSummaryMetadataType,
                                                   TItemPackageType, TItemMetadataType
                                                   > dataRepository)
        {
            _dataRepository = dataRepository;
        }

        public virtual void PostStore(TDataType document)
        {
            _dataRepository.Store(document);
        }

        public virtual HttpResponseMessage PostNew(TDataType document)
        {
            _dataRepository.CreateNew(document);
            var response = Request.CreateResponse<TDataType>(HttpStatusCode.Created, document);

            string uri = Url.Link("DefaultApi", new { id = DataDocument.GetDocumentId(document) });
            response.Headers.Location = new Uri(uri);
            return response;
        }

        public virtual HttpResponseMessage PostBatch(TDataType[] documents)
        {
            _dataRepository.CreateNewBatch(documents);
            return new HttpResponseMessage(HttpStatusCode.Created);
        }

        public virtual void PutUpdate(TDataType document)
        {


            try
            {
                _dataRepository.Update(document);
            }
            catch (InstanceNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
            catch(ConcurrencyException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Conflict));
            }

        }

        public virtual void PutUpdateBatch(TDataType[] documents)
        {
            try
            {
                _dataRepository.BatchUpdate(documents);
            }
            catch (InstanceNotFoundException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
            catch (ConcurrencyException)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Conflict));
            }

        }

        public virtual HttpResponseMessage DeleteItem(TDataType document)
        {
            _dataRepository.Delete(document);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        public virtual HttpResponseMessage DeleteById(string documentId)
        {
            _dataRepository.IdDelete(documentId);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        public virtual HttpResponseMessage DeleteBySummary(TSummaryType summary)
        {
            _dataRepository.ProxyDelete(summary);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        public virtual TSummaryPackageType GetQuery(QuerySpecification qs)
        {
            var retval =  _dataRepository.Query(qs);
            if (null == retval)
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            return retval;
        }

        public virtual TItemPackageType GetItemById(string id)
        {
            var retval = _dataRepository.Load(id);
            if (null == retval)
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            return retval;
        }

        public virtual TItemPackageType GetItemFromSummary(TSummaryType summary)
        {
            var retval = _dataRepository.Load(summary);
            if (null == retval)
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            return retval;
        }

        public virtual IEnumerable<TItemPackageType> GetItemsById(string[] ids)
        {
            var retval = _dataRepository.Load(ids);
            if (null == retval)
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            return retval;
        }

        public virtual IEnumerable<TItemPackageType> GetItemsBySummaries(TSummaryType[] summaries)
        {
            var retval = _dataRepository.Load(summaries);
            if (null == retval)
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            return retval;
        }


        public override System.Threading.Tasks.Task<HttpResponseMessage> ExecuteAsync(System.Web.Http.Controllers.HttpControllerContext controllerContext, CancellationToken cancellationToken)
        {
            ExtractCurrentContext(Request);
            return base.ExecuteAsync(controllerContext, cancellationToken);
        }

        private static void ExtractCurrentContext(HttpRequestMessage request)
        {
            string cultureName;
            var cf = Catalog.Factory.Resolve<IConfig>();

            // Attempt to read the culture cookie from Request
            var cultureHeader = request.Headers.AcceptLanguage.FirstOrDefault();
           
            if(null == cultureHeader || string.IsNullOrWhiteSpace(cultureHeader.Value)) 
                cultureName = cf.Get(WebLocalization.DefaultCulture, "en-US");
            else
            {
                cultureName = cultureHeader.Value;
            }

            // Validate culture name
            var isSupported = false;
            var rm = ContextualString.Resources;
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            var culture = cultures.FirstOrDefault(c => c.Name == cultureName);

            if (null != culture)
            {
                var rs = rm.GetResourceSet(culture, true, false);
                isSupported = rs != null;


                if (!isSupported)
                {
                    var ci = new CultureInfo(culture.TwoLetterISOLanguageName);
                    rs = rm.GetResourceSet(ci, true, false);
                    isSupported = rs != null;
                    if (isSupported)
                        cultureName = ci.Name;
                }
            }


            if (!isSupported)
            {
                cultureName = cf.Get(WebLocalization.DefaultCulture, "en-US");
            }


            // Modify current thread's culture            
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureName);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(cultureName);
        }
    }
}