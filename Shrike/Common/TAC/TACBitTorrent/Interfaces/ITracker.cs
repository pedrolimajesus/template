using System;

namespace TACBitTorrent.Interfaces
{
    public interface ITracker
    {
        void Start();

        void Stop();

        void AddTorrent(ITorrent torrent);

        void RemoveTorrent(Uri torrentUri);

        void RemoveTorrent(ITorrent torrent);

        void AddTorrent(string fileUrl);
    }
}
