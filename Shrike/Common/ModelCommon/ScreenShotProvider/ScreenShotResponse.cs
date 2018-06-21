using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.ScreenShotProvider
{
    public class ScreenShotResponse: ManifestItem
    {
        public ScreenShotResponse()
        {
            ScreenShots = new List<ScreenShotInfo>();

        }

        public Guid DeviceId { get; set; }
        public DateTime TimeTaken { get; set; }
        public IList<ScreenShotInfo> ScreenShots { get; set; } 
    }
}
