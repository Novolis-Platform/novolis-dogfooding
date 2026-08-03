using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Novolis.Avalonia.Video;
using Novolis.Video.Edit;

namespace MovieMakerLab;

internal sealed class MainWindow : Window
{
    static readonly Color Pane = Color.FromRgb(22, 32, 48);
    static readonly Color PaneAlt = Color.FromRgb(30, 44, 62);
    static readonly Color Accent = Color.FromRgb(40, 140, 150);
    static readonly Color Amber = Color.FromRgb(220, 150, 60);

    readonly MovieProject _project = new("Movie Maker Lab", 640, 360);
    readonly EditTransport _transport = new();
    readonly DecodedStillCache _stills = new();
    readonly MoviePreviewComposer _composer;
    readonly VideoSurface _preview = new() { MinHeight = 240 };
    readonly StoryboardStrip _storyboard = new() { MinHeight = 80, PixelsPerSecond = 56 };
    readonly ListBox _collections = new();
    readonly TextBlock _status = new();
    readonly MoviePreviewSession _session;
    Guid? _selectedClipId;
    int _colorIndex;

    static readonly Rgba8[] DemoColors =
    [
        new(24, 90, 130),
        new(40, 120, 90),
        new(140, 70, 50),
        new(90, 60, 130),
        new(40, 40, 48),
    ];

    public MainWindow()
    {
        Title = "Movie Maker Lab";
        Width = 1180;
        Height = 760;
        Background = new SolidColorBrush(Pane);

        _composer = new MoviePreviewComposer(_stills);
        SeedDemo();

        _storyboard.Bind(_project);
        _storyboard.SeekRequested += t => _transport.Seek(t);
        _storyboard.ClipSelected += id =>
        {
            _selectedClipId = id;
            _storyboard.SetSelectedClip(id);
        };

        _transport.Changed += RefreshStatus;
        _session = new MoviePreviewSession(_project, _transport, _composer, _preview, _storyboard);
        _session.Start();
        Closed += (_, _) =>
        {
            _transport.Changed -= RefreshStatus;
            _session.Dispose();
        };

        Content = BuildChrome();
        RefreshCollections();
        RefreshStatus();
    }

    void SeedDemo()
    {
        var intro = MovieEditOps.AddColorCard(_project, "Intro card", DemoColors[0], TimeSpan.FromSeconds(3));
        var mid = MovieEditOps.AddColorCard(_project, "Mid card", DemoColors[1], TimeSpan.FromSeconds(2));
        var end = MovieEditOps.AddColorCard(_project, "End card", DemoColors[2], TimeSpan.FromSeconds(2));
        MovieEditOps.AppendToStoryboard(_project, intro);
        MovieEditOps.AppendToStoryboard(_project, mid);
        MovieEditOps.AppendToStoryboard(_project, end);
        _colorIndex = 3;
    }

