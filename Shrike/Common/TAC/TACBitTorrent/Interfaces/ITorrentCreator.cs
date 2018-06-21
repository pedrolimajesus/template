namespace TACBitTorrent.Interfaces
{
    public interface ITorrentCreator
    {
        //ITorrent CreateTorrent(string sourceTorrentFolder, string destinationFolder);
        ITorrent CreateTorrent(string downloadFolder, string fileSourcePath, string destinationFolder);
    }
}
