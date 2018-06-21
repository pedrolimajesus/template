namespace TACBitTorrent.Interfaces
{
    using System;

    public interface ITorrentSeeder
    {
        void Start();

        void Stop();

        bool IsRunning { get; }

        event Action<ITorrentSeeder, SeedingEventArgs> StartSeedingFile;
    }

    public class SeedingEventArgs
    {
        public Uri TorrentFileUri { get; private set; }

        public DateTime BeginTime { get; private set; }

        public SeedingEventArgs(Uri torrentFileUri, ITorrentDownloader downloader)
        {
            this.TorrentFileUri = torrentFileUri;
            Downloader = downloader;
            BeginTime = DateTime.UtcNow;
        }

        public ITorrentDownloader Downloader { get; private set; }
    }
}
