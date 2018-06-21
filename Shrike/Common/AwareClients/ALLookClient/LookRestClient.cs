using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Lok.AwareLive.Clients.Look
{
    internal class LookRestClient
    {
        private string _device;
        private int _port;
        private string _username;
        private string _password;

        private LookRestClient()
        {
        }

        public LookRestClient(string device, int port, string username, string password)
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

        public HttpStatusCode GetSnapshot(out string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            body = null;
            string uri = OutterURI() + "/snapshots";
            return PerformRestCall(ref body, uri, "POST");
        }

        public HttpStatusCode GetFaces(DateTime start, DateTime end, out string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            body = null;
            string timeStart = TimeParamFormat(start);
            string timeEnd = TimeParamFormat(end);
            string paramValues = string.Format("/data?time={0}-{1}&type=Face&columns=session_id,age,age_confidence,gender,gender_confidence,enter_time,exit_time,time_periods", timeStart, timeEnd);
            string uri = OutterURI() + paramValues;
            return PerformRestCall(ref body, uri, "GET");
        }

        private HttpStatusCode PerformRestCall(ref string body, string uri, string method)
        {
            System.Net.ServicePointManager.CertificatePolicy = new MyPolicy();  // todo: 1) obsolete, 2) this is a global setting.  Shouldn't be here.

            var retval = HttpStatusCode.InternalServerError;
            var req = WebRequest.Create(uri) as HttpWebRequest;
            req.Method = method;
            AddBasicAuthentication(req);

            using (var resp = req.GetResponse() as HttpWebResponse)
            {
                if (resp == null)
                {
                    throw new Exception("Internal Error: no response record returned from 'GetResponse()'.");
                }

                if (resp.StatusCode == HttpStatusCode.Accepted ||
                    resp.StatusCode == HttpStatusCode.OK       ||
                    resp.StatusCode == HttpStatusCode.Created )
                {
                    var reader = new StreamReader(resp.GetResponseStream());
                    body = reader.ReadToEnd();
                    retval = resp.StatusCode;
                }
            }
            return retval;
        }

        private HttpStatusCode PerformRestCall(ref string body, string uri)
        {
            return PerformRestCall(ref body, uri, "GET");
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


        public HttpStatusCode GetFaces(out string body)
        {
            var retval = HttpStatusCode.InternalServerError;
            body = null;
            string uri = OutterURI() + "/data?time=now&type=Face&parameters=primary_gender,majority_gender&columns=id,age,gender";
            Trace.TraceInformation("\nRaw LOOK Query:  {0}", uri);
            return PerformRestCall(ref body, uri);
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
