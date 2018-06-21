using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TACTorrentClient.Console.Test
{
    using System.Diagnostics;
    using System.IO;

    using AppComponents;

    using TACBitTorrent;
    using TACBitTorrent.Enum;

    internal static class Program
    {
        private static void Main(string[] args1)
        {
            //Please fill folder c:\windows\temp\downloads\a with test files for the torrent to be created and shared
            var config = Catalog.Factory.Resolve<IConfig>();
            var downloadPath = config[BitTorrentSettings.DownloadFolder];
            var torrentPath = config[BitTorrentSettings.TrackerTorrentFolder];

            var contentFilename = Path.Combine(downloadPath, "test.txt");

            var fileId = "out";// Guid.NewGuid();


            var relativePath = "DigitalSignage\\Package\\" + fileId + ".7z";
            var absolutePath = Path.Combine(downloadPath, relativePath);

            if (!File.Exists(absolutePath))
            {
                var dirName = Path.GetDirectoryName(absolutePath);
                if (dirName != null && !Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }

                using (var sw = new StreamWriter(absolutePath))
                {
                    sw.WriteLine("It is just a test! " + absolutePath);
                }
            }

            if (!Directory.Exists(torrentPath))
            {
                Directory.CreateDirectory(torrentPath);
            }

            var seeder = TorrentHelper.ClientManager.GetSeeder();
            seeder.StartSeedingFile += (sender, args) =>
                {
                    Debug.WriteLine("Seeding file " +args.TorrentFileUri);
                        System.Console.WriteLine("Seeding {0}", args.TorrentFileUri);
                    
                };

            if (!seeder.IsRunning)
            {
                seeder.Start();
            }

            var torrent = TorrentHelper.CreateTorrent(downloadPath, relativePath, torrentPath);
            System.Console.WriteLine("Torrent Created {0}", torrent.TorrentFileUri);

            //torrent = TorrentHelper.CreateTorrent(downloadPath, "DigitalSignage\\Package\\out.7z", torrentPath);
            //System.Console.WriteLine("Torrent Created {0}", torrent.TorrentFileUri);

            System.Console.ReadLine();

            seeder.Stop();
        }
    }
}