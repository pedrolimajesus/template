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
using System.Linq;
using System.Management.Instrumentation;
using AppComponents.Extensions.QuerySpecificationEx;
using AppComponents.Raven;
using Raven.Abstractions.Commands;
using Raven.Client.Linq;

namespace AppComponents.Data
{
    


    public class DocumentRepositoryService<
        TDataType,
        TSummaryType,
        TSummaryPackageType,
        TSummaryMetadataType,
        TItemPackageType,
        TItemMetadataType> :
            IDataRepositoryService
                <TDataType, TSummaryType, TSummaryPackageType, TSummaryMetadataType, TItemPackageType, TItemMetadataType>
        where TDataType : class, new()
        where TSummaryType : class, new()
        where TSummaryPackageType : DataEnvelope<TSummaryType, TSummaryMetadataType>, new()
        where TItemPackageType : DatumEnvelope<TDataType, TItemMetadataType>, new()
        where TItemMetadataType : class
        where TSummaryMetadataType : class
    {
        private IContextFilter _contextFilter = new NoContextFilter();

        private IMetadataProvider<TDataType, TItemMetadataType> _itemMetadataSource;
        private ISummarizer<TDataType, TSummaryType> _summarizer;
        private IMetadataProvider<TSummaryType, TSummaryMetadataType> _summaryMetadataSource;
        private Action<TDataType, TDataType> _updateAssignment;

        public DocumentRepositoryService()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);

