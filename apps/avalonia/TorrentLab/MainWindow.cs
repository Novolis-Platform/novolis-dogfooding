using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Controls;

namespace TorrentLab;

internal sealed class MainWindow : Window
{
    readonly TorrentSessionPanel _session = new();
    readonly TextBlock _subtitle;

    public MainWindow()
    {
        Title = "Novolis Torrent Lab";
        Width = 960;
        Height = 640;
        MinWidth = 720;
        MinHeight = 480;
        Background = new SolidColorBrush(Color.Parse("#0E141B"));

        _subtitle = new TextBlock
        {
            Text = "Tiny Core sample ready — Start to check local data, or Add torrent… for another file.",
            FontSize = 12,
            Opacity = 0.75,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };

        var sampleBtn = new Button
        {
            Content = "Load Core sample",
            Padding = new Thickness(12, 6),
            MinHeight = 30
        };
        sampleBtn.Click += (_, _) => TryLoadSample();

        var header = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#182230")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2E3A4A")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 12),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Children =
                {
                    new StackPanel
                    {
                        Spacing = 2,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Transfers",
                                FontSize = 18,
                                FontWeight = FontWeight.SemiBold
                            },
                            _subtitle
                        }
                    },
                    Col(sampleBtn, 1)
                }
            }
        };

        Content = new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                DockTop(header),
                _session
            }
        };

        Closed += (_, _) => _session.Dispose();
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
                _subtitle.Text = $"Sample loaded · {Path.GetFileName(path)} — press Start to begin.";
                return;
            }
        }

        if (!quiet)
            _subtitle.Text = "Sample torrent not found. Run with --smoke once, or Add torrent…";
    }

    static Control DockTop(Control control)
    {
        DockPanel.SetDock(control, Dock.Top);
        return control;
    }

    static Control Col(Control c, int column)
    {
        Grid.SetColumn(c, column);
        c.VerticalAlignment = VerticalAlignment.Center;
        return c;
    }
}
