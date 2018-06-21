using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TACTracker.Console.Test
{
    using TACBitTorrent;

    public static class Program
    {
        private static void Main(string[] args)
        {
            var tracker = TorrentHelper.RunTracker();

            //var torrent = TorrentHelper.CreateTorrent(@"C:\Windows\Temp\toTorrent\a", @"C:\Windows\Temp\torrents");

            //var testTorrentUri = torrent.TorrentFileUri;

            //tracker.AddTorrent(TorrentHelper.LoadTorrent(@"C:\Windows\Temp\torrents\tmp947A.torrent"));

            //tracker.AddTorrent(@"C:\Windows\Temp\torrents\tmp947A.torrent");

            // tracker.AddTorrent(@"C:\Windows\Temp\torrents\tmp947A.torrent");


            System.Console.ReadLine();
            tracker.Stop();
        }
    }
}