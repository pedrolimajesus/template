using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Lok.Unik.ModelCommon
{
    using System;

    public class ManifestRequest
    {
        public ManifestRequest()
        {
            Items = new List<ManifestItem>();
            CreationDate = DateTime.UtcNow;
        }

        public int SyncNumber { get; set; }
        public Int64 RequestId { get; set; }
        public string DeviceId { get; set; }
        public DateTime CreationDate { get; set; }

        public List<ManifestItem> Items { get; set; }
    }

    public class ManifestResponse
    {
        public Int64 RequestId { get; set; }

        public List<ManifestItem> Items { get; set; }
    }
}
