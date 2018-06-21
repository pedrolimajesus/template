using System;
using System.Linq;

namespace AppComponents.Web.Email
{
    using System.Diagnostics;
    using System.Net;
    using System.Net.Mail;

    using AppComponents.Extensions.EnumEx;

    using log4net;

    public class SMTPMessagePublisher : IMessagePublisher
    {
        private readonly ILog _log;

        private SmtpClient smtpClient;

        public SmtpClient SMTPClient
        {
            get
            {
                if (this.smtpClient == null)
                {
                    this.SetSmtpServer();
                }

                return this.smtpClient;
            }
            private set
            {
                this.smtpClient = value;
            }
        }

        public SMTPMessagePublisher()
        {
            _log = ClassLogger.Create(this.GetType());
        }

        private void SetSmtpServer()
        {
            var config = Catalog.Factory.Resolve<IConfig>();
            var emailServer = config[SendEmailSettings.EmailServer];
            var emailPort = config[SendEmailSettings.EmailPort];

            this.smtpClient = new SmtpClient(emailServer, Convert.ToInt32(emailPort))
                {
                    EnableSsl = Convert.ToBoolean(config[SendEmailSettings.UseSSL]),
                    Credentials =
                        new NetworkCredential(config[SendEmailSettings.EmailAccount], config[SendEmailSettings.EmailPassword])
                };
        }

        public void Send<T>(T msg, string routeKey)
        {
            try
            {
                var message = msg as SendEmail;

                if (message == null)
                {
                    return;
                }
                var recipients = message.Recipients.Aggregate((c, n) => c + "," + n);

                var mm = new MailMessage(message.Sender, recipients, message.Subject, message.Content)
                    { IsBodyHtml = true };
                this.SMTPClient.Send(mm);
            }
            catch (Exception ex)
            {
                _log.ErrorFormat(string.Format("SMTPMessagePublisher Send Exception: {0}", ex.Message));
                throw;
            }
        }

        public void Send<T>(T msg, Enum routeKey)
        {
            Send(msg, routeKey.EnumName());
        }

        public void Dispose()
        {
            if (this.smtpClient == null)
            {
                return;
            }
            this.smtpClient.Dispose();
            this.smtpClient = null;
        }
    }
}