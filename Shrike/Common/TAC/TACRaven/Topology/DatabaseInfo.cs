using System;

namespace AppComponents.Topology
{
    public class DatabaseInfo
    {
        [DocumentIdentifier]
        public string Application { get; set; }
        public string DatabaseSchemaVersion { get; set; }
        public DateTime InstallDate { get; set; } //TODO Make a default value, nullable may be

        public string Url { get; set; }
    }

    public class EmailServerInfo
    {
        [DocumentIdentifier]
        public string Application { get; set; }

        public string SmtpServer { get; set; }
        public int Port { get; set; }
        public bool IsSsl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime ConfiguredDate { get; set; }
        public string ReplyAddress { get; set; }
    }
}
