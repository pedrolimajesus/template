using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Services.Client;
using System.Linq;
using System.Management.Instrumentation;
using AppComponents.Extensions.EnumerableEx;
using System.Text;
using AppComponents.Extensions.QuerySpecificationEx;

namespace AppComponents.Data
{

    public enum InMemoryDataRepositoryServiceLocalConfig
    {
        OptionalInitialData

    }

    public class InMemoryDataRepositoryService<
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
        
        private static ConcurrentDictionary<string, TDataType> _data = new ConcurrentDictionary<string, TDataType>(); 

        public InMemoryDataRepositoryService()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);

            _summaryMetadataSource =
                config.Get<IMetadataProvider<TSummaryType, TSummaryMetadataType>>(
                    DataRepositoryServiceLocalConfig.SummaryMetadataProvider);
            _itemMetadataSource =
                config.Get<IMetadataProvider<TDataType, TItemMetadataType>>(
                    DataRepositoryServiceLocalConfig.ItemMetadataProvider);
            _summarizer = config.Get<ISummarizer<TDataType,TSummaryType>>(DataRepositoryServiceLocalConfig.Summarizer);
            
            if (config.SettingExists(DataRepositoryServiceLocalConfig.ContextFilter))
                _contextFilter = config.Get<IContextFilter>(DataRepositoryServiceLocalConfig.ContextFilter);

            IEnumerable<TDataType> initialData = null;
            if (config.SettingExists(InMemoryDataRepositoryServiceLocalConfig.OptionalInitialData))
                initialData = config.Get<IEnumerable<TDataType>>(InMemoryDataRepositoryServiceLocalConfig.OptionalInitialData);

            if (null != initialData)
            {
                initialData.ForEach(d=>_data.TryAdd( DataDocument.GetDocumentId(d), d)); 
            }
           

        }

        public void Store(TDataType document)
        {
            var key = DataDocument.GetDocumentId(document);
            if (!_data.ContainsKey(key))
                _data.TryAdd(key, document);
            else
                _data[DataDocument.GetDocumentId(document)] = document;
        }

        public string CreateNew(TDataType document)
        {
            _data.TryAdd(DataDocument.GetDocumentId(document), document);
            return DataDocument.GetDocumentId(document);
        }

        public void CreateNewBatch(IList<TDataType> documents)
        {
            documents.ForEach(doc => CreateNew(doc));
        }

        public void Update(TDataType document)
        {
            TDataType oldData;
            if (!_data.TryGetValue(DataDocument.GetDocumentId(document), out oldData))
            {
                throw new InstanceNotFoundException();
            }

            if (DataDocument.HasEtag<TDataType>())
            {

                
                if (DataDocument.GetDocumentEtag(oldData) != DataDocument.GetDocumentEtag(document))
                    throw new DBConcurrencyException();
            }

            DataDocument.SetDocumentEtagNew(document);
            _data[DataDocument.GetDocumentId(document)] = document;
        }

        public void BatchUpdate(IList<TDataType> documents)
        {
            documents.ForEach(doc=> Update(doc));
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
            TDataType _;
            _data.TryRemove(id, out _);
        }

        public void DeleteBatch(IList<TDataType> documents)
        {
            documents.ForEach(doc=>Delete(doc));
        }

        public void IdDeleteBatch(IList<string> ids)
        {
            ids.ForEach(id=>IdDelete(id));
        }

        public void ProxyDeleteBatch(IList<TSummaryType> documentProxies)
        {
            documentProxies.ForEach(summary=>ProxyDelete(summary));
        }

        public TItemPackageType Load(string id)
        {
            var pk = new TItemPackageType {Metadata = _itemMetadataSource.Metadata};
            TDataType dt = null;
            if (_data.TryGetValue(id, out dt))
            {
                pk.Item = dt;
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
            var items = from dt in _data.Values where id.Contains(DataDocument.GetDocumentId(dt)) select dt;
            retval.AddRange(
                    items.Select(i => new TItemPackageType { Item = i, Metadata = _itemMetadataSource.Metadata }));

            return retval;
        }

        public IList<TItemPackageType> Load(IList<TSummaryType> proxies)
        {
            var retval = new List<TItemPackageType>();

            var ids = from p in proxies select _summarizer.Identify(p);
            var items = from dt in _data.Values where ids.Contains(DataDocument.GetDocumentId(dt)) select dt; 
            retval.AddRange(
                items.Select(i => new TItemPackageType { Item = i, Metadata = _itemMetadataSource.Metadata }));
            

            return retval;
        }

        public TSummaryPackageType All()
        {
            var pk = new TSummaryPackageType
                         {
                             Bookmark = new GenericPageBookmark {More = false},
                             Metadata = _summaryMetadataSource.Metadata,
                             Items = new List<TSummaryType>(_data.Values.Select(doc => _summarizer.Summarize(doc)))
                         };
            return pk;
        }

        public TSummaryPackageType Query(QuerySpecification qs)
        {
            var pk = new TSummaryPackageType();

            var q = _data.Values.AsQueryable();

            q.ApplySpecFilter(qs);
            q = _contextFilter.ApplyContextFilter(q);

            var pbm = new GenericPageBookmark(qs.BookMark);
            q = GenericPaging.Page(q, pbm);

            var items = q.Select(d => _summarizer.Summarize(d)).ToList();
            pbm.Forward();
            pbm.More = items.Any();

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
