using System;

namespace TACBitTorrent.Interfaces
{
    using MonoTorrent.Client;

    public interface ITorrentClientManager
    {
        ITorrentDownloader GetTorrentDownloader(
            Uri torrentDescriptionFileUri, bool initialSeedingEnabled = false, ClientEngine clientEngine = null);

        ITorrentDownloader GetTorrentDownloader(
            Uri torrentDescriptionFileUri, TorrentSettings torrentSettings, ClientEngine clientEngine);

        ITorrentCreator CreateTorrentCreator();

        ITorrentSeeder GetSeeder();

        ClientEngine GetClientEngine();
    }
}