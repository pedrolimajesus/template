namespace TACBitTorrent.Interfaces
{
    using System;

    using TACBitTorrent.Enum;
    using TACBitTorrent.EventArgs;

    public interface ITorrentDownloader
    {
        ITorrent Torrent { get; }
        TorrentState State { get; }

        void Resume();

        void Delete(bool withContent = false);

        void Pause();

        void Start();

        event EventHandler<TorrentEventArgs> TorrentCompleted;

        event EventHandler<TorrentEventArgs> TorrentPaused;

        event EventHandler<TorrentEventArgs> TorrentResumed;

        event EventHandler<TorrentEventArgs> TorrentStarted;

        event EventHandler<TorrentEventArgs> TorrentDeleted;

        event EventHandler<TorrentEventArgs> TorrentError;

        void Stop();
    }
}