using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Inventory
{
    public class InventoryResponse : ManifestItem
    {
        public InventoryResponse()
        {
            OSInfo = new OSInfo();
        }

        public OSInfo OSInfo { get; set; }
        public string DeviceId { get; set; }
    }
}
