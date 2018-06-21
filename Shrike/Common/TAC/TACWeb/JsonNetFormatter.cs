using System;
using System.IO;
using System.Net;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AppComponents.Web
{
    using Newtonsoft.Json;

    public class JsonNetFormatter : MediaTypeFormatter
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public JsonNetFormatter(JsonSerializerSettings jsonSerializerSettings)
        {
            _jsonSerializerSettings = jsonSerializerSettings ?? new JsonSerializerSettings();

            // Fill out the mediatype and encoding we support
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
        }

        public override bool CanReadType(Type type)
        {
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            return true;
        }

        public override Task<object> ReadFromStreamAsync(
            Type type, Stream readStream, System.Net.Http.HttpContent content, IFormatterLogger formatterLogger)
        {
            // Create a serializer
            JsonSerializer serializer = JsonSerializer.Create(_jsonSerializerSettings);

            // Create task reading the content
            return Task.Factory.StartNew(
                () =>
                    {
                        using (StreamReader streamReader = new StreamReader(readStream))
                        {
                            using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
                            {
                                return serializer.Deserialize(jsonTextReader, type);
                            }
                        }
                    });

        }

        public override Task WriteToStreamAsync(
            Type type,
            object value,
            Stream writeStream,
            System.Net.Http.HttpContent content,
            TransportContext transportContext)
        {
            // Create a serializer
            JsonSerializer serializer = JsonSerializer.Create(_jsonSerializerSettings);

            // Create task writing the serialized content
            return Task.Factory.StartNew(
                () =>
                    {
                        using (
                            JsonTextWriter jsonTextWriter = new JsonTextWriter(new StreamWriter(writeStream))
                                { CloseOutput = false })
                        {
                            serializer.Serialize(jsonTextWriter, value);
                            jsonTextWriter.Flush();
                        }
                    });

        }

    }
}