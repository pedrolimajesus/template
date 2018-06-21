using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lok.AwareLive.Clients.Move.Model;

namespace Lok.AwareLive.Clients.Move
{
    public class MoveClient : IMoveClient
    {
        private string _device;
        private int _port = 8182;
        private string _username;
        private string _password;
        private const int _DATA_PREVIEW_SIZE = 1400;

        public void Bind(string device)
        {
            _device = device;
        }

        public HttpStatusCode GetState(out Model.StateRec rec)
        {
            ValidateIsBound();
            
            MoveRestClient client = new MoveRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetState(out body);
            Trace.TraceInformation("\nRaw JSON data:  {0}", body);

            rec = JsonHelper.JsonToStateRec(body);
            return responseCode;
        }

        public HttpStatusCode GetHotspotEvents(DateTime start, DateTime end, out HotspotEventsRec rec)
        {
            ValidateIsBound();

            var client = new MoveRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetHotspotEvents(start, end, out body);
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}", msg);

            rec = JsonHelper.JsonToHotspotEventsRec(body);
            return responseCode;
        }


        public HttpStatusCode GetBorderEvents(DateTime start, DateTime end, out BorderEventsRec rec)
        {
            ValidateIsBound();

            var client = new MoveRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetBorderEvents(start, end, out body);
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}", msg);

