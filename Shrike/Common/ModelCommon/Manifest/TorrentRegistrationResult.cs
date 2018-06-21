using System;
using Lok.Unik.ModelCommon.Client;

namespace Lok.Unik.ModelCommon.Manifest
{
    using Lok.Unik.ModelCommon.ItemRegistration;

    public class MsgRegisterBitTorrent : IDateTimeRegitration
    {
        public MsgRegisterBitTorrent()
        {
            RegisterDate = DateTime.UtcNow;
        }

        public string TrackerHost { get; set; }
        public string TrackerPort { get; set; }
        public string DownloadsPath { get; set; }
        public string TorrentsPath { get; set; }

        #region IDateTimeRegitration Members

        public DateTime RegisterDate { get; private set; }

        #endregion
    }

    public class BitTorrentRegistrationResult
    {
        public ResultCode ResultCode { get; set; }
        public string Server { get; set; }
    }
}
