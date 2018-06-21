namespace Shrike.Data.Reports.Base
{
    using System;

    /// <summary>
    /// Object that contains all needed information for Reports
    /// </summary>
    public class ReportLog
    {
        public string Id { get; set; }
        public DateTime TimeCreated { get; set; }
        public DateTime StartingPeriod { get; set; }
        public DateTime EndingPeriod { get; set; }
        public string TenantRoute { get; set; }
        public WarehouseTaskDescription Task { get; set; }
        public Type DataType { get; set; }
        public bool UserDefined { get; set; }
    }
}
