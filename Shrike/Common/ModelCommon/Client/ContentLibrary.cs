using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using Lok.Unik.ModelCommon.Interfaces;

namespace Lok.Unik.ModelCommon.Client
{

    public enum State
    {

        Enabled,

        Suspended
    }


    public class ContentLibrary : ITaggableEntity
    {
        public ContentLibrary()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
            Files = new List<ContentItem>();
            InvitedUsers = new List<User>();
            Tags = new List<Tag>();
        }

        [DocumentIdentifier]
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime CreationDate { get; set; }

        public string CreatorPrincipalId { get; set; }
        public string CreatorUserName { get; set; }

        public State State { get; set; }

        public IList<ContentItem> Files { get; set; }

        public IList<User> InvitedUsers { get; set; }

        public string BasePath { get; set; }
        
        #region ITaggableEntity Members

        public IList<Tag> Tags { get; set; }

        #endregion
    }


    public class ContentItemUpload
    {
        public ContentItemUpload()
        {
            VersionId = Guid.NewGuid();
            UploadTime = DateTime.UtcNow;
            TransferInfo = new FileTransferInfo();
            
        }

        public Guid VersionId { get; set; }
        public DateTime UploadTime { get; set; }
        public FileTransferInfo TransferInfo { get; set; }
        public string UploadingUserName { get; set; }
        public string UploadingUserPrincipalId { get; set; }
    }

    public class ContentItem 
    {
        public ContentItem()
        {
            Versions = new List<ContentItemUpload>();
            Id = Guid.NewGuid();
        }

        [DocumentIdentifier]
        public Guid Id { get; set; }

        public Guid LibraryId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public int VersionNumber { get; set; }

        public Guid AssociatedApplication { get; set; }

        public IList<ContentItemUpload> Versions { get; set; } 

        public ContentItemUpload LatestVersion
        {
            get { return Versions.LastOrDefault(); }
        }

    }

}
