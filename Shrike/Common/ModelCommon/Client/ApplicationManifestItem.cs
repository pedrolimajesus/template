using System;
using System.Collections.Generic;

namespace Lok.Unik.ModelCommon.Client
{
    using Raven.Imports.Newtonsoft.Json;
    using Raven.Imports.Newtonsoft.Json.Converters;

    public class DeploymentDeviceInfo
    {
        public Guid Identifier { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Model { get; set; }

        public IList<Tag> Tags { get; set; }
    }

    public class DeploymentManifestItem : ManifestItem
    {
        
        public IList<Application> Applications { get; set; }

        public IList<ContentPackage> ContentPackages { get; set; }

        public SchedulePlan SchedulePlan { get; set; }

        public DeploymentDeviceInfo DeviceInfo { get; set; }
    }

    public enum DeploymentStates
    {
        Downloading,
        Installing,
        Installed,
        Uninstalling,
        Uninstalled,
        CannotDownload,
        CannotInstall
    }

    public class ApplicationDeployment
    {
        public Application Application { get; set; }

        public string ContainerFolder { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public DeploymentStates DeploymentState { get; set; }
        public DateTime InstallationTime { get; set; }
        public TimeSpan DownloadDuration { get; set; }
        public string DeployedFilePath { get; set; }
        public bool FailedPendingRetry { get; set; }
        public int Retries { get; set; }
    }

    public class ContentPackageDeployment
    {
        public ContentPackage ContentPackage { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public DeploymentStates DeploymentState { get; set; }
        public DateTime InstallationTime { get; set; }
        public TimeSpan DownloadDuration { get; set; }
        public string DeployedFilePath { get; set; }
        public bool FailedPendingRetry { get; set; }
        public int Retries { get; set; }
    }   

    public class DeploymentState
    {
        public DeploymentState()
        {
            ApplicationDeployments = new List<ApplicationDeployment>();
            ContentPackageDeployments = new List<ContentPackageDeployment>();
            DeviceTags = new List<Tag>();
        }
        public Guid DeviceId { get; set; }
        public Guid OperatingSchedulePlanId { get; set; }
        public DateTime SnapshotTime { get; set; }
        public IList<ApplicationDeployment> ApplicationDeployments { get; set; }
        public IList<ContentPackageDeployment> ContentPackageDeployments { get; set; } 
        public IList<Tag> DeviceTags { get; set; }
    }

    public class DeploymentManifestResponseItem : ManifestItem
    {
        public DeploymentState DeploymentState { get; set; }
    }
}