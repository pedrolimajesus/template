using Lok.Unik.ModelCommon.Manifest;

namespace Lok.Unik.ModelCommon
{
    using System.Runtime.Serialization;

    using Lok.Unik.ModelCommon.Client;
    using Lok.Unik.ModelCommon.Inventory;
    using System;

    //[XmlInclude(typeof(ApplicationManifestItem))]
    //[XmlInclude(typeof(BitTorrentInfo))]
    //[XmlInclude(typeof(CompressedContentFile))]
    [KnownType(typeof(DeploymentManifestItem))]
    [KnownType(typeof(Application))]
    [KnownType(typeof(BitTorrentInfo))]
    [KnownType(typeof(InventoryPolicy))]
    [KnownType(typeof(InventoryResponse))]
    [KnownType(typeof(ManagedAppEventManifestItem))]
    [KnownType(typeof(ManagedAppEventManifestResponse))]
    public class ManifestItem
    {
        public Int64 RequestId { get; set; }
    }
}
