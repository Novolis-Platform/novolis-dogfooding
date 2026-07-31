using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Controls;

namespace TorrentLab;

internal sealed class MainWindow : Window
{
    readonly TorrentSessionPanel _session = new();
    readonly TextBlock _hint;

    public MainWindow()
    {
        Title = "Novolis Torrent Lab";
        Width = 720;
        Height = 560;
        MinWidth = 520;
        MinHeight = 420;

        _hint = new TextBlock
        {
            Text =
                "Dogfood for Novolis.Transports.Torrent + Avalonia torrent controls. " +
                "Sample: Tiny Core Linux Core-current.iso (~18 MB). Run once with --smoke to create the .torrent and verify local transfer.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var sampleBtn = new Button
        {
            Content = "Load Core sample…",
            Padding = new Thickness(10, 4),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        sampleBtn.Click += (_, _) => TryLoadSample();

        Content = new Border
        {
            Padding = new Thickness(16),
            Background = new SolidColorBrush(Color.Parse("#121212")),
            Child = new StackPanel
            {
                Spacing = 8,
                Children = { _hint, sampleBtn, _session }
            }
        };

        Closed += (_, _) => _session.Dispose();

        // Auto-offer sample if present next to the app or under samples/
        TryLoadSample(quiet: true);
    }

    void TryLoadSample(bool quiet = false)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "samples", "Core-current.iso.torrent"),
            Path.Combine(AppContext.BaseDirectory, "samples", "TinyCore-current.iso.torrent"),
            Path.Combine(AppContext.BaseDirectory, "Core-current.iso.torrent"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "samples", "Core-current.iso.torrent")),
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            if (_session.TryLoadTorrent(path))
            {
                _hint.Text = $"Sample loaded: {path}";
                return;
            }
        }

        if (!quiet)
            _hint.Text = "Sample torrent not found. Run with --smoke once to create Core-current.iso.torrent, or Open .torrent…";
    }
}
