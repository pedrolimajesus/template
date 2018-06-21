namespace Shrike.Data.Reports.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AppComponents;
    using AppComponents.Extensions.EnumerableEx;
    using AppComponents.ControlFlow;
    using AppComponents.Raven;

    using ModelCommon.RavenDB;

    using Lok.Unik.ModelCommon.Client;
    using Lok.Unik.ModelCommon.Events;
    using Lok.Unik.ModelCommon.Reporting;

    using AppComponents.Data;

    using Shrike.Data.Reports.Base;

    [NamedContext("UnikTenant")]
    public class ReportDataProvider
    {
        private static ReportDataProvider singleton;

        public static ReportDataProvider GetInstance()
        {
           singleton = singleton??new ReportDataProvider();
            return singleton;
        }

        protected ReportDataProvider()
        {
        }

        /// <summary>
        /// Creates a list of T objects containing the data associated to a ReportLog object 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reportLog">ReportLog Object from which the data will be recovered</param>
        /// <param name="tagFilter">Tag object to be used as filter.</param>
        /// <param name="tagCategoryColumn">Catagory of the tags that will re recovered</param>
        /// <param name="assignTag">Function that assign a Tag to the ReportLog object</param>
        /// <param name="getTags">Function that returns the tags of the T object</param>
        /// <returns></returns>
        private IEnumerable<T> FetchReportData<T>(ReportLog reportLog, Tag tagFilter, string tagCategoryColumn,
            Action<T, string> assignTag, Func<T, IList<Tag>> getTags)
        {
            var rs = Catalog.Factory.Resolve<IReportDataStorage>();
            var reportObject = rs.LoadReportData(reportLog);
            var data = reportObject.ReportData as List<T>;

            if (null == data)
                throw new InvalidOperationException(string.Format("Report data for report {0} is unexpectedly {1}", reportLog.Id, reportObject.ReportData.GetType()));

            if (null != tagCategoryColumn)
            {
                ExtractTagValueToColumn(data, tagCategoryColumn, assignTag, getTags);
            }

            if (null == tagFilter)
                return data;

            return from it in data
                       where getTags(it).Any(tg => tg.Category.Name == tagFilter.Category.Name && tg.Value == tagFilter.Value)
                       select it;

        }

        /// <summary>
        /// Updates the tag of all T object 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="originalData"></param>
        /// <param name="tagCategory"></param>
        /// <param name="setColumnFunc"></param>
        /// <param name="fetchTagsFunc"></param>
        public static void ExtractTagValueToColumn<T>(IEnumerable<T> originalData, string tagCategory,
            Action<T, string> setColumnFunc, Func<T, IList<Tag>> fetchTagsFunc)
        {
            foreach (var it in originalData)
            {
                var tags = fetchTagsFunc(it);
                var val = tags.FirstOrDefault(tag => tag.Category.Name == tagCategory);
                if (null != val)
                    setColumnFunc(it, val.Value);
            }
        }

       

        /// <summary>
        /// Returns an IEnumerable of T object that implement ITaggeableEvent 
        /// </summary>
        /// <typeparam name="T">T class must implement the ItaggeableEvent interface</typeparam>
        /// <param name="reportLog">Report Object from which data will be recovered</param>
        /// <param name="tagFilter">Tag object to be used as filter</param>
        /// <param name="tagCategoryColumn">String containing the tag category filter</param>
        /// <returns></returns>
        public IEnumerable<T> GetReportData<T>(ReportLog reportLog,
                                               Tag tagFilter = null,
                                               string tagCategoryColumn = null) where T : ITaggeableEvent
        {
            return this.FetchReportData<T>(reportLog, tagFilter, tagCategoryColumn,
                                                               (@event, s) => @event.ReportTagValue = s,
                                                               @event => @event.DeviceTags);
        }
        
        
        

       
        public IEnumerable<ReportLog> GetAdminReports()
        {
                var querySpecification = new QuerySpecification()
                {
                    BookMark = new RavenPageBookmark(100),
                    SortOrder = AppComponents.Data.SortOrder.Descending,
                    SortOn = "EndingPeriod",
                    Where = new Filter { Rules = new Comparison [] 
                                                    {  new Comparison { 
                                                            Data = ReportPeriods.Dashboard,
                                                            Field = "Task.ReportPeriod",
                                                            Test = Test.NotEqual
                                                            }
                                                    }
                                    }
                };

                IEnumerable<ReportLog> list = GetFromRepository<ReportLog>(querySpecification);
                
                return list;
               
        }

        public static void DeleteInRepository<T>(T document) where T : class, new()
        {
            using (var ctx = ContextRegistry.NamedContextsFor(
                             ContextRegistry.CreateNamed(ContextRegistry.Kind,
                             UnikContextTypes.UnikWarehouseContextResourceKind)))
            {
                var repository = ReportDataProvider.OpenCoreRepository<T>();
                repository.Delete(document);
                
            }
        }

        public static void DeleteBatchInRepository<T>(List<T> list) where T : class, new()
        {
            using (var ctx = ContextRegistry.NamedContextsFor(
                             ContextRegistry.CreateNamed(ContextRegistry.Kind,
                             UnikContextTypes.UnikWarehouseContextResourceKind)))
            {
                var repository = ReportDataProvider.OpenCoreRepository<T>();
                repository.DeleteBatch(list);

            }
        }

        public static void StoreInRepository<T>(T document) where T : class, new()
        {
            using (var ctx = ContextRegistry.NamedContextsFor(
                             ContextRegistry.CreateNamed(ContextRegistry.Kind,
                             UnikContextTypes.UnikWarehouseContextResourceKind)))
            {
                var repository = ReportDataProvider.OpenCoreRepository<T>();
                repository.Store(document);
            }
        }


        public static IEnumerable<T> GetFromRepository<T>(QuerySpecification querySpecification = null) where T : class, new()
        {
            using (var ctx = ContextRegistry.NamedContextsFor(
                             ContextRegistry.CreateNamed(ContextRegistry.Kind,
                             UnikContextTypes.UnikWarehouseContextResourceKind)))
            {
                List<T> reports = new List<T>();
                var repository = ReportDataProvider.OpenCoreRepository<T>();

                Func<QuerySpecification, DataEnvelope<T, NoMetadata>> function = null;
                if (querySpecification == null)
                {
                    function = qe => repository.All();
                }
                else 
                {
                    function = qe => repository.Query(qe);
                }

                bool loop = true;
                while (loop)
                {
                    var qr = function(querySpecification);
                    if (qr.Items.Any())
                    {
                        reports.AddRange(qr.Items.ToArray());
                    }
                    qr.Bookmark.Forward();
                    loop = qr.Bookmark.More;
                }

                return reports;
            }
        }


        public static
            IDataRepositoryService<T, T, DataEnvelope<T, NoMetadata>, NoMetadata, DatumEnvelope<T, NoMetadata>, NoMetadata>
            OpenCoreRepository<T>(IContextFilter contextFilter = null) where T : class, new()
        {

            return Catalog.Preconfigure()
                .Add(DataRepositoryServiceLocalConfig.SummaryMetadataProvider, new NoMetadataProvider<T>())
                .Add(DataRepositoryServiceLocalConfig.ItemMetadataProvider, new NoMetadataProvider<T>())
                .Add(DataRepositoryServiceLocalConfig.Summarizer, new IdentitySummarizer<T, T>())
                //.Add(DataRepositoryServiceLocalConfig.UpdateAssignment, new Updater<T>(t => t.Properties()))
                .Add(DataRepositoryServiceLocalConfig.UpdateAssignment, new Action<T, T>( (t1, t2) => Console.Write("Strings [{0}] [{1}]", t1.ToString(), t2.ToString()) ) )
                .ConfiguredResolve
                <IDataRepositoryService
                        <T, T, DataEnvelope<T, NoMetadata>,
                        NoMetadata,
                        DatumEnvelope<T, NoMetadata>,
                        NoMetadata>
                        >();
        }


        #region

        /// <summary>
        ///  Returns a IEnumerable of T object that implements from ITaggeable
        /// </summary>
        /// <typeparam name="T">T class must be a ITaggeable class</typeparam>
        /// <param name="reportLog">ReportLog object from which data will be recovered.</param>
        /// <param name="tagFilter">Tag object filter</param>
        /// <param name="tagCategoryColumn">String the tag category to be used as filter.</param>
        /// <returns></returns>
        public IEnumerable<T> GetCountData<T>(ReportLog reportLog,
                                      Tag tagFilter = null,
                                      string tagCategoryColumn = null) where T : ITaggeable
        {
            return this.FetchReportData<T>(reportLog, tagFilter, tagCategoryColumn,
                                       (sample, s) => sample.ReportTagValue = s,
                                       sample => (sample.ForTag == null) ? Enumerable.Empty<Tag>().ToList() : EnumerableEx.OfOne(sample.ForTag).ToList());
        }

        
        
        #endregion

    }
}
