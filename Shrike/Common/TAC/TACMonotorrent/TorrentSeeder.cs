using System;
using System.Collections.Generic;
using System.Linq;

namespace TACMonotorrent
{
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using AppComponents;
    using log4net;
    using MonoTorrent.Client;
    using MonoTorrent.TorrentWatcher;
    using TACBitTorrent;
    using TACBitTorrent.Enum;
    using TACBitTorrent.Interfaces;

    public class TorrentSeeder : ITorrentSeeder
    {
        private readonly ILog _log = ClassLogger.Create(typeof(TorrentSeeder));

        private readonly IList<ITorrentDownloader> downloaders;

        private readonly ClientEngine clientEngine;

        public TorrentSeeder()
        {
            downloaders = new List<ITorrentDownloader>();
            clientEngine = TorrentHelper.ClientManager.GetClientEngine(); //creates new client engine
        }

        #region Implementation of ITorrentSeeder

        public void Start()
        {
            lock (downloaders)
            {
                StartSeedingAllTorrents();
                IsRunning = true;
            }
        }

        public void Stop()
        {
            lock (downloaders)
            {
                StopSeeding();
                downloaders.Clear();
                watcher.Stop();
                IsRunning = false;
            }
        }

        public bool IsRunning { get; private set; }

        public event Action<ITorrentSeeder, SeedingEventArgs> StartSeedingFile;

        private void OnStartSeedingFile(ITorrentDownloader downloader)
        {
            if (StartSeedingFile != null)
            {
                StartSeedingFile(this, new SeedingEventArgs(downloader.Torrent.TorrentFileUri, downloader));
            }
        }

        private TorrentFolderWatcher watcher;

        private void StartSeedingAllTorrents()
        {
            var config = Catalog.Factory.Resolve<IConfig>();
            var torrentFolder = config[BitTorrentSettings.TrackerTorrentFolder];
            var torrentDirInfo = new DirectoryInfo(torrentFolder);
            if (!torrentDirInfo.Exists) torrentDirInfo = Directory.CreateDirectory(torrentFolder);

            foreach (var torrentFileInfo in torrentDirInfo.GetFiles("*.torrent"))
            {
                StartSeedingTorrentFile(torrentFileInfo);
            }

            watcher = new TorrentFolderWatcher(Path.GetFullPath(torrentFolder), "*.torrent");
            watcher.TorrentFound += (sender, e) =>
                {
                    if (e == null)
                    {
                        throw new ArgumentNullException("e");
                    }
                    try
                    {
                        // hack
                        Thread.Sleep(500);
                        Debug.WriteLine("Torrent found at " + torrentFolder + " " + e.TorrentPath);
                        this.StartSeedingTorrentFile(new FileInfo(e.TorrentPath));
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null)
                            _log.Error("Error loading torrent from disk: " + ex.ToString() + " \n InnerException: " + ex.InnerException.ToString());
                        else
                            _log.Error("Error loading torrent from disk: " + ex.ToString());
                        Debug.WriteLine("Error loading torrent from disk: {0}", ex.Message);
                        Debug.WriteLine("Stacktrace: {0}", ex.ToString());
                    }
                };

            watcher.Start();
            watcher.ForceScan();
        }

        private void StartSeedingTorrentFile(FileInfo torrentFileInfo)
        {
            //if it is already being seed , do not seed it more than once
            if (
                downloaders.Any(
                    d =>
                    d.Torrent.TorrentFileUri.LocalPath.Equals(torrentFileInfo.FullName, StringComparison.InvariantCultureIgnoreCase)
                    )
                )
            {
                return;
            }

            if (!torrentFileInfo.Exists) return;

            var downloader = StartSeeding(new Uri(String.Format("file:///{0}", torrentFileInfo.FullName)));
            lock (downloaders)
            {
                downloaders.Add(downloader);
            }
        }

        public ITorrentDownloader StartSeeding(Uri torrentUri)
        {
            Debug.WriteLine("Seeding {0} ...", torrentUri);
            var downloader = TorrentHelper.ClientManager.GetTorrentDownloader(torrentUri, true, clientEngine);
            downloader.Start();

            OnStartSeedingFile(downloader);
            return downloader;
        }

        private void StopSeeding(IEnumerable<ITorrentDownloader> downloaders1)
        {
            foreach (var torrentDownloader in downloaders1)
            {
                torrentDownloader.Stop();
            }
        }

        private void StopSeeding()
        {
            if (downloaders != null)
            {
                StopSeeding(downloaders);
            }
        }

        #endregion
    }
}