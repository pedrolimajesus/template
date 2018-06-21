using System;

namespace TACMonotorrent
{
    using AppComponents;

    using MonoTorrent.Client;
    using MonoTorrent.Client.Encryption;

    using TACBitTorrent.Enum;
    using TACBitTorrent.Interfaces;

    public class TorrentClientManager : ITorrentClientManager
    {
        public ITorrentDownloader GetTorrentDownloader(Uri torrentDescriptionFileUri, bool initialSeedingEnabled = false, ClientEngine clientEngine = null)
        {
            var engine = clientEngine ?? GetClientEngine();

            var torrentSettings = new TorrentSettings { InitialSeedingEnabled = initialSeedingEnabled };

            var torrentDownloader = GetTorrentDownloader(torrentDescriptionFileUri, torrentSettings, engine);
            return torrentDownloader;
        }

        public ClientEngine GetClientEngine()
        {
            var config = Catalog.Factory.Resolve<IConfig>();
            var listenPort = Convert.ToInt32(config[BitTorrentSettings.ClientPeerPort]);
            var engineSettings = new EngineSettings(config[BitTorrentSettings.DownloadFolder], listenPort)
                { PreferEncryption = false, AllowedEncryption = EncryptionTypes.All };
            var engine = new ClientEngine(engineSettings);
            return engine;
        }

        public ITorrentDownloader GetTorrentDownloader(Uri torrentDescriptionFileUri, TorrentSettings torrentSettings, ClientEngine clientEngine)
        {
            var torrentDownloader = new TorrentDownloader(torrentDescriptionFileUri, torrentSettings, clientEngine);
            return torrentDownloader;
        }

        public ITorrentCreator CreateTorrentCreator()
        {
            var creator = new TorrentCreator();
            return creator;
        }

        private ITorrentSeeder seeder =null;
        public ITorrentSeeder GetSeeder()
        {
            return this.seeder ?? (this.seeder = new TorrentSeeder());
        }
    }
}
