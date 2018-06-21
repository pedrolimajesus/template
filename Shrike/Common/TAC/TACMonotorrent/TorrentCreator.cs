using System;
using System.Linq;

namespace TACMonotorrent
{
    using System.IO;

    using AppComponents;

    using MonoTorrent;
    using MonoTorrent.Common;

    using TACBitTorrent.Enum;
    using TACBitTorrent.Interfaces;

    public class TorrentCreator : ITorrentCreator
    {
        private const int PieceLength = 16384;//64 * 1024;

        public ITorrent CreateTorrent(string downloadFolder, string relativeFileSourcePath, string destinationFolder)
        {
            var creator = new MonoTorrent.Common.TorrentCreator();

            var config = Catalog.Factory.Resolve<IConfig>();

            creator.Announces.Clear();

            // Add one tier which contains a tracker
            var tier = new RawTrackerTier
                {
                    string.Format(
                        "http://{0}:{1}/announce",
                        config[BitTorrentSettings.TrackerHost],
                        config[BitTorrentSettings.TrackerPort])
                };

            creator.Announces.Add(tier);

            creator.Announce = string.Format(
                "http://{0}:{1}/announce",
                config[BitTorrentSettings.TrackerHost],
                config[BitTorrentSettings.TrackerPort]);

            
            creator.CreatedBy = "Monotorrent Client/" + VersionInfo.ClientVersion;

            creator.Comment = downloadFolder;
            creator.Publisher = relativeFileSourcePath;

            creator.PieceLength = PieceLength;
            creator.PublisherUrl = string.Empty;

            //not allowing dht, peer exchange
            creator.Private = true;

            creator.Hashed += (o, e) => Console.WriteLine("{0} {1}", e.FileSize, e.CurrentFile.First());

            var fullSourcePath = Path.Combine(downloadFolder, relativeFileSourcePath);
            var fileSource = new TorrentFileSource(fullSourcePath);
            
            //var randomName = Path.GetTempFileName();
            var destFile = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(fullSourcePath) + ".torrent");

            creator.Create(fileSource, destFile);

            var torrentFile = new TorrentFile(destFile, relativeFileSourcePath);
            return torrentFile;
        }
    }
}