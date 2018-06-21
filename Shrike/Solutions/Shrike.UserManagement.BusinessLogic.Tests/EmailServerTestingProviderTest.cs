using AppComponents.Topology;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shrike.ExceptionHandling.Exceptions;
using Shrike.UserManagement.BusinessLogic.Business.NodeTestingProviders;

namespace Shrike.UserManagement.BusinessLogic.Tests
{
    [TestClass]
    public class EmailServerTestingProviderTest
    {
        [TestMethod]
        public void TestSmtpServerTest()
        {
            var provider = new EmailServerTestingProvider();
            provider.TestSmtpServer("smtp.gmail.com", 25);
        }

        [TestMethod]
        [ExpectedException(typeof(SmtpException))]
        public void NegativeTestSmtpServerTest()
        {
            var provider = new EmailServerTestingProvider();
            provider.TestSmtpServer("smtp.gma☺il.com", 25);//it has an invalid character
            //http://dev.lo-ksystems.com:900/browse/ACC-179
        }

        [TestMethod]
        public void TestSmtpServer1Test()
        {
            var provider = new EmailServerTestingProvider();
            var info = new EmailServerInfo
            {
                SmtpServer = "smtp.gmail.com",
                ReplyAddress = "control@lo-ksystems.com",
                Port = 587,
                IsSsl = true,
                Username = "loksystemstest@gmail.com",
                Password = "LokSys2012"
            };

            provider.TestSmtpServer(info, "roryvidal@gmail.com", "tenancy01");
        }
    }
}
