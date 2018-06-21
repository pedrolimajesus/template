using Lok.Unik.ModelCommon.Aware;

namespace ModelCommon.RavenDB
{
    using System.Linq;

    using Lok.Unik.ModelCommon.Client;

    using Raven.Client.Indexes;


    public class AllDeviceIndex : AbstractMultiMapIndexCreationTask
    {
        private const string DefaultIndexName = "All/Device";
        public AllDeviceIndex()
        {
            AddMap<Kiosk>(kiosks => from kiosk in kiosks select new { kiosk.Id });
            AddMap<Mobile>(mobiles => from mobile in mobiles select new { mobile.Id });
            AddMap<AwareDevice>(awareDevices => from awareDevice in awareDevices select new { awareDevice.Id });
        }

        public override string IndexName
        {
            get
            {
                return DefaultIndexName;
            }
        }
    }
}