using System.Linq;

namespace ModelCommon.RavenDB
{
    using Lok.Unik.ModelCommon.Client;

    using Raven.Client.Indexes;

    public class TaggableIndex : AbstractMultiMapIndexCreationTask
    {
        public const string DefaultIndexName = "All/ITaggable";

        public TaggableIndex()
        {
            AddMap<Kiosk>(kiosks => from kiosk in kiosks select new { kiosk.Id, kiosk.Tags });
            AddMap<Mobile>(mobiles => from mobile in mobiles select new { mobile.Id, mobile.Tags });
            AddMap<SchedulePlan>(schedulePlans => from schedulePlan in schedulePlans select new { schedulePlan.Id, schedulePlan.Tags });
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
