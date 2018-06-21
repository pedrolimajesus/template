namespace ModelCommon.RavenDB
{
    public class KioskHashTagCount
    {
        public string Tag { set; get; }

        public int Count { get; set; }

        internal IndexedTag ToIndexedTag()
        {
            return new IndexedTag { Tag = this.Tag, TagCount = this.Count };
        }
    }
}
