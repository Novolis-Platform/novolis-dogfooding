using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MeshBench.Models;
using MeshBench.Services;
using MeshBench.Ui;
using Novolis.Avalonia.Rendering;
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
    private readonly ListBox _partsList = new();
    private readonly PartInspectorPanel _inspector = new();
    private readonly TextBlock _status = new() { Margin = new Thickness(8, 4) };
    private readonly TextBlock _workspacePath = new() { Opacity = 0.7, FontSize = 11 };

    private MeshPartRecord? _selectedPart;
    private TimelineTreeRow? _selectedTimelineRow;
    private IReadOnlyList<TimelineTreeRow> _timelineRows = [];
    private bool _orbiting;
    private bool _movingPart;
    private int _saveInFlight;
    private int _boxCounter;
    private int _sphereCounter;
    private Point _lastPointer;
    private DispatcherTimer? _renderTimer;

    public MainWindow(MeshBenchSession session, MeshSceneStore scenes, PathTraceViewport viewport, SavePointSound sound)
    {
        _session = session;
        _scenes = scenes;
        _viewport = viewport;
        _sound = sound;

        Title = "Mesh Studio";
        Width = 1480;
        Height = 920;
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

        toolbar.Children.Add(Button("Save", OnSavePoint, "Ctrl+S"));
        toolbar.Children.Add(Button("Restore", OnRestore));
        toolbar.Children.Add(Button("Branch", OnBranch));
        toolbar.Children.Add(Separator());
        toolbar.Children.Add(Button("Box", OnAddBox, "B"));
        toolbar.Children.Add(Button("Sphere", OnAddSphere, "S"));
        toolbar.Children.Add(Button("Duplicate", OnDuplicate, "Ctrl+D"));
        toolbar.Children.Add(Button("Delete", OnDeletePart, "Del"));
        toolbar.Children.Add(Separator());
        toolbar.Children.Add(Button("Fit view", OnFitView, "F"));

        _frame.PointerPressed += OnViewportPointerPressed;
        _frame.PointerReleased += OnViewportPointerReleased;
        _frame.PointerMoved += OnViewportPointerMoved;
        _frame.PointerWheelChanged += OnViewportWheel;

        _timelineList.ItemTemplate = new FuncDataTemplate<TimelineTreeRow>((row, _) =>
            new TextBlock
            {
                Text = row is null
                    ? string.Empty
                    : $"{new string(' ', row.Depth * 2)}{row.Label} [{row.Branch}]{(row.IsHead ? "  ●" : string.Empty)}",
                FontFamily = "Consolas,Cascadia Mono,monospace",
                FontSize = 12,
            },
            supportsRecycling: true);

        _timelineList.SelectionChanged += (_, _) =>
            _selectedTimelineRow = _timelineList.SelectedItem as TimelineTreeRow;

        _partsList.ItemTemplate = new FuncDataTemplate<MeshPartRecord>((part, _) =>
            new TextBlock { Text = part?.Summary ?? string.Empty },
            supportsRecycling: true);

        _partsList.SelectionChanged += (_, _) =>
        {
            _selectedPart = _partsList.SelectedItem as MeshPartRecord;
            _inspector.Bind(_selectedPart);
            RebuildViewport();
        };

        _inspector.PartChanged += (_, _) => OnSceneEdited();

        var viewportPanel = new DockPanel();
        DockPanel.SetDock(_status, Dock.Bottom);
        viewportPanel.Children.Add(_status);
        viewportPanel.Children.Add(_frame);

        var center = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        center.Children.Add(toolbar);
        Grid.SetRow(viewportPanel, 1);
        center.Children.Add(viewportPanel);

        var right = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        var meshesPanel = new DockPanel();
        var meshesHeader = new TextBlock
        {
            Text = "Meshes",
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(8, 8, 8, 4),
        };
        DockPanel.SetDock(meshesHeader, Dock.Top);
        meshesPanel.Children.Add(meshesHeader);
        meshesPanel.Children.Add(_partsList);
        Grid.SetRow(meshesPanel, 0);
        right.Children.Add(meshesPanel);

        var inspectorHost = new DockPanel();
        var inspectorHeader = new TextBlock
        {
            Text = "Inspector",
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(8, 8, 8, 4),
        };
        DockPanel.SetDock(inspectorHeader, Dock.Top);
        inspectorHost.Children.Add(inspectorHeader);
        inspectorHost.Children.Add(_inspector);
        Grid.SetRow(inspectorHost, 1);
        right.Children.Add(inspectorHost);

        var timelinePanel = new DockPanel();
        var timelineHeader = new TextBlock
        {
            Text = "Timeline",
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(8, 8, 8, 4),
        };
        DockPanel.SetDock(timelineHeader, Dock.Top);
        DockPanel.SetDock(_workspacePath, Dock.Bottom);
        _workspacePath.Margin = new Thickness(8);
        timelinePanel.Children.Add(timelineHeader);
        timelinePanel.Children.Add(_workspacePath);
        timelinePanel.Children.Add(_timelineList);

        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("260,*,320") };
        Grid.SetColumn(timelinePanel, 0);
        root.Children.Add(timelinePanel);
        Grid.SetColumn(center, 1);
        root.Children.Add(center);
        Grid.SetColumn(right, 2);
        root.Children.Add(right);

        return root;
    }

    private static Control Separator() => new Border { Width = 8 };

    private static Button Button(string text, EventHandler<RoutedEventArgs> click, string? tooltip = null)
    {
        var button = new Button { Content = text };
        if (tooltip is not null)
            ToolTip.SetTip(button, tooltip);
        button.Click += click;
        return button;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        _viewport.Attach(_frame);
        await _session.OpenOrCreateDefaultAsync();
        _workspacePath.Text = _session.Workspace?.Root.FullName ?? string.Empty;
        _boxCounter = _session.Scene.Parts.Count(p => p.Kind.Equals("box", StringComparison.OrdinalIgnoreCase));
        _sphereCounter = _session.Scene.Parts.Count(p => p.Kind.Equals("sphere", StringComparison.OrdinalIgnoreCase));
        SyncCameraFromScene();
        RefreshUi();
        StartRenderLoop();
    }

    private void StartRenderLoop()
    {
        _renderTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) =>
        {
            _viewport.Tick();
            UpdateStatus();
        });
        _renderTimer.Start();
    }

    private void UpdateStatus()
    {
        var dirty = _session.IsDirty ? "  ● unsaved" : string.Empty;
        var partCount = _session.Scene.Parts.Count;
        var hint = _movingPart
            ? "Shift+drag: move  |  drag: orbit  |  wheel: zoom"
            : "Shift+drag moves selection  |  Ctrl+S save";
        _status.Text = $"{partCount} mesh{(partCount == 1 ? string.Empty : "es")}{dirty}  |  Samples {_viewport.DisplayedSamples}  |  {hint}";
    }

    private void RefreshUi()
    {
        _partsList.ItemsSource = null;
        _partsList.ItemsSource = _session.Scene.Parts.ToList();
        _timelineRows = _session.TimelineRows;
        _timelineList.ItemsSource = _timelineRows;
        if (_selectedPart is not null && !_session.Scene.Parts.Contains(_selectedPart))
            _selectedPart = null;
        _inspector.Bind(_selectedPart);
        RebuildViewport();
        UpdateStatus();
    }

    private void OnSceneEdited()
    {
        _session.MarkDirty();
        RebuildViewport();
        if (_selectedPart is not null)
            _partsList.ItemsSource = _session.Scene.Parts.ToList();
        UpdateStatus();
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

    private void OnFitView(object? sender, RoutedEventArgs e)
    {
        var (center, radius) = SceneBounds.Compute(_session.Scene);
        _viewport.FitToBounds(center, radius);
        _session.Scene.Camera = _viewport.CaptureCameraState();
        OnSceneEdited();
    }

    private async void OnSavePoint(object? sender, RoutedEventArgs e)
    {
        if (Interlocked.CompareExchange(ref _saveInFlight, 1, 0) != 0)
            return;

        try
        {
            _inspector.Bind(_selectedPart);
            _session.Scene.Camera = _viewport.CaptureCameraState();
            await _session.SavePointAsync("Manual save");
            _sound.Play();
            RefreshUi();
        }
        finally
        {
            Interlocked.Exchange(ref _saveInFlight, 0);
        }
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
        _boxCounter++;
        var part = new MeshPartRecord
        {
            Name = $"Box {_boxCounter}",
            Kind = "box",
            Center = [Random.Shared.NextSingle() * 0.8f - 0.4f, 0.5f, Random.Shared.NextSingle() * 0.8f - 0.4f],
            HalfExtents = [0.35f, 0.35f, 0.35f],
            Color = [Random.Shared.NextSingle() * 0.4f + 0.45f, Random.Shared.NextSingle() * 0.35f + 0.35f, 0.32f],
        };
        _session.Scene.Parts.Add(part);
        _selectedPart = part;
        OnSceneEdited();
        RefreshUi();
        _partsList.SelectedItem = part;
        _inspector.Bind(part);
    }

    private void OnAddSphere(object? sender, RoutedEventArgs e)
    {
        _sphereCounter++;
        var part = new MeshPartRecord
        {
            Name = $"Sphere {_sphereCounter}",
            Kind = "sphere",
            Center = [Random.Shared.NextSingle() * 1.2f - 0.2f, 0.55f, Random.Shared.NextSingle() * 1.2f - 0.2f],
            Radius = 0.3f + Random.Shared.NextSingle() * 0.25f,
            Color = [0.3f, Random.Shared.NextSingle() * 0.45f + 0.4f, 0.75f],
        };
        _session.Scene.Parts.Add(part);
        _selectedPart = part;
        OnSceneEdited();
        RefreshUi();
        _partsList.SelectedItem = part;
        _inspector.Bind(part);
    }

    private void OnDuplicate(object? sender, RoutedEventArgs e)
    {
        if (_selectedPart is null)
            return;

        var copy = _selectedPart.Clone();
        copy.Center[0] += 0.35f;
        _session.Scene.Parts.Add(copy);
        _selectedPart = copy;
        _partsList.SelectedItem = copy;
        _inspector.Bind(copy);
        OnSceneEdited();
        RefreshUi();
    }

    private void OnDeletePart(object? sender, RoutedEventArgs e)
    {
        if (_selectedPart is null)
            return;

        _session.Scene.Parts.Remove(_selectedPart);
        _selectedPart = null;
        _inspector.Bind(null);
        OnSceneEdited();
        RefreshUi();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.S)
        {
            OnSavePoint(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.D)
        {
            OnDuplicate(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Delete:
                OnDeletePart(sender, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.F:
                OnFitView(sender, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.B:
                OnAddBox(sender, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.S when !e.KeyModifiers.HasFlag(KeyModifiers.Control):
                OnAddSphere(sender, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
    }

    private void OnViewportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _lastPointer = e.GetPosition(_frame);
        _movingPart = e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _selectedPart is not null;
        _orbiting = !_movingPart;
    }

    private void OnViewportPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _orbiting = false;
        _movingPart = false;
    }

    private void OnViewportPointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(_frame);
        var delta = pos - _lastPointer;
        _lastPointer = pos;

        if (_movingPart && _selectedPart is not null)
        {
            var scale = _viewport.Orbit.Distance * 0.0025f;
            _selectedPart.Center[0] += (float)delta.X * scale;
            _selectedPart.Center[2] -= (float)delta.Y * scale;
            _inspector.Bind(_selectedPart);
            OnSceneEdited();
            return;
        }

        if (!_orbiting)
            return;

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
