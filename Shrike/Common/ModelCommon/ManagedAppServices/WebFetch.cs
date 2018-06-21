using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.ManagedAppServices
{
    public enum WebFetchType
    {
        FetchWebPageImage,
        FetchMediaFile
    }

    public class WebFetchItem
    {
        public Guid Identifier { get; set; }
        public WebFetchType FetchType { get; set; }
        public string Url { get; set; }
    }

    public class WebFetchRequest
    {
        public WebFetchRequest()
        {
            RequestItems = new List<WebFetchItem>();
        }

        public IList<WebFetchItem> RequestItems { get; set; } 
    }

    public class WebFetchResponseItem
    {
        public Guid Identifier { get; set; }
        public bool Fetched { get; set; }
        public byte[] Data { get; set; }
    }

    public class WebFetchResponse
    {
        public WebFetchResponse()
        {
            ResponseItems = new List<WebFetchResponseItem>();

        }

        public IList<WebFetchResponseItem> ResponseItems { get; set; } 
    }
}