            _summaryMetadataSource =
                config.Get<IMetadataProvider<TSummaryType, TSummaryMetadataType>>(
                    DataRepositoryServiceLocalConfig.SummaryMetadataProvider);
            _itemMetadataSource =
                config.Get<IMetadataProvider<TDataType, TItemMetadataType>>(
                    DataRepositoryServiceLocalConfig.ItemMetadataProvider);
            _summarizer = config.Get<ISummarizer<TDataType, TSummaryType>>(DataRepositoryServiceLocalConfig.Summarizer);
            _updateAssignment =
                config.Get<Action<TDataType, TDataType>>(DataRepositoryServiceLocalConfig.UpdateAssignment);
            if (config.SettingExists(DataRepositoryServiceLocalConfig.ContextFilter))
                _contextFilter = config.Get<IContextFilter>(DataRepositoryServiceLocalConfig.ContextFilter);
        }

        #region IDataRepositoryService<TDataType,TSummaryType,TSummaryPackageType,TSummaryMetadataType,TItemPackageType,TItemMetadataType,TSummarizerType> Members

        public void Store(TDataType document)
        {
            StoreDocument(document);
        }

        public string CreateNew(TDataType document)
        {
            //DataDocument.NixId(document);
            return StoreDocument(document);
        }


        public void CreateNewBatch(IList<TDataType> documents)
        {
            foreach (var d in documents) DataDocument.NixId(d);
            StoreDocuments(documents);
        }


        public void Update(TDataType document)
        {
            var id = DataDocument.GetDocumentId(document);
            using (var dc = DocumentStoreLocator.ContextualResolve())
            {
                var existing = dc.Load<TDataType>(id);
                if(null == existing)
                    throw new InstanceNotFoundException();
               
                _updateAssignment(existing, document);
                dc.SaveChanges();
            }
        }

        public void BatchUpdate(IList<TDataType> documents)
        {
            using (var dc = DocumentStoreLocator.ContextualResolve())
            {
                var existing = dc.Load<TDataType>(documents.Select(DataDocument.GetDocumentId));
                foreach (var item in existing)
                {
                    var update =
                        documents.First(d => DataDocument.GetDocumentId(d) == DataDocument.GetDocumentId(item));
                    if (null == update)
                        throw new InstanceNotFoundException();
                    _updateAssignment(item, update);
                }

                dc.SaveChanges();
            }
        }

        public void Delete(TDataType document)
        {
            var id = DataDocument.GetDocumentId(document);
            IdDelete(id);
        }

        public void ProxyDelete(TSummaryType documentProxy)
        {
            var id = _summarizer.Identify(documentProxy);
            IdDelete(id);
        }

        public void IdDelete(string id)
        {
            using (var dc = DocumentStoreLocator.ContextualResolve())
            {
                var target = dc.Load<TDataType>(id);
                dc.Delete(target);
                dc.SaveChanges();
            }
        }

        public void DeleteBatch(IList<TDataType> documents)
        {
            IdDeleteBatch(documents.Select(DataDocument.GetDocumentId).ToList());
        }

        public void IdDeleteBatch(IList<string> ids)
        {
            using (var dc = DocumentStoreLocator.ContextualResolve())
            {
                var docs = dc.Load<TDataType>(ids);
                foreach (var doc in docs)
                    dc.Delete(doc);
                dc.SaveChanges();
            }
        }

        public void ProxyDeleteBatch(IList<TSummaryType> documentProxies)
        {
            IdDeleteBatch(documentProxies.Select(dp => _summarizer.Identify(dp)).ToList());
        }

        public TItemPackageType Load(string id)
        {
            var pk = new TItemPackageType {Metadata = _itemMetadataSource.Metadata};
            using (var dc = DocumentStoreLocator.ContextualResolve())
            {
                pk.Item = dc.Load<TDataType>(id);
            }

            return pk;
        }

        public TItemPackageType Load(TSummaryType documentProxy)
        {
            return Load(_summarizer.Identify(documentProxy));
        }

        public IList<TItemPackageType> Load(IList<string> id)
        {
            var retval = new List<TItemPackageType>();
            using (var dc = DocumentStoreLocator.ContextualResolve())
            {
                var items = dc.Load<TDataType>(id);
                retval.AddRange(
                    items.Select(i => new TItemPackageType {Item = i, Metadata = _itemMetadataSource.Metadata}));
            }

            return retval;
        }

        public IList<TItemPackageType> Load(IList<TSummaryType> proxies)
        {
            var retval = new List<TItemPackageType>();
            using (var dc = DocumentStoreLocator.ContextualResolve())
            {
                var items = dc.Load<TDataType>(proxies.Select(p => _summarizer.Identify(p)));
                retval.AddRange(
                    items.Select(i => new TItemPackageType {Item = i, Metadata = _itemMetadataSource.Metadata}));
            }

            return retval;
        }

        public TSummaryPackageType All()
        {
            var pk = new TSummaryPackageType();
            using (var dc = DocumentStoreLocator.ContextualResolve())
            {
                var q = dc.Query<TDataType>();
                pk.Bookmark = new RavenPageBookmark(1000);
                pk.Metadata = _summaryMetadataSource.Metadata;
                pk.Items = q.GetAllUnSafe().Select(doc=>_summarizer.Summarize(doc)).ToList();
                return pk;
            }

        }

        public TSummaryPackageType Query(QuerySpecification qs)
        {
            var pk = new TSummaryPackageType();
            using (var dc = DocumentStoreLocator.ContextualResolve())
            {
                var q = dc.Query<TDataType>();

                q.ApplySpecFilter(qs);
                q = (IRavenQueryable<TDataType>) _contextFilter.ApplyContextFilter(q);

                var pbm = new RavenPageBookmark(qs.BookMark);
                q = q.Page(pbm);

                var items = q.Select(d => _summarizer.Summarize(d)).ToList();
                pk.Bookmark = pbm;
                pk.Items = items;
                pk.Metadata = _summaryMetadataSource.Metadata;
            }

            return pk;
        }

        public TSummaryPackageType NamedQuery(string registrationName, string[] arguments, IPageBookmark bookMark)
        {
            var qh =
                Catalog.Factory.Resolve
                    <IRegisteredQueryHandler<TSummaryType, TSummaryPackageType, TSummaryMetadataType>>(
                        registrationName);
            return qh.ExecuteQuery(arguments, bookMark);
        }

        #endregion

        private static string StoreDocument(TDataType document)
        {
            string id;
            using (var dc = DocumentStoreLocator.ContextualResolve())
            {
                dc.Store(document);
                dc.SaveChanges();
                id = dc.Advanced.GetDocumentId(document);
            }

            return id;
        }

        private void StoreDocuments(IEnumerable<TDataType> documents)
        {
            using (var dc = DocumentStoreLocator.ContextualResolve())
            {
                foreach (var doc in documents) dc.Store(doc);
                dc.SaveChanges();
            }
        }
    }
}