using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TACBitTorrent.EventArgs
{
    using TACBitTorrent.Interfaces;

    public class TorrentEventArgs : System.EventArgs
    {
        private readonly ITorrent torrent;
        public TorrentEventArgs(ITorrent torrent)
        {
            this.torrent = torrent;
        }

        public ITorrent Torrent
        {
            get
            {
                return this.torrent;
            }
        }
    }
}
