using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using Lok.AwareLive.Clients.Look;
using Lok.AwareLive.Clients.Look.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AwareLiveClients.Tests
{
    [TestClass]
    public class LookTests
    {
        private const string Device = "10.1.90.12";
        private const string UserName = "admin";
        private const string Password = "look";

        [TestMethod]
        [Description("Attempts to call a method without first binding.  An exception is expected.")]
        public void ClientBindNotCalled()
        {
            // Initialize
            var client = new LookClient();

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
        [Description("Gets the LOOK Devices running state for DataService and Engine.")]
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
        [Description("Gets a snapshot from the LOOK device")]
        public void GetSnapshot()
        {
            // Initialize
            var client = GetCommonClient();

            // Call
            SnapshotRec rec;
            var errCode = client.GetSnapshot(out rec);
            Trace.TraceInformation("\nSnapshot URL => {0}", rec.FullPath);
            Trace.TraceInformation("\nReturned status => {0}", errCode);

            // Validate
            Assert.AreEqual(errCode, HttpStatusCode.Created, "REST Call did not return CREATED");
        }

        [TestMethod]
        [Description("Gets information on the current people that are currently in front of the LOOK device")]
        public void GetFacesNow()
        {
            // Initialize
            var client = GetCommonClient();

            // Call
            FacesRec faces;
            var errCode = client.GetFacesNow(out faces);

            if (faces != null)
            {
                var sb = new StringBuilder();

                sb.AppendFormat(" Majority Gender => {0}\n", faces.majority_gender);
                sb.AppendFormat(" Primary Gender => {0}\n", faces.primary_gender);
                foreach (var face in faces.faces)
                {
                    sb.AppendFormat("  - Face ID:{0} => Age:{1} Gender:{2}\n", face.id, face.age, face.gender);
                }

                Trace.TraceInformation("\n{0}", sb);
            }


            // Validate
            Assert.AreEqual(errCode, HttpStatusCode.OK, "REST Call did not return 200 - OK");        
        }


        [TestMethod]
        [Description("Gets information on the last 10 minutes of people in front of the LOOK device")]
        public void GetFacesLast10Minutes()
        {
            // Initialize
            var client = GetCommonClient();

            // Call
            FacesHistoricRec faces;
            var errCode = client.GetFaces(DateTime.UtcNow.Subtract(new TimeSpan(0, 10, 0)), DateTime.UtcNow, out faces);

            if (faces != null)
            {
                var sb = new StringBuilder();

                foreach (var face in faces.faces)
                {
                    sb.AppendFormat("  - Face ID:{0} => Age:{1} Gender:{2}\n", face.id, face.age, face.gender);
                }

                Trace.TraceInformation("\n{0}", sb);
            }


            // Validate
            Assert.AreEqual(errCode, HttpStatusCode.OK, "REST Call did not return 200 - OK");
        }


        private ILookClient GetCommonClient()
        {
            var client = new LookClient() as ILookClient;
            client.Bind(Device, UserName, Password);

            return client;
        }
    }
}
