using System;

namespace TACBitTorrent
{
    using System.Diagnostics;
    using System.IO;

    using AppComponents;

    using TACBitTorrent.Enum;
    using TACBitTorrent.Interfaces;

    public static class TorrentHelper
    {
        private static ITracker tracker;

        private static ITrackerFactory trackerFactory;

        /// <summary>
        /// Using default configuration
        /// </summary>
        private static ITrackerFactory TrackerFactory
        {
            get
            {
                return trackerFactory ?? (trackerFactory = Catalog.Factory.Resolve<ITrackerFactory>());
            }
        }

        private static ITorrentClientManager clientManager;

        public static ITorrentClientManager ClientManager
        {
            get
            {
                return clientManager ?? (clientManager = Catalog.Factory.Resolve<ITorrentClientManager>());
            }
        }

        public static ITracker RunTracker()
        {
            if (tracker != null)
            {
                return tracker;
            }

            var config = Catalog.Factory.Resolve<IConfig>();
            tracker = TrackerFactory.CreateTracker(
                config[BitTorrentSettings.TrackerHost],
                Convert.ToInt32(config[BitTorrentSettings.TrackerPort]),
                config[BitTorrentSettings.TrackerTorrentFolder]);

            tracker.Start();
            return tracker;
        }

        public static ITorrentDownloader StartTorrentDownload(Uri torrentUri)
        {
            Debug.WriteLine("Downloading torrent: {0}", torrentUri);
            var downloader = ClientManager.GetTorrentDownloader(torrentUri);
            downloader.Start();
            return downloader;
        }

        public static ITorrent CreateTorrent(string downloadFolder, string relativeFileSourcePath, string destinationFolder)
        {
            var torrent = ClientManager.CreateTorrentCreator().CreateTorrent(downloadFolder, relativeFileSourcePath, destinationFolder);
            return torrent;
        }

        public static byte[] GetTorrentFileBytes(string relativeTorrentContentPath)
        {
            var config = Catalog.Factory.Resolve<IConfig>();
            var downloadFolder = config[BitTorrentSettings.DownloadFolder];
            var absoluteTorrentContentPath = Path.Combine(downloadFolder, relativeTorrentContentPath);

            if (File.Exists(absoluteTorrentContentPath))
            {
                var torrentFolder = config[BitTorrentSettings.TrackerTorrentFolder];
                //var dirname = Path.GetDirectoryName(relativeTorrentContentPath);
                var filename = Path.GetFileNameWithoutExtension(absoluteTorrentContentPath) + ".torrent";

                if (!File.Exists(filename))
                {
                    var torrent = CreateTorrent(downloadFolder, relativeTorrentContentPath, torrentFolder);
                    File.Move(torrent.TorrentFileUri.LocalPath, filename);
                }

                return GetBytesFromFile(filename);
            }
            return null;
        }

        private static byte[] GetBytesFromFile(string filename)
        {
            var fi = new FileInfo(filename);
            var size = fi.Length;
            var bytes = new byte[size];

            using(var fs = new FileStream(filename,FileMode.Open))
            {
                fs.Read(bytes, 0,(int)size);
            }
            return bytes;
        }

        public static void SaveTorrentBytes(string filename, byte[] torrentBytes)
        {
            //var filename = bitTorrentInfo.FileTransferUri.LocalPath;
            var dirname = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(dirname) && !Directory.Exists(dirname))
            {
                Directory.CreateDirectory(dirname);
            }

            using(var fs = new FileStream(filename, FileMode.OpenOrCreate))
            {
                fs.Write(torrentBytes, 0, torrentBytes.Length);
            }
        }

        public static ITorrentSeeder StartSeedingAllTorrents()
        {
            var seeder = ClientManager.GetSeeder();
            seeder.Start();
            return seeder;
        }
    }
}
