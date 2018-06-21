namespace Shrike.DAL.Helper
{
    using System.IO;

    using Newtonsoft.Json;

    public static class JsonFileSerializer
    {
        public static void ArchiveObject<T>(T item, string fullFilePath)
        {
            var js = JsonConvert.SerializeObject(item);

            using (var sw = new StreamWriter(fullFilePath))
            {
                sw.Write(JsonFormat(js));
            }
        }

        public static T ExtractObject<T>(string fullFilePath)
        {
            using (var sr = new StreamReader(fullFilePath))
            {
                var js = sr.ReadToEnd();
                var json = JsonFormat(js);
                var item = JsonConvert.DeserializeObject<T>(json);
                return item;
            }
        }

        private static string JsonFormat(string js)
        {
            js = js.Replace("\t", string.Empty);
            js = js.Replace("\n", string.Empty);
            js = js.Replace("\r", string.Empty);
            return js;
        }
    }
}
