namespace TACBitTorrent.Interfaces
{
    using System;

    public interface ITorrent
    {
        Uri TorrentFileUri { get; }

        string RelativeFilePath { get; }
    }
}
