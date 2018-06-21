using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TACBitTorrent.Configuration
{
    using System.Configuration;

    using AppComponents;

    using TACBitTorrent.Enum;

    public class TorrentConfiguration : DictionaryConfigurationBase
    {
        public override void FillDictionary()
        {
            //TODO Read from a configuration section from the configuration file

            var appSettings = ConfigurationManager.AppSettings;
            this._configurationCache.TryAdd(
                BitTorrentSettings.TrackerHost.ToString(), appSettings[BitTorrentSettings.TrackerHost.ToString()]);
            this._configurationCache.TryAdd(
                BitTorrentSettings.TrackerPort.ToString(), appSettings[BitTorrentSettings.TrackerPort.ToString()]);
            this._configurationCache.TryAdd(
                BitTorrentSettings.TrackerTorrentFolder.ToString(),
                appSettings[BitTorrentSettings.TrackerTorrentFolder.ToString()]);
            this._configurationCache.TryAdd(
                BitTorrentSettings.ClientPeerPort.ToString(), appSettings[BitTorrentSettings.ClientPeerPort.ToString()]);
            this._configurationCache.TryAdd(
                BitTorrentSettings.TrackerFactoryClass.ToString(),
                appSettings[BitTorrentSettings.TrackerFactoryClass.ToString()]);
            this._configurationCache.TryAdd(
                BitTorrentSettings.TorrentCreatorClass.ToString(),
                appSettings[BitTorrentSettings.TorrentCreatorClass.ToString()]);
            this._configurationCache.TryAdd(
                BitTorrentSettings.TorrentClientManagerClass.ToString(),
                appSettings[BitTorrentSettings.TorrentClientManagerClass.ToString()]);
            this._configurationCache.TryAdd(
                BitTorrentSettings.DownloadFolder.ToString(), appSettings[BitTorrentSettings.DownloadFolder.ToString()]);
        }
    }
}