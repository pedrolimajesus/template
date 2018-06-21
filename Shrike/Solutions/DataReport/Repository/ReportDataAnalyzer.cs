using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AppComponents;
using AppComponents.Raven;
using DataReport.Base;
using Lok.Unik.ModelCommon.Client;
using Raven.Client;
using Raven.Client.Linq;

namespace DataReport.Repository
{
    /// <summary>
    /// TODO use this as a reusable library
    /// </summary>
    public static class ReportDataAnalyzer
    {
        public static void CreateCompositeReport(ReportLog whal, CancellationToken cancellationToken,
            IDocumentSession ds,Guid subReportId, ReportPeriods subReportPeriod, 
            Action<ReportObject> compositeOperation)
        {
            var qry = from report in ds.Query<ReportLog>()
                      where report.Task.ReportPeriod == subReportPeriod
                            && report.Task.TaskId == subReportId
                            && report.StartingPeriod >= whal.StartingPeriod
                            && report.EndingPeriod <= whal.EndingPeriod
                      orderby report.StartingPeriod
                      select report;

            var compositePartReports = qry.GetAllUnSafe();

            // TODO: doing this all in memory could easily get out of hand ...
            if (compositePartReports.Any())
            {
                var reportStorage = Catalog.Factory.Resolve<IReportDataStorage>();
               
                foreach (var it in compositePartReports)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reportObject = reportStorage.LoadReportData(it);
                    compositeOperation(reportObject);
                }
            }

        }

        public static void CreateCompositeReport<T>(ReportLog whal,  CancellationToken cancellationToken,
            IDocumentSession ds, Guid subReportId, ReportPeriods subReportPeriod)
        {
            var qry = from report in ds.Query<ReportLog>()
                      where report.Task.ReportPeriod == subReportPeriod
                            && report.Task.TaskId == subReportId
                            && report.StartingPeriod >= whal.StartingPeriod
                            && report.EndingPeriod <= whal.EndingPeriod
                      orderby report.StartingPeriod
                      select report;

            var compositePartReports = qry.GetAllUnSafe();

            // TODO: doing this all in memory could easily get out of hand ...
            if (compositePartReports.Any())
            {
                var reportStorage = Catalog.Factory.Resolve<IReportDataStorage>();
                var compositeEvents = new List<T>();
                foreach (var it in compositePartReports)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reportObject = reportStorage.LoadReportData(it);
                    var subReport = reportObject.ReportData as List<T>;
                    compositeEvents.AddRange(subReport);
                }

                cancellationToken.ThrowIfCancellationRequested();

                reportStorage.StoreReportData(whal, compositeEvents);
                whal.DataType = typeof (T);
                ds.Store(whal);
                ds.SaveChanges();
            }


        }

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
    }
}
