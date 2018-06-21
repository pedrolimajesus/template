namespace AppComponents.Web.Email
{
    using System;
    using System.Configuration;
    using System.Net.Configuration;

    using log4net;

    public class WebEmailConfiguration : DictionaryConfigurationBase
    {
        private static readonly ILog _log = ClassLogger.Create(typeof(WebEmailConfiguration));

        private const string SectionName = "system.net/mailSettings/smtp";

        public override void FillDictionary()
        {
            try
            {
                var settings = (SmtpSection)ConfigurationManager.GetSection(SectionName);
                //var config = WebConfigurationManager.OpenWebConfiguration(HttpContext.Current.Request.ApplicationPath);
                //var settings = (MailSettingsSectionGroup)config.GetSectionGroup("system.net/mailSettings");
                if (settings == null)
                {
                    return;
                }

                this._configurationCache.TryAdd(SendEmailSettings.EmailReplyAddress.ToString(), settings.From);
                this._configurationCache.TryAdd(SendEmailSettings.EmailServer.ToString(), settings.Network.Host);
                this._configurationCache.TryAdd(SendEmailSettings.EmailPort.ToString(), settings.Network.Port);
                this._configurationCache.TryAdd(SendEmailSettings.EmailAccount.ToString(), settings.Network.UserName);
                this._configurationCache.TryAdd(SendEmailSettings.EmailPassword.ToString(), settings.Network.Password);
                this._configurationCache.TryAdd(SendEmailSettings.UseSSL.ToString(), settings.Network.EnableSsl);
                this._configurationCache.TryAdd(SendEmailSettings.WorkerExchange.ToString(), settings.Network.TargetName);
            }
            catch (Exception exception)
            {
                _log.Error("Web Email Configuration could not be read", exception);
                _log.ErrorFormat("WebEmailConfiguration failed for {0}", SectionName);
            }

        }
    }
}