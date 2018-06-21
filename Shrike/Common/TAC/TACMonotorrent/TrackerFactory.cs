using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TACBitTorrent.Interfaces;

namespace TACMonotorrent
{
    public class TrackerFactory : ITrackerFactory
    {
        public ITracker CreateTracker(string host, int port, string torrentFolder)
        {
            var tracker = new Tracker( host,  port,  torrentFolder);
            return tracker;
        }

        public ITorrentCreator CreateTorrentCreator()
        {
            var creator = new TorrentCreator();
            return creator;
        }
    }
}
