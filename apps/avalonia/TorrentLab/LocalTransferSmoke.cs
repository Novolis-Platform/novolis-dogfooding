using System.Net;
using Novolis.Transports.Torrent;

namespace TorrentLab;

/// <summary>Creates the Tiny Core sample torrent and proves local seed hashing (+ best-effort leech).</summary>
internal static class LocalTransferSmoke
{
    public static int Run(string samplesDir)
    {
        var iso = Path.Combine(samplesDir, "Core-current.iso");
        var torrentPath = Path.Combine(samplesDir, "Core-current.iso.torrent");
        if (!File.Exists(iso))
        {
            Console.Error.WriteLine($"Missing ISO: {iso}");
            Console.Error.WriteLine("Download: http://tinycorelinux.net/15.x/x86/release/Core-current.iso");
            return 2;
        }

        Console.WriteLine($"Creating torrent from {iso} ({new FileInfo(iso).Length} bytes)…");
        var torrent = TorrentCreator.CreateSingleFile(iso, torrentPath);
        Console.WriteLine($"Torrent {torrent.InfoHash} · {torrent.PiecesCount} pieces · {torrentPath}");

        var root = Path.Combine(Path.GetTempPath(), "NovolisTorrentSmoke", Guid.NewGuid().ToString("N"));
        var seedDir = Path.Combine(root, "seed");
        var leechDir = Path.Combine(root, "leech");
        Directory.CreateDirectory(seedDir);
        Directory.CreateDirectory(leechDir);
        File.Copy(iso, Path.Combine(seedDir, Path.GetFileName(iso)), overwrite: true);

        const int seedPort = 51980;
        const int leechPort = 51981;

        try
        {
            using var seeder = new TorrentClient(seedPort, seedDir);
            seeder.Start();
            seeder.Start(torrent);
            Console.WriteLine("Seeder hashing…");

            var seedReady = SpinWait.SpinUntil(() =>
            {
                var p = seeder.GetProgressInfo(torrent.InfoHash);
                return p is { CompletedPercentage: >= 99.9m };
            }, TimeSpan.FromMinutes(2));

            if (!seedReady)
            {
                Console.Error.WriteLine("Seeder did not finish hashing in time.");
                return 3;
            }

            Console.WriteLine("Seeder at 100% — sample torrent is ready for TorrentLab UI.");

            using var leecher = new TorrentClient(leechPort, leechDir);
            leecher.Start();
            leecher.Start(torrent);
            Thread.Sleep(500);
            leecher.AddPeer(torrent.InfoHash, new IPEndPoint(IPAddress.Loopback, seedPort));
            Console.WriteLine("Leecher AddPeer(127.0.0.1) — waiting up to 120s…");

            var done = SpinWait.SpinUntil(() =>
            {
                var p = leecher.GetProgressInfo(torrent.InfoHash);
                if (p is null) return false;
                if (Environment.TickCount64 / 500 % 4 == 0)
                    Console.WriteLine($"  leech {p.CompletedPercentage:0.0}% ↓{p.DownloadSpeed:0} B/s seeders={p.SeederCount}");
                return p.CompletedPercentage >= 99.9m;
            }, TimeSpan.FromSeconds(120));

            if (done)
            {
                var downloaded = Path.Combine(leechDir, Path.GetFileName(iso));
                Console.WriteLine($"OK — full local transfer to {downloaded}");
            }
            else
            {
                var p = leecher.GetProgressInfo(torrent.InfoHash);
                Console.WriteLine(
                    $"Partial leech {p?.CompletedPercentage:0.0}% (peer wire still evolving). UI can still load {torrentPath}.");
            }

            return 0;
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}
