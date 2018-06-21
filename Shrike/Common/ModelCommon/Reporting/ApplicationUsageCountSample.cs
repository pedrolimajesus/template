using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Client;

namespace Lok.Unik.ModelCommon.Reporting
{
    public class ApplicationUsageCountSample : ITaggeable
    {
        

        public string Application { get; set; }
        public string ApplicationId { get; set; }
        
        public int TotalInstances { get; set; }
        public int TotalMinutes { get; set; }
        public int AverageMinutesPerRun { get; set; }
        public int MostLaunchedHour { get; set; }
        

        public Tag ForTag { get; set; }
        public string ReportTagValue { get; set; }
    }


    public class ContentUsageCountSample : ITaggeable
    {
        

        public string ContentPackage { get; set; }
        public string ContentPackageId { get; set; }

        public int TotalInstances { get; set; }
        public int TotalMinutes { get; set; }

        public int AverageMinutesPerRun { get; set; }
        public int MostLaunchedHour { get; set; }

        public Tag ForTag { get; set; }
        public string ReportTagValue { get; set; }
    }

    public class ManagedAppInteractionCountSample : ITaggeable
    {
        

        public string ContentTitle { get; set; }
        public string ContentItemTitle { get; set; }
        public string Group { get; set; }
        public bool Active { get; set; }

        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public int AverageAge { get; set; }
        public int AgeGp0_12 { get; set; }
        public int AgeGp13_19 { get; set; }
        public int AgeGp20_29 { get; set; }
        public int AgeGp30_39 { get; set; }
        public int AgeGp40_49 { get; set; }
        public int AgeGp50_59 { get; set; }
        public int AgeGp60_69 { get; set; }
        public int AgeGp70Up { get; set; }
        
        public int TotalExposureMinutes { get; set; }

        public string MostUsedPlacement { get; set; }

        public Tag ForTag { get; set; }
        public string ReportTagValue { get; set; }
    }

    public class ApplicationDeploymentCountSample : ITaggeable
    {
        

        public string Application { get; set; }
        public string ApplicationId { get; set; }

        public int TotalInstalled{ get; set; }
        public int PercentCoverage { get; set; }
        public int AverageDownloadTimeMinutes { get; set; }
        
        public Tag ForTag { get; set; }
        public string ReportTagValue { get; set; }
    }

    public class ContentDeploymentCountSample : ITaggeable
    {
        

        public string ContentPackage { get; set; }
        public string ContentPackageId { get; set; }

        public int TotalInstalled { get; set; }
        public int PercentCoverage { get; set; }
        public int AverageDownloadTimeMinutes { get; set; }

        public Tag ForTag { get; set; }
        public string ReportTagValue { get; set; }
    }
}
