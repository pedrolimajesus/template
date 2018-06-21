using System;

using TACBitTorrent.Interfaces;

namespace TACMonotorrent
{
    using System.IO;

    using AppComponents;

    using MonoTorrent.Client;
    using MonoTorrent.Common;

    using TACBitTorrent.Enum;

    using TorrentEventArgs = TACBitTorrent.EventArgs.TorrentEventArgs;
    using TorrentState = TACBitTorrent.Enum.TorrentState;

    public class TorrentDownloader : ITorrentDownloader
    {
        public TorrentState State { get; private set; }

        public ITorrent Torrent { get; private set; }

        private readonly Torrent torrent;

        private readonly ClientEngine engine;

        private readonly string defaultSaveFolder;

        private readonly TorrentManager manager;

        public TorrentDownloader(
            Uri torrentDescriptionFileUri, TorrentSettings torrentSettings, ClientEngine clientEngine)
        {
            try
            {
                torrent = MonoTorrent.Common.Torrent.Load(torrentDescriptionFileUri.LocalPath);
                this.Torrent = new TorrentFile(torrentDescriptionFileUri.LocalPath, torrent.Publisher);

                var config = Catalog.Factory.Resolve<IConfig>();
                defaultSaveFolder = config[BitTorrentSettings.DownloadFolder];

                var relativePath = torrent.Publisher;
                var absolutePath = Path.Combine(defaultSaveFolder, relativePath);
                var saveFolder = Path.GetDirectoryName(absolutePath);
                if (saveFolder != null && !Directory.Exists(saveFolder))
                {
                    Directory.CreateDirectory(saveFolder);
                }

                manager = new TorrentManager(torrent, saveFolder, torrentSettings);

                State = TorrentState.Downloading;

                manager.TorrentStateChanged += ManagerOnTorrentStateChanged;

                engine = clientEngine;
                engine.Register(manager);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Resume()
        {
        }

        public void Delete(bool withContent = false)
        {
            var fileInfo = new FileInfo(this.Torrent.TorrentFileUri.LocalPath);
            File.Delete(fileInfo.FullName);

            if (!withContent)
            {
                return;
            }

            var dirName = Path.GetDirectoryName(fileInfo.FullName);
            if (dirName != null)
            {
                Directory.Delete(dirName);
            }
        }

        public void Pause()
        {
            manager.Pause();
        }

        public void Start()
        {
            if (!engine.IsRunning)
            {
                engine.StartAll();
            }

            manager.Start();
        }

        public event EventHandler<TorrentEventArgs> TorrentCompleted;

        public event EventHandler<TorrentEventArgs> TorrentPaused;

        public event EventHandler<TorrentEventArgs> TorrentStopped;

        public event EventHandler<TorrentEventArgs> TorrentError;

        public event EventHandler<TorrentEventArgs> TorrentResumed;

        public event EventHandler<TorrentEventArgs> TorrentStarted;

        public event EventHandler<TorrentEventArgs> TorrentDeleted;

        public void Stop()
        {
            if (engine.IsRunning)
            {
                engine.StopAll();
            }
            manager.Stop();
        }

        private void OnTorrentEvent(EventHandler<TorrentEventArgs> theEvent)
        {
            if (theEvent == null)
            {
                return;
            }

            var eArgs = new TorrentEventArgs(this.Torrent);
            theEvent(this, eArgs);
        }

        private void ManagerOnTorrentStateChanged(object sender, TorrentStateChangedEventArgs e)
        {
            State = (TorrentState)e.NewState;

            switch (State)
            {
                case TorrentState.Stopped:
                    this.OnTorrentEvent(TorrentStopped);
                    break;
                case TorrentState.Paused:
                    this.OnTorrentEvent(TorrentPaused);
                    break;
                case TorrentState.Downloading:
                    this.OnTorrentEvent(TorrentResumed);
                    break;
                case TorrentState.Seeding:
                    var isComplete = e.TorrentManager.Complete && !e.TorrentManager.IsInitialSeeding;
                    if (isComplete)
                    {
                        this.OnTorrentEvent(TorrentCompleted);
                    }
                    break;
                case TorrentState.Hashing:
                    break;
                case TorrentState.Stopping:
                    this.OnTorrentEvent(TorrentPaused);
                    break;
                case TorrentState.Error:
                    this.OnTorrentEvent(TorrentError);
                    break;
                case TorrentState.Metadata:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}