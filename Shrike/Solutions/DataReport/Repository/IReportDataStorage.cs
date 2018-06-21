
namespace Shrike.Data.Reports.Repository
{
    using Shrike.Data.Reports.Base;

    /// <summary>
    /// 
    /// </summary>
    public interface IReportDataStorage
    {
        /// <summary>
        /// Stores a ReportLog object and its object data into a IFilesContainer container.
        /// The ReportLog object and its data is stored into a ReportObject object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="metadata">ReportLog object to be stored</param>
        /// <param name="dataObject">Reporting data associated to the ReportLog;its data.</param>
        void StoreReportData<T>(ReportLog metadata, T dataObject);
        
        /// <summary>
        /// Returns the ReportObject associated to ReportLog that was saved previously.
        /// </summary>
        /// <param name="metadata">Metadata object used to recover the ReportObject</param>
        /// <returns>ReportObject associated to the metadata</returns>
        ReportObject LoadReportData(ReportLog metadata);
    }
}
