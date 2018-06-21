using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModelCommon.RavenDB
{
    using Lok.Unik.ModelCommon.Client;

    using Raven.Client.Indexes;

    public class SchedulePlanByTagFullPath : AbstractIndexCreationTask<SchedulePlan>
    {
        private const string DefaultIndexName = "SchedulePlans/ByTags_FullPath";
        public SchedulePlanByTagFullPath()
        {
            Map =
                schedulePlans =>
                from schedulePlan in schedulePlans
                from tag in schedulePlan.Tags
                select new { Tags_FullPath = tag.FullPath };
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
