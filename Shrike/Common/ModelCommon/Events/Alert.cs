using System;
using System.Collections.Generic;

using AppComponents;

using AppComponents.Extensions.EnumEx;

using Lok.Unik.ModelCommon.Client;
using Lok.Unik.ModelCommon.Interfaces;

namespace Lok.Unik.ModelCommon.Events
{
    using System.ComponentModel;

    public enum AlertStatus
    {
        Unassigned,

        Open,

        Closed
    }

    public class CommentPost
    {
        
        [DisplayName("Date")]
        public DateTime TimePosted { get; set; }

        [DisplayName("Comment")]
        public string Comment { get; set; }

        [DisplayName("UserId")]
        public Guid PostingUserId { get; set; }

        [DisplayName("User")]
        public string PostingUserName { get; set; }

        public Tuple<AlertStatus, AlertStatus> StatusChange { get; set; }

        public Tuple<Guid, Guid> AssigneeChange { get; set; }
    }

    public enum AlertCategory
    {
        Operations = 1,
        Default = 12000,
        Content = 13000,
        Devices = 50000,
        None = -1
    }

    public enum AlertKinds
    {
        //Operations managers
        YellowCategory = 5000,

        CheckPaperReminder,

        CheckMaintenanceReminder,

        NoInteractions,

        CPUUseHigh,

        MemoryUseHigh,

        RedCategory = 10000,

        DeviceCategory = 11000,

        OutOfPaper,

        DeviceUnreachable,

        DiskSpaceLow,

        DevicesNotWorking,

        MemoryUseCritical,

        //Default roles
        ApplicationCategory = 12000,

        ApplicationStartFailed,

        ApplicationContentMissing,

        ApplicationContentBad,

        ApplicationNotDeployed,

        //Content managers
        DeploymentCategory = 13000,

        UnavailableContentScheduled,

        DisabledContentScheduled,

        DeploymentLongRunning,

        ContentLoadProblem
    }

    public class Alert : ITaggableEntity
    {
        public Alert()
        {
            Comments = new List<CommentPost>();
        }

        public Guid Id
        {
            get
            {
                return Identifier;
            }
            set
            {
                Identifier = value;
            }
        }

        [DocumentIdentifier]
        public Guid Identifier { get; set; }

        public DateTime TimeGenerated { get; set; }

        public HealthStatus AlertHealthLevel { get; set; }

        public string AlertTitle { get; set; }

        public string Message { get; set; }

        public Guid AssignedUserId { get; set; }

        public string AssignedUserName { get; set; }

        public Guid RelatedDevice { get; set; }

        public string RelatedDeviceName { get; set; }
        
        public string RelatedDeviceModel { get; set; }

        public AlertStatus Status { get; set; }

        public AlertKinds Kind { get; set; }

        public DateTime TimeStatusChanged { get; set; }

        public IList<CommentPost> Comments { get; set; }

        public int MatchHash { get; set; }

        #region ITaggableEntity

        public IList<Tag> Tags { get; set; }

        #endregion

        public int CreateMatchHash()
        {
            return Hash.GetCombinedHashCode(RelatedDevice.ToString(), Kind.EnumName(), AlertHealthLevel.EnumName());
        }

    }
}