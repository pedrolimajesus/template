namespace ModelCommon.RavenDB
{
    using System.Linq;

    using Lok.Unik.ModelCommon.Client;

    using Raven.Abstractions.Indexing;
    using Raven.Client.Indexes;

    public class KioskTagIndex : AbstractIndexCreationTask<Kiosk, KioskHashTagCount>
    {
        public KioskTagIndex()
        {
            this.Map = kiosks => from kiosk in kiosks from tag in kiosk.Tags select new { Tag = tag.FullPath, Count = 1 };
            this.Reduce =
                results =>
                from result in results
                group result by result.Tag
                into g select new { Tag = g.Key, Count = g.Sum(t => t.Count) };
            this.Sort(result => result.Count, SortOptions.Int);
        }
    }
}