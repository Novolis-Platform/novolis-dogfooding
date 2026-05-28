using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MeshBench.Models;
using MeshBench.Services;
using Novolis.Avalonia.Rendering;
using Novolis.Timeline;
using Novolis.Timeline.Presentation;

namespace MeshBench;

internal sealed class MainWindow : Window
{
    private readonly MeshBenchSession _session;
    private readonly MeshSceneStore _scenes;
    private readonly PathTraceViewport _viewport;
    private readonly SavePointSound _sound;

    private readonly Rgba32FrameControl _frame = new();
    private readonly ListBox _timelineList = new() { Width = 260 };
    private readonly ListBox _partsList = new() { Width = 280 };
    private readonly TextBlock _status = new() { Margin = new Thickness(8, 4) };
    private readonly TextBlock _workspacePath = new() { Opacity = 0.7, FontSize = 11 };

    private MeshPartRecord? _selectedPart;
    private TimelineTreeRow? _selectedTimelineRow;
    private IReadOnlyList<TimelineTreeRow> _timelineRows = [];
    private bool _dragging;
    private Point _lastPointer;
    private DispatcherTimer? _renderTimer;

    public MainWindow(MeshBenchSession session, MeshSceneStore scenes, PathTraceViewport viewport, SavePointSound sound)
    {
        _session = session;
        _scenes = scenes;
        _viewport = viewport;
        _sound = sound;

        Title = "MeshBench — Novolis CAD dogfood";
        Width = 1400;
        Height = 900;
        Content = BuildLayout();
        Opened += OnOpened;
        KeyDown += OnKeyDown;
    }

