using System.Linq;

namespace ModelCommon.RavenDB
{
    using Lok.Unik.ModelCommon.Client;

    using Raven.Client.Indexes;

    public class ApplicationByTagFullPath : AbstractIndexCreationTask<Application>
    {
        private const string DefaultIndexName = "Applications/ByTags_FullPath";
        public ApplicationByTagFullPath()
        {
            Map =
                applications =>
                from application in applications
                from tag in application.Tags
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
