using System;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Web;
using AppComponents;
using AppComponents.Topology;
using log4net;
using Shrike.ExceptionHandling;
using Shrike.ExceptionHandling.Logic;
using Shrike.Tenancy.Web;

namespace Shrike.UserManagement.BusinessLogic.Business.NodeTestingProviders
{
    public class EmailServerTestingProvider : INodeTestingProvider
    {
        private readonly ILog _log;

        public EmailServerTestingProvider()
        {
            _log = ClassLogger.Create(GetType());
        }

        public bool TestNode(object parameters)
        {
            return TestNode((EmailServerInfo) parameters);
        }

        public bool TestNode(EmailServerInfo parameters, string email = null)
        {
            try
            {
                TestSmtpServer(parameters, email);
                return true;
            }
            catch (Exception exception)
            {
                ExceptionHandler.Manage(exception, this, Layer.BusinessLogic);
                return false;
            }
        }

        public void TestSmtpServer(string server, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect(server, port);
                    client.Close();
                }
            }
            catch (Exception exception)
            {
                var smtpException = new ExceptionHandling.Exceptions.SmtpException(exception.Message, exception);
                ExceptionHandler.Manage(smtpException, this, Layer.BusinessLogic);
            }
        }

        public void TestSmtpServer(EmailServerInfo info, string email)
        {
            try
            {
                TestSmtpServer(info, email, TenantHelper.GetCurrentTenantFormUrl(HttpContext.Current));
            }
            catch (Exception exception)
            {
                throw new ExceptionHandling.Exceptions.SmtpException(
                    string.Format(
                        "Email settings: recipient={0}, sender={1}, server={2}, port={3}, isSSL={4}, username={5}",
                        email, info.ReplyAddress, info.SmtpServer, info.Port, info.IsSsl, info.Username) +
                    Environment.NewLine + exception.Message, exception);
            }
        }

        public void TestSmtpServer(EmailServerInfo info, string email, string tenancy)
        {
            string body;
            var subject = SendEmail.GetTemplateRendering("testEmail", out body,
                tenancy,
                email,
                DateTime.UtcNow.ToString("F"));

            var smtpServer = new SmtpClient(info.SmtpServer, info.Port)
            {
                Credentials = new System.Net.NetworkCredential(info.Username, info.Password),
                EnableSsl = info.IsSsl
            };

            var mail = new MailMessage {From = new MailAddress(info.ReplyAddress)};
            mail.To.Add(email);
            mail.Subject = subject;
            mail.Body = body;
            mail.ReplyToList.Add(info.ReplyAddress);
            smtpServer.Send(mail);

            //var emailSender = SendEmail.CreateFromTemplateSMTP(
            //    info.ReplyAddress,
            //    new[] {email},
            //    "testEmail",
            //    TenantHelper.GetCurrentTenantFormUrl(HttpContext.Current),
            //    email,
            //    DateTime.UtcNow.ToString("F")
            //    );
            //emailSender.Send();
        }
    }
}