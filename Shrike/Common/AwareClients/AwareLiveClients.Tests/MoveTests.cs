using System;
using System.Diagnostics;
using System.Net;
using Lok.AwareLive.Clients.Move;
using Lok.AwareLive.Clients.Move.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AwareLiveClients.Tests
{
    [TestClass]
    public class MoveTests
    {
        private const string Device = "10.1.90.220";
        private const string UserName = "admin";
        private const string Password = "move";

        [TestMethod]
        [Description("Attempts to call a method without first binding.  An exception is expected.")]
        public void ClientBindNotCalled()
        {
            // Initialize
            var client = new MoveClient();

            // Call
            try
            {
                StateRec rec;
                var errCode = client.GetState(out rec);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("\nReceived expected exception => {0}", ex);
                return;
            }

            Assert.Fail("Test expected to raise an exception.  No exception raised.");
        }

        [TestMethod]
        [Description("Gets the MOVE Devices running state for DataService and Engine.")]
        public void GetDeviceState()
        {
            // Initialize
            var client = GetCommonClient();

            // Call
            StateRec rec;
            var errCode = client.GetState(out rec);


            // Validate
            Assert.AreEqual(errCode, HttpStatusCode.OK, "REST Call did not return 200 - OK");
        }

        [TestMethod]
        public void GetHotspotEventsDay()
        {
            // Initialize
            var client = GetCommonClient();

            // Call
            HotspotEventsRec rec;
            var utcNow = DateTime.UtcNow;
            DateTime start = utcNow.AddDays(-1);  // Now minus a day
            DateTime end = utcNow;
            var errCode = client.GetHotspotEvents(start, end, out rec);

            if (rec != null)
            {
                Trace.TraceInformation("\nNumber of records returned => {0}", rec.HotspotEvents.Count);
                Trace.TraceInformation("\nFormated Data:\n{0}", rec);
            }

            // Validate
            Assert.AreEqual(errCode, HttpStatusCode.OK, "REST Call did not return 200 - OK");
            Assert.IsNotNull(rec, "Method failed to return any data");
        }

        [TestMethod]
        public void GetBorderEventsDay()
        {
            // Initialize
            var client = GetCommonClient();

            // Call
            BorderEventsRec rec;
            var utcNow = DateTime.UtcNow;
            DateTime start = utcNow.Subtract(new TimeSpan(2, 0, 0, 0));  // Now minus a day
            DateTime end = utcNow.Add(new TimeSpan(1,0,0,0)); // Add a day
            var errCode = client.GetBorderEvents(start, end, out rec);

            if (rec != null)
            {
                Trace.TraceInformation("\nNumber of records returned => {0}", rec.BorderEvents.Count);
                Trace.TraceInformation("\nFormated Data:\n{0}", rec);
            }

            // Validate
            Assert.AreEqual(errCode, HttpStatusCode.OK, "REST Call did not return 200 - OK");
            Assert.IsNotNull(rec, "Method failed to return any data");             
        }

        [TestMethod]
        public void GetListOfBorders()
        {
            // Initialize
            var client = GetCommonClient();

            // Call
            BorderDefinitionList rec;
            var errCode = client.GetListOfBorders(out rec);

            if (rec != null)
            {
                Trace.TraceInformation("\nNumber of records returned => {0}", rec.BorderDefinitions.Count);
                Trace.TraceInformation("\nFormated Data:\n{0}", rec);
            }

            // Validate
            Assert.AreEqual(errCode, HttpStatusCode.OK, "REST Call did not return 200 - OK");
            Assert.IsNotNull(rec, "Method failed to return any data");
        }


        [TestMethod]
        public void GetListOfHotspots()
        {
            // Initialize
            var client = GetCommonClient();

            // Call
            HotspotDefinitionList rec;
            var errCode = client.GetListOfHotspots(out rec);

            if (rec != null)
            {
                Trace.TraceInformation("\nNumber of records returned => {0}", rec.HotspotDefinitions.Count);
                Trace.TraceInformation("\nFormated Data:\n{0}", rec);
            }

            // Validate
            Assert.AreEqual(errCode, HttpStatusCode.OK, "REST Call did not return 200 - OK");
            Assert.IsNotNull(rec, "Method failed to return any data");
        }

        [TestMethod]
        public void GetAllEVentDay()
        {
            // Initialize
            var client = GetCommonClient();

            // Call
            EventsRec rec;
            var utcNow = DateTime.UtcNow;
            DateTime start = utcNow.Subtract(new TimeSpan(1, 0, 0, 0));  // Now minus a day
            DateTime end = utcNow;
            var errCode = client.GetAllEvents(start, end, out rec);

            if (rec != null)
            {
                Trace.TraceInformation("\nNumber of HOTSPOT records returned => {0}", rec.HotspotEvents.Count);
                Trace.TraceInformation("\nNumber of BORDER records returned => {0}", rec.BorderEvents.Count);
                Trace.TraceInformation("\nFormated Data:\n{0}", rec);
            }

            // Validate
            Assert.AreEqual(errCode, HttpStatusCode.OK, "REST Call did not return 200 - OK");
            Assert.IsNotNull(rec, "Method failed to return any data");
        }


        [TestMethod]
        public void GetSVGLayoutInfo()
        {
            // Initialize
            var client = GetCommonClient();

            // Call
            AreaDefinitionList areasRec;
            LineDefinitionList linesRec;
            var errCode = client.GetSVGLayoutInfo(out areasRec, out linesRec);

            if (areasRec != null)
            {
                Trace.TraceInformation("\nNumber of Areas records returned => {0}", areasRec.AreaDefinitions.Count);
                Trace.TraceInformation("\nFormated Data:\n{0}", areasRec);
            }

            if (linesRec != null)
            {
                Trace.TraceInformation("\nNumber of Areas records returned => {0}", linesRec.LineDefinitions.Count);
                Trace.TraceInformation("\nFormated Data:\n{0}", linesRec);
            }

            // Validate
            Assert.AreEqual(errCode, HttpStatusCode.OK, "REST Call did not return 200 - OK");
            Assert.IsNotNull(areasRec, "Method failed to return any Area data");
            Assert.IsNotNull(linesRec, "Method failed to return any Lines data");
        }




        private IMoveClient GetCommonClient()
        {
            var client = new MoveClient() as IMoveClient;
            client.Bind(Device, UserName, Password);

            return client;
        }
    }
}
