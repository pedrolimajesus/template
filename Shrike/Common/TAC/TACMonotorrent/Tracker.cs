using System;
using System.Collections.Generic;

using TACBitTorrent.Interfaces;

namespace TACMonotorrent
{
    using System.Diagnostics;
    using System.IO;
    using AppComponents;
    using log4net;
    using MonoTorrent;
    using MonoTorrent.Common;
    using MonoTorrent.TorrentWatcher;
    using MonoTorrent.Tracker;
    using MonoTorrent.Tracker.Listeners;

    /// <summary>
    /// Small ITrackable file to be stored on memory
    /// </summary>
    public class CustomITrackable : ITrackable
    {

        public CustomITrackable(Torrent t)
        {
            this.Files = t.Files;
            this.InfoHash = t.InfoHash;
            this.Name = t.Name;
        }

        /// <summary>
        /// The files in the torrent
        /// </summary>
        public MonoTorrent.Common.TorrentFile[] Files { get; set; }

        /// <summary>
        /// The infohash of the torrent
        /// </summary>
        public InfoHash InfoHash { get; private set; }

        /// <summary>
        /// The name of the torrent
        /// </summary>
        public string Name { get; private set; }
    }

    public class Tracker : ITracker
    {
        private readonly ILog _log = ClassLogger.Create(typeof(Tracker));

        private readonly MonoTorrent.Tracker.Tracker realTracker;

        private readonly TorrentFolderWatcher watcher;

        private readonly HttpListener listener;

        private readonly IDictionary<string, ITrackable> torrentTrackables;

        public Tracker(string hostIpAddress, int port, string torrentFolder)
        {
            torrentTrackables = new Dictionary<string, ITrackable>();

            realTracker = new MonoTorrent.Tracker.Tracker
                {
                    AllowUnregisteredTorrents = true,
                    AnnounceInterval = TimeSpan.FromHours(1),
                    MinAnnounceInterval = TimeSpan.FromMinutes(10)
                };

            //var listenPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(hostIpAddress), port);
            //listener = new HttpListener(listenPoint);
            var endPoint = string.Format("http://{0}:{1}/announce/", hostIpAddress, port);
            listener = new HttpListener(endPoint);
            realTracker.RegisterListener(listener);

            this.watcher = new TorrentFolderWatcher(Path.GetFullPath(torrentFolder), "*.torrent");
            this.watcher.TorrentFound += delegate(object sender, TorrentWatcherEventArgs e)
                {
                    try
                    {
                        // hack
                        System.Threading.Thread.Sleep(500);

                        this.AddATorrent(e.TorrentPath);
                    }
                    catch (Exception ex)
                    {
                        if(ex.InnerException!=null)
                            _log.Error("Error loading torrent from disk: " + ex.ToString()+" \n InnerException: "+ex.InnerException.ToString());
                        else
                            _log.Error("Error loading torrent from disk: "+ ex.ToString());
                        Debug.WriteLine("Error loading torrent from disk: {0}", ex.Message);
                        Debug.WriteLine("Stacktrace: {0}", ex.ToString());
                    }
                };
        }

        private void AddATorrent(string torrentPath)
        {
            var aTorrent = Torrent.Load(torrentPath);
            // InfoHashTrackable stores the infohash and name of the torrent.
            // ITrackable trackable = new InfoHashTrackable(t);
            ITrackable trackable = new CustomITrackable(aTorrent);

            // lock tracker for asynchronous operations
            lock (this.realTracker)
            {
                torrentTrackables.Add(torrentPath, trackable);
                this.realTracker.Add(trackable);
            }
            Debug.WriteLine("Tracking new torrent: {0}", torrentPath);
            Console.WriteLine("Tracking new torrent: {0}", torrentPath);
        }

        public void Start()
        {
            if (listener.Running)
            {
                return;
            }
            this.listener.Start();
            Debug.WriteLine("Tracker listening {0}", listener.Running);

            this.watcher.Start();
            this.watcher.ForceScan();
            Debug.WriteLine("Tracker torrent watcher");
        }

        public void Stop()
        {
            if (!this.listener.Running)
            {
                return;
            }

            Debug.WriteLine("Tracker stopping.");
            this.listener.Stop();

            this.watcher.Stop();

            lock (this.realTracker)
            {
                foreach (var torrentTrackable in torrentTrackables)
                {
                    realTracker.Remove(torrentTrackable.Value);
                }

                this.torrentTrackables.Clear();
            }
        }

        public void AddTorrent(ITorrent torrent)
        {
            AddATorrent(torrent.TorrentFileUri.LocalPath);
        }

        public void AddTorrent(string fileUrl)
        {
            AddATorrent(fileUrl);
        }

        public void RemoveTorrent(Uri torrentUri)
        {
            lock (this.realTracker)
            {
                this.Remove(torrentUri);
            }
        }

        private void Remove(Uri torrentUri)
        {
            var trackable = this.torrentTrackables[torrentUri.LocalPath];
            this.realTracker.Remove(trackable);
            this.torrentTrackables.Remove(torrentUri.LocalPath);
        }

        public void RemoveTorrent(ITorrent torrent)
        {
            this.RemoveTorrent(torrent.TorrentFileUri);
        }
    }
}