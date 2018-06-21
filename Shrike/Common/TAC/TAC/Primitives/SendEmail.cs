// // 
// //  Copyright 2012 David Gressett
// // 
// //    Licensed under the Apache License, Version 2.0 (the "License");
// //    you may not use this file except in compliance with the License.
// //    You may obtain a copy of the License at
// // 
// //        http://www.apache.org/licenses/LICENSE-2.0
// // 
// //    Unless required by applicable law or agreed to in writing, software
// //    distributed under the License is distributed on an "AS IS" BASIS,
// //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //    See the License for the specific language governing permissions and
// //    limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;

using AppComponents.Extensions.EnumerableEx;
using AppComponents.Extensions.ExceptionEx;

using Newtonsoft.Json;

namespace AppComponents
{
    using System.Web;

    public enum SendEmailSettings
    {
        EmailServer,

        EmailAccount,

        EmailPassword,

        EmailPort,

        UseSSL,

        EmailReplyAddress,

        WorkerExchange
    }

    public class EmailTemplate
    {
        public const string Container = "emailtemplates";

        public string Subject { get; set; }

        public string Content { get; set; }
    }

    public class SendEmail
    {
        public const string Route = "emaildistribution";

        public const string Queue = "emailqueue";

        public const string Exchange = "emailexchange";

        private IMessagePublisher _sender;

        public string Sender { get; set; }

        public string[] Recipients { get; set; }

        public string Subject { get; set; }

        public string Content { get; set; }

        public bool HtmlFormat { get; set; }

        public static SendEmail CreateFromTemplateSMTP(string sender, string[] recipients, string templateName, params object[] data)
        {
            string body;
            var subject = GetTemplateRendering(templateName, out body, data);

            var sender1 = Catalog.Preconfigure().ConfiguredResolve<IMessagePublisher>();
            return new SendEmail
                {
                    Sender = sender,
                    Recipients = recipients,
                    Subject = subject,
                    Content = body,
                    HtmlFormat = true,
                    _sender = sender1
                };
        }

