namespace TACBitTorrent.Interfaces
{
    public interface ITrackerFactory
    {
        ITracker CreateTracker(string host, int port, string torrentFolder);
    }
}
