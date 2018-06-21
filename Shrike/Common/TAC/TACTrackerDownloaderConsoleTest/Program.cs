using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TACTrackerDownloaderConsoleTest
{
    using System.IO;

    using AppComponents;

    using TACBitTorrent;
    using TACBitTorrent.Enum;

    internal class Program
    {
        private static void Main(string[] args01)
        {
            //Please fill folder c:\windows\temp\downloads\a with test files for the torrent to be created and shared
            var config = Catalog.Factory.Resolve<IConfig>();
            var downloadPath = config[BitTorrentSettings.DownloadFolder];
            var torrentPath = config[BitTorrentSettings.TrackerTorrentFolder];

            if (!Directory.Exists(torrentPath))
            {
                Directory.CreateDirectory(torrentPath);
            }

            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
            }

            var seeder = TorrentHelper.ClientManager.GetSeeder();
            seeder.StartSeedingFile += (sender, args) =>
                {
                    Console.WriteLine("Console app test, Downloading {0}", args.TorrentFileUri);

                    var downloader = args.Downloader;
                    downloader.TorrentStarted +=
                        (sender1, eventArgs) => Console.WriteLine("Started: " + eventArgs.Torrent.TorrentFileUri);
                    downloader.TorrentResumed +=
                        (sender1, eventArgs) => Console.WriteLine("Resumed: " + eventArgs.Torrent.TorrentFileUri);
                    downloader.TorrentPaused +=
                        (sender1, eventArgs) => Console.WriteLine("Paused: " + eventArgs.Torrent.TorrentFileUri);
                    downloader.TorrentDeleted +=
                        (sender1, eventArgs) => Console.WriteLine("Deleted: " + eventArgs.Torrent.TorrentFileUri);
                    downloader.TorrentCompleted +=
                        (sender1, eventArgs) => Console.WriteLine("Completed: " + eventArgs.Torrent.TorrentFileUri);
                };

            if (!seeder.IsRunning)
            {
                seeder.Start();
            }

            System.Console.ReadLine();

            seeder.Stop();

            System.Console.ReadLine();
        }
    }
}