        public static string GetTemplateRendering(string templateName, out string body, params object[] data)
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Safe);
            string subject;
            body = GetTemplateRendering(templateName, data, config, out subject);
            return subject;
        }

        private static string GetTemplateRendering(string templateName, object[] data, IConfig config, out string subject)
        {
            var template = GetEmailTemplate(templateName, config);

            if (null == template)
            {
                throw new ArgumentException(string.Format("template {0} not found", templateName));
            }

            string body;
            if (null != data && data.Any())
            {
                body = string.Format(template.Content, data);
            }
            else
            {
                body = template.Content;
            }

            subject = null != data && data.Any() ? string.Format(template.Subject, data) : template.Subject;
            return body;
        }

        public static SendEmail CreateSMTP(string sender, string content, string subject, string [] recipients)
        {
            var sender1 = Catalog.Preconfigure().ConfiguredResolve<IMessagePublisher>();
            return new SendEmail
            {
                Sender = sender,
                Recipients = recipients,
                Subject = subject,
                Content = content,
                HtmlFormat = true,
                _sender = sender1
            };
        }

        public static SendEmail CreateFromTemplate
            (string sender, string[] recipients, string templateName, params object[] data)
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Safe);
            string body;
            var subject = GetTemplateRendering(templateName, out body, data);

            var host = config.Get(CommonConfiguration.DefaultBusConnection, string.Empty);
            var exchange = config.Get(SendEmailSettings.WorkerExchange, Exchange);

            DeclareExchange(exchange, host);

            return new SendEmail
            {
                Sender = sender,
                Recipients = recipients,
                Subject = subject,
                Content = body,
                HtmlFormat = true,
                _sender =
                    Catalog.Preconfigure().Add(MessagePublisherLocalConfig.HostConnectionString, host).Add(
                        MessagePublisherLocalConfig.ExchangeName, exchange).ConfiguredResolve<IMessagePublisher>(
                            ScopeFactoryContexts.Distributed)
            };
        }

        private static EmailTemplate GetEmailTemplate(string templateName, IConfig config)
        {
            var src = new StringResourcesCache();

            var ch = config[CommonConfiguration.DefaultStorageConnection];
            if (ch.StartsWith("~") && HttpContext.Current != null)
            {
                ch = HttpContext.Current.Server.MapPath(ch);
            }

            var templates =
                Catalog.Preconfigure().Add(BlobContainerLocalConfig.ContainerHost, ch).Add(
                    BlobContainerLocalConfig.ContainerName, EmailTemplate.Container).Add(
                        BlobContainerLocalConfig.OptionalAccess, EntityAccess.Private).ConfiguredResolve
                    <IBlobContainer<EmailTemplate>>();

            var jsonFile = templateName + ".json";

            var template = src.ResourceNames.Contains(jsonFile)
                                         ? JsonConvert.DeserializeObject<EmailTemplate>(src[jsonFile])
                                         : templates.Get(templateName);
            return template;
        }

        public static void DeclareExchange(string exchange, string host)
        {
            var specifier =
                Catalog.Preconfigure().Add(MessageBusSpecifierLocalConfig.HostConnectionString, host).ConfiguredResolve
                    <IMessageBusSpecifier>();

            specifier.DeclareExchange(exchange, ExchangeTypes.Direct).SpecifyExchange(exchange).DeclareQueue(
                Queue, Route);
        }

        public void Send()
        {
            _sender.Send(this, Route);
        }
    }

    public class SendEmailWorker : IDisposable
    {
        private readonly string _account;

        private readonly string _password;

        private readonly int _port;

        private readonly string _server;

        private readonly bool _useSSL;
        
        private readonly string _replyAddress;

        private bool _isDisposed;

        private readonly IMessageListener _listener;

        public SendEmailWorker()
        {
            var config = Catalog.Factory.Resolve<IConfig>();

            _server = config[SendEmailSettings.EmailServer];
            _account = config[SendEmailSettings.EmailAccount];
            _password = config[SendEmailSettings.EmailPassword];
            _port = config.Get(SendEmailSettings.EmailPort, 25);
            _useSSL = config.Get(SendEmailSettings.UseSSL, false);
            _replyAddress = config[SendEmailSettings.EmailReplyAddress];

            var host = config[CommonConfiguration.DefaultBusConnection];
            var exchange = config.Get(SendEmailSettings.WorkerExchange, SendEmail.Exchange);

            SendEmail.DeclareExchange(exchange, host);

            _listener =
                Catalog.Preconfigure().Add(MessageListenerLocalConfig.HostConnectionString, host).Add(
                    MessageListenerLocalConfig.ExchangeName, exchange).Add(
                        MessageListenerLocalConfig.QueueName, SendEmail.Queue).ConfiguredResolve<IMessageListener>();

            _listener.Listen(
                new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>(typeof(SendEmail), Email));
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _listener.Dispose();
            }
        }

        #endregion

        private void Email(object msg, CancellationToken ct, IMessageAcknowledge ack)
        {
            var log = ClassLogger.Create(typeof(SendEmailWorker));

            var command = (SendEmail)msg;

            Debug.Assert(!string.IsNullOrEmpty(command.Sender));
            Debug.Assert(!string.IsNullOrEmpty(command.Subject));
            Debug.Assert(null != command.Content);
            Debug.Assert(command.Recipients.EmptyIfNull().Any());

            if (!command.Recipients.Any())
            {
                return;
            }

            if (ct.IsCancellationRequested)
            {
                return;
            }

            var cred = new NetworkCredential { UserName = _account, Password = _password };
            var email = new SmtpClient
                { Credentials = cred, UseDefaultCredentials = true, EnableSsl = _useSSL, Port = _port, Host = _server };

            var message = new MailMessage
                {
                    From = new MailAddress(command.Sender),
                    Subject = command.Subject,
                    Body = command.Content,
                    IsBodyHtml = command.HtmlFormat
                };

            foreach (var r in command.Recipients)
            {
                message.To.Add(new MailAddress(r));
            }

            if (!string.IsNullOrEmpty(_replyAddress))
            {
                message.ReplyToList.Add(_replyAddress);
            }

            var sent = false;
            var retryCount = 0;

            while (!sent)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    email.Send(message);
                    log.InfoFormat(
                        "Sent email about '{0}' to {1} recipients", command.Subject, command.Recipients.Count());
                    sent = true;
                }
                catch (SmtpFailedRecipientsException)
                {
                    sent = true;
                }
                catch (SmtpFailedRecipientException)
                {
                    sent = true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount > 10)
                    {
                        const string es = "Could not send email.";
                        log.Error(es);
                        log.TraceException(ex);
                        var on = Catalog.Factory.Resolve<IApplicationAlert>();
                        on.RaiseAlert(ApplicationAlertKind.Services, es, ex);
                        ack.MessageRejected();
                        break;
                    }

                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }

                    ct.WaitHandle.WaitOne(3000);
                }
            }

            if (sent)
            {
                ack.MessageAcknowledged();
            }
        }
    }
}