            rec = JsonHelper.JsonToBorderEventsRec(body);
            return responseCode;
        }


        public HttpStatusCode GetAllEvents(DateTime start, DateTime end, out EventsRec rec)
        {
            ValidateIsBound();

            var client = new MoveRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetAllEvents(start, end, out body);
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}", msg);

            rec = JsonHelper.JsonToEventsRec(body);
            return responseCode;
        }

        public HttpStatusCode GetListOfBorders(out BorderDefinitionList rec)
        {
            // To allow legacy code to work with the new SVG Configuration now used by MOVE, we'll map
            // the SVG structures into the legacy structures.
            // todo: CONTROL needs to upgrade to using the new SVG calls and data structures.

            AreaDefinitionList ignored;
            LineDefinitionList linesList;
            rec = new BorderDefinitionList();
            var responseCode = GetSVGLayoutInfo(out ignored, out linesList);

            // Map SVG structure "LineDefinitionList" into legacy "BorderDefinitionList"
            foreach (var line in linesList.LineDefinitions)
            {
                var border = new BorderDefinition();
                border.Active = line.Active;
                border.Id = line.Id;
                border.Initial.X = line.Initial.X;
                border.Initial.Y = line.Initial.Y;
                border.LeftName = line.LeftName;
                border.RightName = line.RightName;
                border.Name = line.Name;
                border.Terminal.X = line.Terminal.X;
                border.Terminal.Y = line.Terminal.Y;
                rec.BorderDefinitions.Add(border);
            }

            return responseCode;
        }

        public HttpStatusCode GetListOfBordersDeprecated(out BorderDefinitionList rec)
        {
            ValidateIsBound();

            var client = new MoveRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetListOfBorders(out body);
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}", msg);

            rec = JsonHelper.JsonToBorderList(body);
            return responseCode;
        }

        public HttpStatusCode GetListOfHotspots(out HotspotDefinitionList rec)
        {
            // To allow legacy code to work with the new SVG Configuration now used by MOVE, we'll map
            // the SVG structures into the legacy structures.
            // todo: CONTROL needs to upgrade to using the new SVG calls and data structures.

            AreaDefinitionList areasList;
            LineDefinitionList ignored;
            rec = new HotspotDefinitionList();
            var responseCode = GetSVGLayoutInfo(out areasList, out ignored);

            // Map SVG structure "AreaDefinitionList" into legacy "HotspotDefinitionList"
            foreach (var area in areasList.AreaDefinitions)
            {
                var hotspot = new HotspotDefinition();
                hotspot.Active = area.Active;
                hotspot.Id = area.Id;
                hotspot.MaskColor = GetSystemDrawingColorFromHexString(area.Color);
                hotspot.Name = area.Name;
                rec.HotspotDefinitions.Add(hotspot);
            }

            return responseCode;
        }

        private MaskColor GetSystemDrawingColorFromHexString(string hexString)
        {
            var maskColor = new MaskColor() {Red = 0, Blue = 0, Green = 0};
            if (!System.Text.RegularExpressions.Regex.IsMatch(hexString, @"[#]([0-9]|[a-f]|[A-F]){6}\b"))
                return maskColor;

            maskColor.Red = int.Parse(hexString.Substring(1, 2), NumberStyles.HexNumber);
            maskColor.Green = int.Parse(hexString.Substring(3, 2), NumberStyles.HexNumber);
            maskColor.Blue = int.Parse(hexString.Substring(5, 2), NumberStyles.HexNumber);
            return maskColor;
        }

        public HttpStatusCode GetListOfHotspotsDeprecated(out HotspotDefinitionList rec)
        {
            // This method used the old MOVE API REST calls that are now deprecated.  Call, instead "GetListOfHotspots".
            ValidateIsBound();

            var client = new MoveRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetListOfHotspots(out body);
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}", msg);

            rec = JsonHelper.JsonToHotspotList(body);
            return responseCode;
        }

        public HttpStatusCode GetSVGLayoutInfo(out Model.AreaDefinitionList areasList,
                                               out Model.LineDefinitionList linesList)
        {
            ValidateIsBound();

            var client = new MoveRestClient(_device, _port, _username, _password);
            string body = null;
            var responseCode = client.GetLayout(out body);
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}{1}", msg, (msg.Length<body.Length) ? "..." : "");

            areasList = XmlHelper.XmlToAreaDefinitionsList(body);
            linesList = XmlHelper.XmlToLineDefinitionsList(body);

            return responseCode;
        }

        public void Bind(string device, string username, string password)
        {
            _device = device;
            _username = username;
            _password = password;
        }

        public void Bind(string device, int port, string userName, string password)
        {
            Bind(device, userName, password);
            _port = port;
        }

        public void ValidateIsBound()
        {
            if (string.IsNullOrEmpty(_device) ||
                string.IsNullOrEmpty(_username) ||
                string.IsNullOrEmpty(_password))
            {
                throw new Exception("Attempting to use Move REST call without first binding to a target device");
            }
        }

        public HttpStatusCode SetAreasSubscription(AreasSubscription areasSubscription)
        {
            ValidateIsBound();

            string body = areasSubscription.ToJsonAsList();
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}{1}", msg, (msg.Length < body.Length) ? "..." : "");

            var client = new MoveRestClient(_device, _port, _username, _password);
            var responseCode = client.SetAreasSubscription(body);

            return responseCode;
        }

        public HttpStatusCode SetLinesSubscription(LinesSubscription linesSubscription)
        {
            ValidateIsBound();

            string body = linesSubscription.ToJsonAsList();
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}{1}", msg, (msg.Length < body.Length) ? "..." : "");

            var client = new MoveRestClient(_device, _port, _username, _password);
            var responseCode = client.SetLinesSubscription(body);

            return responseCode;
        }


        public HttpStatusCode TurnEngineOn()
        {
            ValidateIsBound();

            string body = "{ \"data_service\" : \"running\", \"engine\" : \"running\" }";
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}{1}", msg, (msg.Length < body.Length) ? "..." : "");

            var client = new MoveRestClient(_device, _port, _username, _password);
            var responseCode = client.SetState(body);

            return responseCode;
        }

        public HttpStatusCode TurnEngineOff()
        {
            ValidateIsBound();

            string body = "{ \"data_service\" : \"stopped\", \"engine\" : \"stopped\" }";
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}{1}", msg, (msg.Length < body.Length) ? "..." : "");

            var client = new MoveRestClient(_device, _port, _username, _password);
            var responseCode = client.SetState(body);

            return responseCode;
        }



        public HttpStatusCode GetListOfAreaSubscriptions(out AreasSubscriptionList subscriptionList)
        {
            throw new NotImplementedException();
        }


        public HttpStatusCode AddAreasSubscription(AreasSubscription areasSubscription)
        {
            ValidateIsBound();

            string body = areasSubscription.ToJsonAsItem();
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}{1}", msg, (msg.Length < body.Length) ? "..." : "");

            var client = new MoveRestClient(_device, _port, _username, _password);
            var responseCode = client.AddAreasSubscription(body);

            return responseCode;
        }

        public HttpStatusCode AddLinesSubscription(LinesSubscription linesSubscription)
        {
            ValidateIsBound();

            string body = linesSubscription.ToJsonAsItem();
            string msg = (body.Length > _DATA_PREVIEW_SIZE) ? body.Substring(0, _DATA_PREVIEW_SIZE) : body;
            Trace.TraceInformation("\nRaw JSON data:  {0}{1}", msg, (msg.Length < body.Length) ? "..." : "");

            var client = new MoveRestClient(_device, _port, _username, _password);
            var responseCode = client.AddLinesSubscription(body);

            return responseCode;
        }

    }
}
