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

using System.Collections.Generic;
using System.Linq;

namespace AppComponents.Data
{
    public interface IPageBookmark
    {
        int PageSize { get; set; }
        int CurrentPage { get; set; }
        int LastSkippedResults { get; set; }
        int TotalResults { get; set; }
        int TotalPages { get; }

        bool More { get; set; }
        bool Done { get; }
        void Forward();
    }

    public interface INamedDocument
    {
        string Id { get; set; }
        string Name { get; set; }
    }


    public class NamedRef<T> where T : INamedDocument
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public static implicit operator NamedRef<T>(T doc)
        {
            return new NamedRef<T>
                       {
                           Id = doc.Id,
                           Name = doc.Name
                       };
        }
    }

    public enum SortOrder
    {
        None,
        Ascending,
        Descending
    }

    public enum PredicateJoin
    {
        And,
        Or
    }

    public enum Test
    {
        Equal,
        NotEqual,
        Contains,
        LessThan,
        GreaterThan,
        LessThanEqual,
        GreaterThanEqual
    }

    public class Comparison
    {
        public string Field { get; set; }
        public Test Test { get; set; }
        public object Data { get; set; }
    }

    public class Filter
    {
        public PredicateJoin PredicateJoin { get; set; }
        public IList<Comparison> Rules { get; set; }
    }

    public class QuerySpecification
    {
        public IPageBookmark BookMark { get; set; }
        public string SortOn { get; set; }
        public SortOrder SortOrder { get; set; }

        public Filter Where { get; set; }
    }

    public class DataEnvelope<T, MetadataType>
        where T : class
        where MetadataType : class
    {
        public IPageBookmark Bookmark { get; set; }
        public IList<T> Items { get; set; }
        public MetadataType Metadata { get; set; }
    }

    public class DatumEnvelope<T, MetadataType>
        where T : class
        where MetadataType : class
    {
        public T Item { get; set; }
        public MetadataType Metadata { get; set; }
    }

    public interface ISummarizer<DataType, SummaryType>
        where DataType : class, new()
        where SummaryType : class
    {
        SummaryType Summarize(DataType item);
        string Identify(SummaryType summary);
    }

    public interface IMetadataProvider<DataType, MetadataType>
        where DataType : class
        where MetadataType : class
    {
        MetadataType Metadata { get; }
    }

    public interface IDataQueryService<T, PackageType, MetadataType>
        where T : class
        where MetadataType : class
        where PackageType : DataEnvelope<T, MetadataType>
    {
        PackageType Query(QuerySpecification qs);
        PackageType NamedQuery(string registrationName, string[] arguments, IPageBookmark bookMark);
        PackageType All();
    }

    public interface IRegisteredQueryHandler<T, PackageType, MetadataType>
        where T : class
        where MetadataType : class
        where PackageType : DataEnvelope<T, MetadataType>
    {
        PackageType ExecuteQuery(string[] arguments, IPageBookmark bookMark);
    }

    public interface IContextFilter
    {
        IQueryable<T> ApplyContextFilter<T>(IQueryable<T> query);
        bool InContext<T>(T document);
    }

    public enum DataRepositoryServiceLocalConfig
    {
        ContextResolver,
        SummaryMetadataProvider,
        ItemMetadataProvider,
        Summarizer,
        UpdateAssignment,
        ContextFilter
    }

    public interface IDataRepositoryService<
        TDataType,
        TSummaryType,
        TSummaryPackageType,
        TSummaryMetadataType,
        TItemPackageType,
        TItemMetadataType> : IDataQueryService<TSummaryType, TSummaryPackageType, TSummaryMetadataType>
        where TDataType : class, new()
        where TSummaryType : class
        where TSummaryPackageType : DataEnvelope<TSummaryType, TSummaryMetadataType>, new()
        where TItemPackageType : DatumEnvelope<TDataType, TItemMetadataType>, new()
        where TItemMetadataType : class
        where TSummaryMetadataType : class
    {
        void Store(TDataType document);
        string CreateNew(TDataType document);
        void CreateNewBatch(IList<TDataType> documents);

        void Update(TDataType document);
        void BatchUpdate(IList<TDataType> documents);

        void Delete(TDataType document);
        void ProxyDelete(TSummaryType documentProxy);
        void IdDelete(string id);

        void DeleteBatch(IList<TDataType> documents);
        void IdDeleteBatch(IList<string> ids);
        void ProxyDeleteBatch(IList<TSummaryType> documentProxies);

        TItemPackageType Load(string id);
        TItemPackageType Load(TSummaryType documentProxy);
        IList<TItemPackageType> Load(IList<string> id);
        IList<TItemPackageType> Load(IList<TSummaryType> proxies);
    }

    public static class DataRepositoryServiceFactory
    {
        public static IDataRepositoryService <
            T,
            T,
            DataEnvelope<T,NoMetadata>,
            NoMetadata,
            DatumEnvelope<T,NoMetadata>,
            NoMetadata> CreateSimple<T>() where T: class, new()
        {
            return Catalog.Factory.Resolve <
                   IDataRepositoryService
                       <T, T, DataEnvelope<T, NoMetadata>, NoMetadata, DatumEnvelope<T, NoMetadata>, NoMetadata>>();
        }


    }
}