    Control BuildChrome()
    {
        var tasks = BuildTasksPane();
        var collections = BuildCollectionsPane();
        var preview = BuildPreviewPane();
        var storyboard = BuildStoryboardPane();

        var top = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(220)),
                new ColumnDefinition(new GridLength(280)),
                new ColumnDefinition(GridLength.Star),
            ],
            RowDefinitions = [new RowDefinition(GridLength.Star)],
            Margin = new Thickness(8),
        };
        Grid.SetColumn(tasks, 0);
        Grid.SetColumn(collections, 1);
        Grid.SetColumn(preview, 2);
        top.Children.Add(tasks);
        top.Children.Add(collections);
        top.Children.Add(preview);

        return new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                new Border
                {
                    [DockPanel.DockProperty] = Dock.Top,
                    Background = new SolidColorBrush(PaneAlt),
                    Padding = new Thickness(14, 10),
                    Child = new TextBlock
                    {
                        Text = "Movie Maker Lab",
                        FontSize = 22,
                        FontFamily = new FontFamily("Segoe UI Semibold"),
                        Foreground = Brushes.White,
                    },
                },
                new Border
                {
                    [DockPanel.DockProperty] = Dock.Bottom,
                    Background = new SolidColorBrush(PaneAlt),
                    Padding = new Thickness(12, 6),
                    Child = _status,
                },
                new Border
                {
                    [DockPanel.DockProperty] = Dock.Bottom,
                    Margin = new Thickness(8, 0, 8, 8),
                    Child = storyboard,
                },
                top,
            },
        };
    }

    Control BuildTasksPane()
    {
        Button TaskButton(string label, Action action)
        {
            var b = new Button
            {
                Content = label,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10, 8),
                Background = new SolidColorBrush(Accent),
                Foreground = Brushes.White,
            };
            b.Click += (_, _) => action();
            return b;
        }

        return PaneBox("Tasks", new StackPanel
        {
            Children =
            {
                TaskButton("Import pictures…", ImportPicturesAsync),
                TaskButton("Make color card", AddColorCard),
                TaskButton("Add to storyboard", AddSelectedToStoryboard),
                TaskButton("Split at playhead", SplitAtPlayhead),
                TaskButton("Remove clip", RemoveSelectedClip),
                TaskButton("Play / Pause", () => _transport.Toggle()),
                TaskButton("Rewind", () => _transport.Seek(TimeSpan.Zero)),
            },
        });
    }

    Control BuildCollectionsPane()
    {
        _collections.SelectionChanged += (_, _) => RefreshStatus();
        return PaneBox("Collections", _collections);
    }

    Control BuildPreviewPane()
    {
        _preview.Label = "Preview";
        return PaneBox("Monitor", new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                new StackPanel
                {
                    [DockPanel.DockProperty] = Dock.Bottom,
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Margin = new Thickness(0, 8, 0, 0),
                    Children =
                    {
                        TransportButton("◀◀", () => _transport.Seek(TimeSpan.Zero)),
                        TransportButton("▶ / ❚❚", () => _transport.Toggle()),
                    },
                },
                new Border
                {
                    Background = Brushes.Black,
                    Child = _preview,
                },
            },
        });
    }

    Control BuildStoryboardPane() =>
        PaneBox("Storyboard", new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = _storyboard,
        });

    static Button TransportButton(string label, Action action)
    {
        var b = new Button
        {
            Content = label,
            Padding = new Thickness(14, 6),
            Background = new SolidColorBrush(Amber),
            Foreground = Brushes.Black,
        };
        b.Click += (_, _) => action();
        return b;
    }

    Border PaneBox(string title, Control body) =>
        new()
        {
            Background = new SolidColorBrush(PaneAlt),
            BorderBrush = new SolidColorBrush(Color.FromRgb(55, 75, 95)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(4),
            Child = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontFamily = new FontFamily("Segoe UI Semibold"),
                        Foreground = new SolidColorBrush(Accent),
                        Margin = new Thickness(0, 0, 0, 8),
                        [DockPanel.DockProperty] = Dock.Top,
                    },
                    body,
                },
            },
        };

    async void ImportPicturesAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import pictures",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Images")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"],
                },
            ],
        });

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path is null)
                continue;

            var name = Path.GetFileNameWithoutExtension(path);
            var asset = MovieEditOps.AddImage(_project, name, path, TimeSpan.FromSeconds(3));
            try
            {
                var frame = AvaloniaStillLoader.LoadBgra(path, _project.Width, _project.Height);
                _stills.SetStill(asset.Id, frame);
            }
            catch (Exception ex)
            {
                _status.Text = $"Import failed for {name}: {ex.Message}";
                continue;
            }

            MovieEditOps.AppendToStoryboard(_project, asset);
        }

        RefreshUi();
    }

    void AddColorCard()
    {
        var color = DemoColors[_colorIndex % DemoColors.Length];
        _colorIndex++;
        var asset = MovieEditOps.AddColorCard(
            _project,
            $"Color {_colorIndex}",
            color,
            TimeSpan.FromSeconds(2));
        MovieEditOps.AppendToStoryboard(_project, asset);
        RefreshUi();
    }

    void AddSelectedToStoryboard()
    {
        if (_collections.SelectedItem is not AssetRow row)
            return;
        var asset = _project.FindAsset(row.Id);
        if (asset is null)
            return;
        MovieEditOps.AppendToStoryboard(_project, asset);
        RefreshUi();
    }

    void SplitAtPlayhead()
    {
        MovieEditOps.SplitAt(_project, _transport.Position);
        RefreshUi();
    }

    void RemoveSelectedClip()
    {
        if (_selectedClipId is not { } id)
            return;
        MovieEditOps.RemoveClip(_project, id);
        _selectedClipId = null;
        _storyboard.SetSelectedClip(null);
        RefreshUi();
    }

    void RefreshUi()
    {
        _storyboard.Bind(_project);
        _storyboard.SetSelectedClip(_selectedClipId);
        _session.Refresh();
        RefreshCollections();
        RefreshStatus();
    }

    void RefreshCollections()
    {
        _collections.ItemsSource = _project.Assets
            .Select(a => new AssetRow(a.Id, $"{a.Kind}: {a.Name} ({a.Duration.TotalSeconds:0.#}s)"))
            .ToList();
    }

    void RefreshStatus()
    {
        var dur = StoryboardQuery.TotalDuration(_project);
        _status.Text =
            $"Clips {_project.Clips.Count} · Assets {_project.Assets.Count} · " +
            $"Playhead {_transport.Position:mm\\:ss\\.f} / {dur:mm\\:ss\\.f}" +
            (_transport.IsPlaying ? " · Playing" : " · Paused");
        _status.Foreground = Brushes.White;
    }

    sealed record AssetRow(Guid Id, string Label)
    {
        public override string ToString() => Label;
    }
}
