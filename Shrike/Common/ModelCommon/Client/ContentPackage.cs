using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using Lok.Unik.ModelCommon.Interfaces;

namespace Lok.Unik.ModelCommon.Client
{
    
    public class ContentAssemblyHistory
    {
        public Guid ContentFileId { get; set; }
        public Guid ContentLibraryId { get; set; }
        public string ContentLibraryTitle { get; set; }
        public string ContentFileTitle { get; set; }
        public string ContentFileDescription { get; set; }
        public string ContentFileVersion { get; set; }
    }

    public class ContentPackage : ITaggableEntity
    {
        public ContentPackage()
        {
            Id = Guid.NewGuid();
            TransferInfo = new BitTorrentInfo();
            CreationTime = DateTime.UtcNow;
            AssociateTo = new GroupLink();
            AssemblyHistories = new List<ContentAssemblyHistory>();
            Tags = new List<Tag>();
        }

        public Guid Id { get; set; }

        public string CreatorPrincipalId { get; set; }

        public BitTorrentInfo TransferInfo { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime CreationTime { get; set; }

        public string AssociatedApplication { get; set; }

        public string ConfigurationRelativePath { get; set; }

        public GroupLink AssociateTo { get; set; }

        public IList<ContentAssemblyHistory> AssemblyHistories { get; set; }

        public int DeploymentFileSize { get; set; } 

        #region ITaggableEntity Members

        public IList<Tag> Tags { get; set; }

        #endregion
    }
}
