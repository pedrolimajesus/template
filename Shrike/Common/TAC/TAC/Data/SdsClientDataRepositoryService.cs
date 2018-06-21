using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.Extensions.QuerySpecificationEx;

namespace AppComponents.Data
{

    public enum SdsClientDataRepositoryServiceLocalConfig
    {
        SdsClient,
        TableId
    }

    public class SdsClientDataRepositoryService<
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
        private ISummarizer<TDataType,TSummaryType> _summarizer;
        private IMetadataProvider<TSummaryType, TSummaryMetadataType> _summaryMetadataSource;
        private Action<TDataType, TDataType> _updateAssignment;
        private IStructuredDataClient _client;
        private Enum _tableId;

        public SdsClientDataRepositoryService()
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

            _client = config.Get<IStructuredDataClient>(SdsClientDataRepositoryServiceLocalConfig.SdsClient);
            _tableId = config.Get<Enum>(SdsClientDataRepositoryServiceLocalConfig.SdsClient);

        }

        public void Store(TDataType document)
        {
            using (var trx = _client.BeginTransaction())
            {
                var tbl = _client.OpenTable<TDataType>(_tableId);

                var readData = tbl.Fetch(new string[] { DataDocument.GetDocumentId(document) });
                
                if (readData.Any())
                {
                    InternalUpdate(document, tbl);
                }
                else
                {
                    tbl.AddNew(DataDocument.GetDocumentId(document), document);
                }

                _client.Commit();
                
            }
        }


        public string CreateNew(TDataType document)
        {
            using (var trx = _client.BeginTransaction())
            {
                var tbl = _client.OpenTable<TDataType>(_tableId);
                tbl.AddNew(DataDocument.GetDocumentId(document), document);
                _client.Commit();
            }

            return DataDocument.GetDocumentId(document);
        }

        public void CreateNewBatch(IList<TDataType> documents)
        {
            using (var trx = _client.BeginTransaction())
            {
                var tbl = _client.OpenTable<TDataType>(_tableId);
                documents.ForEach(doc=> tbl.AddNew(DataDocument.GetDocumentId(doc), doc));
                
                _client.Commit();
            }
        }

        public void Update(TDataType document)
        {
            using (var trx = _client.BeginTransaction())
            {
                var tbl = _client.OpenTable<TDataType>(_tableId);

                
                InternalUpdate(document, tbl);

                _client.Commit();
                
            }
        }

        private static void InternalUpdate(TDataType document, IStructuredDataDictionary<TDataType> tbl)
        {
            var fetchedData = tbl.Fetch(new string[] {DataDocument.GetDocumentId(document)});
            if (!fetchedData.Any())
            {
                throw new InstanceNotFoundException();
            }


            var oldData = fetchedData.First().Data;
            var eTag = fetchedData.First().ETag;
            if (DataDocument.HasEtag<TDataType>())
            {
                eTag = DataDocument.GetDocumentEtag(document);
                if (DataDocument.GetDocumentEtag(oldData) != eTag)
                    throw new DBConcurrencyException();
            }

            tbl.Update(DataDocument.GetDocumentId(document), document, eTag);
        }

        public void BatchUpdate(IList<TDataType> documents)
        {
            using (var trx = _client.BeginTransaction())
            {
                var tbl = _client.OpenTable<TDataType>(_tableId);

                documents.ForEach(doc=>InternalUpdate(doc, tbl));


                _client.Commit();

            }
        }

        public void Delete(TDataType document)
        {
            IdDelete(DataDocument.GetDocumentId(document));
            
        }

        public void ProxyDelete(TSummaryType documentProxy)
        {
            var id = _summarizer.Identify(documentProxy);
            IdDelete(id);
            
        }

        public void IdDelete(string id)
        {
            using (var trx = _client.BeginTransaction())
            {
                var tbl = _client.OpenTable<TDataType>(_tableId);

                tbl.Delete(id);

                _client.Commit();

            }
        }

        public void DeleteBatch(IList<TDataType> documents)
        {
            IdDeleteBatch(documents.Select(DataDocument.GetDocumentId<TDataType>).ToList());

        }

        public void IdDeleteBatch(IList<string> ids)
        {
            using (var trx = _client.BeginTransaction())
            {
                var tbl = _client.OpenTable<TDataType>(_tableId);

                ids.ForEach(tbl.Delete);

                _client.Commit();

            }
        }

        public void ProxyDeleteBatch(IList<TSummaryType> documentProxies)
        {
            IdDeleteBatch(documentProxies.Select(dp => _summarizer.Identify(dp)).ToList());

            
        }

        public TItemPackageType Load(string id)
        {
            using (var trx = _client.BeginTransaction())
            {
                var tbl = _client.OpenTable<TDataType>(_tableId);

                var readData = tbl.Fetch(new string[] {id});
                var retval = new TItemPackageType { Metadata = _itemMetadataSource.Metadata};
                if (readData.Any())
                {
                    retval.Item = readData.First().Data;
                }
                return retval;

            }
        }

        public TItemPackageType Load(TSummaryType documentProxy)
        {
            return Load(_summarizer.Identify(documentProxy));

        }

        public IList<TItemPackageType> Load(IList<string> id)
        {
            using (var trx = _client.BeginTransaction())
            {
                var tbl = _client.OpenTable<TDataType>(_tableId);
                var retval = new List<TItemPackageType>();
                var readData = tbl.Fetch(id.ToArray());
                if (readData.Any())
                {
                    retval.AddRange(readData.Select(rd=>new TItemPackageType{ Metadata = _itemMetadataSource.Metadata, Item = rd.Data}));
                }


                return retval;

            }
        }

        public IList<TItemPackageType> Load(IList<TSummaryType> proxies)
        {
            return Load(proxies.Select(dp => _summarizer.Identify(dp)).ToList());

        }

        public TSummaryPackageType All()
        {
            
            using (var trx = _client.BeginTransaction())
            {
                var tbl = _client.OpenTable<TDataType>(_tableId);
                var allData = tbl.Fetch(tbl.Keys.ToArray()).EmptyIfNull().Select(ra => ra.Data);
                var pk = new TSummaryPackageType
                             {
                                 Bookmark = new GenericPageBookmark {More = false},
                                 Metadata = _summaryMetadataSource.Metadata,
                                 Items = new List<TSummaryType>(allData.Select(doc => _summarizer.Summarize(doc)))
                             };
                return pk;
            }
        }

        public TSummaryPackageType Query(QuerySpecification qs)
        {
            IEnumerable<TDataType> allData;
            using (var trx = _client.BeginTransaction())
            {
                var tbl = _client.OpenTable<TDataType>(_tableId);

                allData = tbl.Fetch(tbl.Keys.ToArray()).EmptyIfNull().Select(ra => ra.Data);
                
            }

            var pk = new TSummaryPackageType();

            var q = allData.AsQueryable();

            q.ApplySpecFilter(qs);
            q = _contextFilter.ApplyContextFilter(q);

            var pbm = new GenericPageBookmark(qs.BookMark);
            q = GenericPaging.Page(q, pbm);

            var items = q.Select(d => _summarizer.Summarize(d)).ToList();
            pk.Bookmark = pbm;
            pk.Items = items;
            pk.Metadata = _summaryMetadataSource.Metadata;


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
    }
}
