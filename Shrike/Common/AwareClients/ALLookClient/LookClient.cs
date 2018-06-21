using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lok.AwareLive.Clients.Look.Model;

namespace Lok.AwareLive.Clients.Look
{
    public class LookClient : ILookClient
    {
        private string _device;
        private int _port = 8182;
        private string _username;
        private string _password;
        private const int _DATA_PREVIEW_SIZE = 1400;

        public void Bind(string device, string username, string password)
        {
            _device = device;
            _username = username;
            _password = password;
        }

        public System.Net.HttpStatusCode GetState(out Model.StateRec rec)
        {
            ValidateIsBound();
            
            LookRestClient client = new LookRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetState(out body);
            Trace.TraceInformation("\nRaw JSON data:  {0}", body);

            rec = JsonHelper.JsonToStateRec(body);
            return responseCode;
        }

        public System.Net.HttpStatusCode GetSnapshot(out Model.SnapshotRec rec)
        {
            ValidateIsBound();

            var client = new LookRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetSnapshot(out body);
            Trace.TraceInformation("\nRaw JSON data:  {0}", body);

            rec = JsonHelper.JsonToSnapshotRec(body);
            if (rec != null)
            {
                // Build full path
                rec.FullPath = string.Format("https://{0}:{1}/{2}", _device, _port, rec.Path);
            }
            return responseCode;
        }

        private void ValidateIsBound()
        {
            if (string.IsNullOrEmpty(_device) ||
                string.IsNullOrEmpty(_username) ||
                string.IsNullOrEmpty(_password))
            {
                throw new Exception("Attempting to use Move REST call without first binding to a target device");
            }
        }

        public HttpStatusCode GetFacesNow(out FacesRec faces)
        {
            ValidateIsBound();

            var client = new LookRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetFaces(out body);
            Trace.TraceInformation("\nRaw JSON data:  {0}", body);

            if (responseCode == HttpStatusCode.OK)
            {
                faces = JsonHelper.JsonToFacesRec(body);                
            }
            else
            {
                faces = null;
            }

            return responseCode;
        }

        public HttpStatusCode GetFaces(DateTime start, DateTime end, out FacesHistoricRec faces)
        {
            ValidateIsBound();

            var client = new LookRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetFaces(start, end, out body);
            Trace.TraceInformation("\nRaw JSON data:  {0}", body);

            if (responseCode == HttpStatusCode.OK)
            {
                faces = JsonHelper.JsonToFacesHistoricRec(body);
            }
            else
            {
                faces = null;
            }

            return responseCode;       
        }


        public bool GetMostRelevantUserInfo(out int age, out Gender gender)
        {
            ValidateIsBound();
            age = 0;
            gender = Gender.Unknown;

            var client = new LookRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetFaces(out body);
            Trace.TraceInformation("\nRaw JSON data:  {0}", body);

            FacesRec rec = JsonHelper.JsonToFacesRec(body);
            if (rec.faces.Count < 1) return false;
            FaceRec primaryFace = null;
            foreach (var item in rec.faces)
            {
                if (primaryFace == null)
                {
                    primaryFace = item;
                    continue;
                }
                if (item.id < primaryFace.id)
                {
                    primaryFace = item;
                }
            }
            age = primaryFace.age;
            gender = primaryFace.gender;

            return responseCode == System.Net.HttpStatusCode.OK;
        }

        /*
         * 
{
    "majority_gender": -1,
    "objects": [
        {
            "age": "39.5744680851064",
            "age_confidence": "0.480845452551689",
            "gender": "-1",
            "gender_confidence": "0.936170212765957",
            "id": "0"
        }
    ],
    "primary_gender": -1
}
         * */

    }
}
