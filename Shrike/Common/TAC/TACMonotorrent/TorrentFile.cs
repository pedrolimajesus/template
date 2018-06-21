using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TACMonotorrent
{
    using TACBitTorrent.Interfaces;

    public class TorrentFile : ITorrent
    {
        private readonly Uri uriFile;
        public TorrentFile(string filePath, string relativePath)
        {
            uriFile = new Uri(filePath);
            RelativeFilePath = relativePath;
        }

        public Uri TorrentFileUri
        {
            get
            {
                return uriFile;
            }
        }

        public string RelativeFilePath { get; private set; }
    }
}
