namespace Shrike.Data.Reports.Base
{
    using System;
    using System.Collections.Generic;

    using AppComponents;

    

    public class WarehouseTaskDescription
    {
        public WarehouseTaskDescription()
        {
            this.ReportPresentations = new List<ReportPresentationInfo>();

        }

        [DocumentIdentifier]
        public Guid TaskId { get; set; }

        public string Provider { get; set; }
        public string Description { get; set; }
        public ReportPeriods ReportPeriod { get; set; }
        public bool ByTenant { get; set; }
        public IList<ReportPresentationInfo> ReportPresentations { get; set; }
        public string ReportTemplateId { get; set; }
        public bool UserDefined { get; set; }
        public string OnSpecifiedTenant { get; set; }
    }
}
