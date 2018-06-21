namespace Shrike.Data.Reports.Repository
{
    using Shrike.Data.Reports.Base;

    /// <summary>
    /// Container that will store ReportLog and its ReportData object(s)
    /// </summary>
    public class ReportObject
    {
        /// <summary>
        /// ReportLog that will be stored
        /// </summary>
        public ReportLog Metadata { get; set; }
        
        /// <summary>
        ///  Object that will contain the associated data to the ReportLog.
        /// </summary>
        public object ReportData { get; set; }
    }
}
