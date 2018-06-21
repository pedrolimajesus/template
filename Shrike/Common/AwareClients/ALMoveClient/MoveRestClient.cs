using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Lok.AwareLive.Clients.Move
{
    internal class MoveRestClient
    {
        private string _device;
        private int _port;
        private string _username;
        private string _password;

        private MoveRestClient()
        {
        }

        public MoveRestClient(string device, int port, string username, string password)
        {
            _device = device;
            _port = port;
            _username = username;
            _password = password;
        }

        public HttpStatusCode GetState(out string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            body = null;
            string uri = OutterURI() + "/state";
            return PerformRestCall(ref body, uri);
        }

        public HttpStatusCode GetHotspotEvents(DateTime start, DateTime end, out string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            body = null;
            string uri = OutterURI() + InnerHotspotEventsURI(start, end);
            return PerformRestCall(ref body, uri);
        }

        public HttpStatusCode GetBorderEvents(DateTime start, DateTime end, out string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            body = null;
            string uri = OutterURI() + InnerBorderEventsURI(start, end);
            return PerformRestCall(ref body, uri);
        }

        public HttpStatusCode GetAllEvents(DateTime start, DateTime end, out string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            body = null;
            string uri = OutterURI() + InnerEventsURI(start, end);
            return PerformRestCall(ref body, uri);
        }

        public HttpStatusCode GetListOfBorders(out string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            body = null;
            string uri = OutterURI() + "/borders_api/borders/active";
            return PerformRestCall(ref body, uri);
        }

        public HttpStatusCode GetListOfHotspots(out string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            body = null;
            string uri = OutterURI() + "/hotspots_api/hotspots/active/descriptions";
            return PerformRestCall(ref body, uri);
        }

        public HttpStatusCode GetLayout(out string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            body = null;
            string uri = OutterURI() + "/layout";
            return PerformRestCall(ref body, uri);
        }

        private string InnerHotspotEventsURI(DateTime start, DateTime end)
        {
            string startTime = TimeParamFormat(start);
            string endTime = TimeParamFormat(end);
            return string.Format("/data?info=area_events&time={0}-{1}", startTime, endTime);
        }

        private string InnerBorderEventsURI(DateTime start, DateTime end)
        {
            string startTime = TimeParamFormat(start);
            string endTime = TimeParamFormat(end);
            return string.Format("/data?info=line_events&time={0}-{1}", startTime, endTime);
        }

        private string InnerEventsURI(DateTime start, DateTime end)
        {
            string startTime = TimeParamFormat(start);
            string endTime = TimeParamFormat(end);
            return string.Format("/data?info=area_events,line_events&time={0}-{1}", startTime, endTime);
        }

        private HttpStatusCode PerformRestCall(ref string body, string uri)
        {
            System.Net.ServicePointManager.CertificatePolicy = new MyPolicy();  // todo: 1) obsolete, 2) this is a global setting.  Shouldn't be here.

            var retval = HttpStatusCode.InternalServerError;
            var req = WebRequest.Create(uri) as HttpWebRequest;
            AddBasicAuthentication(req);

            using (var resp = req.GetResponse() as HttpWebResponse)
            {
                if (resp == null)
                {
                    throw new Exception("Internal Error: no response record returned from 'GetResponse()'.");
                }

                if (resp.StatusCode == HttpStatusCode.Accepted ||
                    resp.StatusCode == HttpStatusCode.OK)
                {
                    var reader = new StreamReader(resp.GetResponseStream());
                    body = reader.ReadToEnd();
                    retval = resp.StatusCode;
                }
            }
            return retval;
        }

        private void AddBasicAuthentication(HttpWebRequest req)
        {
            string authInfo = string.Format("{0}:{1}", _username, _password);
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes((authInfo)));
            req.Headers["Authorization"] = "Basic " + authInfo;
        }


        private string OutterURI()
        {
            string uri = string.Format("https://{0}:{1}", _device, _port);
            return uri;
        }

        private string TimeParamFormat(DateTime dt)
        {
            //var dtInUTC = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            var dtInUTC = dt.ToUniversalTime();
            string value = string.Format("{0}-{1}-{2}T{3}:{4}:{5}",
                dtInUTC.Date.Year, dtInUTC.Month.ToString("00"), dtInUTC.Day.ToString("00"),
                dtInUTC.Hour.ToString("00"), dtInUTC.Minute.ToString("00"), dtInUTC.Second.ToString("00"));
            return value;
        }

        public HttpStatusCode SetAreasSubscription(string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            //body = null;
            string uri = OutterURI() + "/hotspots_api/subscribers";
            return PerformPutRestCallSendingBody(body, uri);
        }

        public HttpStatusCode SetLinesSubscription(string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            //body = null;
            string uri = OutterURI() + "/borders_api/subscribers";
            return PerformPutRestCallSendingBody(body, uri);
        }

        private HttpStatusCode PerformPutRestCallSendingBody(string body, string uri)
        {
            return RestCallSendingBody(body, uri, "PUT");
        }

        private HttpStatusCode PerformPostRestCallSendingBody(string body, string uri)
        {
            return RestCallSendingBody(body, uri, "POST");
        }

        /// <summary>
        /// Perform a REST call as a client
        /// </summary>
        /// <param name="body"></param>
        /// <param name="uri"></param>
        /// <param name="restVerb">must be GET, PUT, POST, or DELETE</param>
        /// <returns></returns>
        private HttpStatusCode RestCallSendingBody(string body, string uri, string restVerb)
        {
            System.Net.ServicePointManager.CertificatePolicy = new MyPolicy();  // todo: 1) obsolete, 2) this is a global setting.  Shouldn't be here.

            var retval = HttpStatusCode.InternalServerError;
            var req = WebRequest.Create(uri) as HttpWebRequest;
            req.ContentType = "text/json";
            req.Method = restVerb;
            AddBasicAuthentication(req);

            using (var streamWriter = new StreamWriter(req.GetRequestStream()))
            {
                streamWriter.Write(body);
                streamWriter.Flush();
                streamWriter.Close();
            }

            using (var resp = req.GetResponse() as HttpWebResponse)
            {
                if (resp == null)
                {
                    throw new Exception("Internal Error: no response record returned from 'GetResponse()'.");
                }

                retval = resp.StatusCode;
            }
            return retval;
        }

        public HttpStatusCode SetState(string body)
        {
            System.Net.ServicePointManager.CertificatePolicy = new MyPolicy();  // todo: 1) obsolete, 2) this is a global setting.  Shouldn't be here.
            string uri = OutterURI() + "/state";
            var retval = HttpStatusCode.InternalServerError;
            var req = WebRequest.Create(uri) as HttpWebRequest;
            req.ContentType = "text/json";
            req.Method = "PUT";
            AddBasicAuthentication(req);

            using (var streamWriter = new StreamWriter(req.GetRequestStream()))
            {
                streamWriter.Write(body);
                streamWriter.Flush();
                streamWriter.Close();
            }

            using (var resp = req.GetResponse() as HttpWebResponse)
            {
                if (resp == null)
                {
                    throw new Exception("Internal Error: no response record returned from 'GetResponse()'.");
                }

                retval = resp.StatusCode;
            }
            return retval;
        }

        public HttpStatusCode AddAreasSubscription(string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            //body = null;
            string uri = OutterURI() + "/hotspots_api/subscribers";
            return PerformPostRestCallSendingBody(body, uri);
        }

        public HttpStatusCode AddLinesSubscription(string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            //body = null;
            string uri = OutterURI() + "/borders_api/subscribers";
            return PerformPostRestCallSendingBody(body, uri);
        }
    }



    internal class MyPolicy : ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem)
        {
            return true; // Return true to force the certificate to be accepted. 
        }
    }
}