    private Control BuildLayout()
    {
        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8),
        };

        toolbar.Children.Add(Button("Save point", OnSavePoint));
        toolbar.Children.Add(Button("Restore", OnRestore));
        toolbar.Children.Add(Button("Branch", OnBranch));
        toolbar.Children.Add(new Border { Width = 12 });
        toolbar.Children.Add(Button("Add box", OnAddBox));
        toolbar.Children.Add(Button("Add sphere", OnAddSphere));
        toolbar.Children.Add(Button("Delete", OnDeletePart));

        _frame.PointerPressed += OnViewportPointerPressed;
        _frame.PointerReleased += (_, _) => _dragging = false;
        _frame.PointerMoved += OnViewportPointerMoved;
        _frame.PointerWheelChanged += OnViewportWheel;

        _timelineList.SelectionChanged += (_, _) =>
        {
            var index = _timelineList.SelectedIndex;
            _selectedTimelineRow = index >= 0 && index < _timelineRows.Count ? _timelineRows[index] : null;
        };

        _partsList.SelectionChanged += (_, _) =>
        {
            _selectedPart = _partsList.SelectedItem as MeshPartRecord;
            RebuildViewport();
        };

        var viewportPanel = new DockPanel();
        DockPanel.SetDock(_status, Dock.Bottom);
        viewportPanel.Children.Add(_status);
        viewportPanel.Children.Add(_frame);

        var center = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
        };
        center.Children.Add(toolbar);
        Grid.SetRow(viewportPanel, 1);
        center.Children.Add(viewportPanel);

        var root = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("260,*,280"),
        };

        var timelinePanel = new DockPanel();
        var timelineHeader = new TextBlock { Text = "Timeline", FontWeight = FontWeight.SemiBold, Margin = new Thickness(8, 8, 8, 4) };
        DockPanel.SetDock(timelineHeader, Dock.Top);
        DockPanel.SetDock(_workspacePath, Dock.Bottom);
        _workspacePath.Margin = new Thickness(8);
        timelinePanel.Children.Add(timelineHeader);
        timelinePanel.Children.Add(_workspacePath);
        timelinePanel.Children.Add(_timelineList);

        var partsPanel = new DockPanel();
        var partsHeader = new TextBlock { Text = "Meshes", FontWeight = FontWeight.SemiBold, Margin = new Thickness(8, 8, 8, 4) };
        DockPanel.SetDock(partsHeader, Dock.Top);
        partsPanel.Children.Add(partsHeader);
        partsPanel.Children.Add(_partsList);

        root.Children.Add(timelinePanel);
        Grid.SetColumn(center, 1);
        root.Children.Add(center);
        Grid.SetColumn(partsPanel, 2);
        root.Children.Add(partsPanel);

        return root;
    }

    private static Button Button(string text, EventHandler<RoutedEventArgs> click)
    {
        var button = new Button { Content = text };
        button.Click += click;
        return button;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        _viewport.Attach(_frame);
        await _session.OpenOrCreateDefaultAsync();
        _workspacePath.Text = _session.Workspace?.Root.FullName ?? string.Empty;
        SyncCameraFromScene();
        RefreshUi();
        StartRenderLoop();
    }

    private void StartRenderLoop()
    {
        _renderTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) =>
        {
            _viewport.Tick();
            _status.Text = $"Samples: {_viewport.DisplayedSamples}  |  Drag to orbit, wheel to zoom, Ctrl+S save point";
        });
        _renderTimer.Start();
    }

    private void RefreshUi()
    {
        _partsList.ItemsSource = _session.Scene.Parts.ToList();
        _timelineRows = _session.TimelineRows;
        _timelineList.ItemsSource = _timelineRows
            .Select(r => $"{new string(' ', r.Depth * 2)}{r.Label} [{r.Branch}]{(r.IsHead ? " *" : string.Empty)}")
            .ToList();
        RebuildViewport();
    }

    private void RebuildViewport()
    {
        _session.Scene.Camera = _viewport.CaptureCameraState();
        _viewport.ApplyCameraState(_session.Scene.Camera);
        _viewport.SetScene(_scenes.Compile(_session.Scene));
    }

    private void SyncCameraFromScene()
    {
        _viewport.ApplyCameraState(_session.Scene.Camera);
        var size = _frame.Bounds.Size;
        if (size.Width > 0 && size.Height > 0)
            _viewport.Resize((int)size.Width, (int)size.Height);
    }

    private async void OnSavePoint(object? sender, RoutedEventArgs e)
    {
        _session.Scene.Camera = _viewport.CaptureCameraState();
        _session.PersistWorkingCopy();
        await _session.SavePointAsync("Manual save");
        _sound.Play();
        RefreshUi();
    }

    private async void OnRestore(object? sender, RoutedEventArgs e)
    {
        if (_selectedTimelineRow is null)
            return;

        await _session.RestoreAsync(_selectedTimelineRow.Id);
        SyncCameraFromScene();
        RefreshUi();
    }

    private async void OnBranch(object? sender, RoutedEventArgs e)
    {
        if (_selectedTimelineRow is null)
            return;

        await _session.BranchAsync(_selectedTimelineRow.Id, $"branch-{_session.TimelineRows.Count}");
        RefreshUi();
    }

    private void OnAddBox(object? sender, RoutedEventArgs e)
    {
        _session.Scene.Parts.Add(new MeshPartRecord
        {
            Kind = "box",
            Center = [Random.Shared.NextSingle() * 0.6f, 0.5f, Random.Shared.NextSingle() * 0.6f],
            HalfExtents = [0.35f, 0.35f, 0.35f],
            Color = [Random.Shared.NextSingle() * 0.5f + 0.4f, 0.4f, 0.35f],
        });
        RebuildViewport();
        RefreshUi();
    }

    private void OnAddSphere(object? sender, RoutedEventArgs e)
    {
        _session.Scene.Parts.Add(new MeshPartRecord
        {
            Kind = "sphere",
            Center = [Random.Shared.NextSingle() * 1.2f - 0.2f, 0.55f, Random.Shared.NextSingle() * 1.2f - 0.2f],
            Radius = 0.3f + Random.Shared.NextSingle() * 0.25f,
            Color = [0.3f, Random.Shared.NextSingle() * 0.5f + 0.4f, 0.75f],
        });
        RebuildViewport();
        RefreshUi();
    }

    private void OnDeletePart(object? sender, RoutedEventArgs e)
    {
        if (_selectedPart is null)
            return;

        _session.Scene.Parts.Remove(_selectedPart);
        _selectedPart = null;
        RebuildViewport();
        RefreshUi();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            OnSavePoint(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void OnViewportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragging = true;
        _lastPointer = e.GetPosition(_frame);
    }

    private void OnViewportPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging)
            return;

        var pos = e.GetPosition(_frame);
        var delta = pos - _lastPointer;
        _lastPointer = pos;
        _viewport.Orbit.AddLookDelta((float)delta.X * 0.008f, (float)delta.Y * -0.008f);
        _viewport.ResetAccumulation();
    }

    private void OnViewportWheel(object? sender, PointerWheelEventArgs e)
    {
        _viewport.Orbit.AdjustDistance((float)-e.Delta.Y * 0.15f);
        _viewport.ResetAccumulation();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        var size = _frame.Bounds.Size;
        if (size.Width > 0 && size.Height > 0)
            _viewport.Resize((int)size.Width, (int)size.Height);
    }
}
