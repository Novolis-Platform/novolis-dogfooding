using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Novolis.Avalonia.Controls;
using Novolis.Avalonia.Layout;
using Novolis.Transports.WireFish;
using WireFishViewer.Capture;

namespace WireFishViewer;

internal sealed class MainWindow : Window
{
    private readonly IPacketStore _store;
    private readonly CaptureSessionService _capture;
    private readonly PacketTableView _packetTable;
    private readonly TreeDetailsView _treeDetails;
    private readonly HexDumpView _hexDump;
    private readonly AnalyzerWorkspace _workspace;
    private readonly ComboBox _interfaceCombo;
    private readonly Button _startButton;
    private readonly Button _stopButton;
    private readonly Button _startNpcapButton;
    private readonly TextBlock _warningBanner;
    private readonly DispatcherTimer _statusTimer;
    private bool _npcapOk = true;

    public MainWindow(IPacketStore store, CaptureSessionService capture)
    {
        _store = store;
        _capture = capture;
        _packetTable = CreatePacketTable();
        _treeDetails = new TreeDetailsView();
        _hexDump = new HexDumpView();
        _workspace = new AnalyzerWorkspace(_packetTable, _treeDetails, _hexDump);

        _interfaceCombo = new ComboBox { MinWidth = 280, PlaceholderText = "Select interface" };
        _startButton = new Button { Content = "Start" };
        _stopButton = new Button { Content = "Stop", IsEnabled = false };
        _startNpcapButton = new Button { Content = "Start Npcap" };
        _warningBanner = new TextBlock
        {
            IsVisible = false,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(8, 0, 8, 4),
        };

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _statusTimer.Tick += (_, _) => UpdateStatus();

        _startButton.Click += OnStartClicked;
        _stopButton.Click += OnStopClicked;
        _startNpcapButton.Click += OnStartNpcapClicked;
        _packetTable.SelectionChanged += OnPacketSelectionChanged;
        _workspace.FilterBar.ApplyRequested += OnFilterApplyRequested;

        _workspace.Toolbar.AddAction(_startButton);
        _workspace.Toolbar.AddAction(_stopButton);
        _workspace.Toolbar.AddAction(_startNpcapButton);
        _workspace.Toolbar.AddAction(_interfaceCombo);

        ConfigureWindow();
        PopulateInterfaces(preferBestDevice: true);
        UpdateStatus();

        var root = new DockPanel();
        DockPanel.SetDock(_warningBanner, Dock.Top);
        root.Children.Add(_warningBanner);
        root.Children.Add(_workspace);
        Content = root;

        _packetTable.ItemsSource = _store.Packets;
    }

    private static PacketTableView CreatePacketTable()
    {
        var table = new PacketTableView();
        // Sorting while capturing forces full re-binds; keep columns fixed for live capture.
        table.CanUserSortColumns = false;
        table.SetColumns(
        [
            PacketTableView.TextColumn("#", nameof(PacketRow.Number), 48),
            PacketTableView.TextColumn("Time", nameof(PacketRow.Time), 120),
            PacketTableView.TextColumn("Source", nameof(PacketRow.Source), 140),
            PacketTableView.TextColumn("Destination", nameof(PacketRow.Destination), 140),
            PacketTableView.TextColumn("Protocol", nameof(PacketRow.Protocol), 72),
            PacketTableView.TextColumn("Length", nameof(PacketRow.Length), 64),
            PacketTableView.TextColumn("Info", nameof(PacketRow.Info), 320),
        ]);
        return table;
    }

    private void ConfigureWindow()
    {
        Title = "WireFish Viewer";
        Width = 1280;
        Height = 800;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private void PopulateInterfaces(bool preferBestDevice = false)
    {
        var selectedKey = preferBestDevice
            ? null
            : (_interfaceCombo.SelectedItem as CaptureDeviceInfo)?.CaptureKey;
        _interfaceCombo.ItemsSource = CaptureDeviceCatalog.ListDevices();
        if (_interfaceCombo.ItemCount > 0)
        {
            var matchIndex = -1;
            if (selectedKey is not null)
            {
                for (var i = 0; i < _interfaceCombo.ItemCount; i++)
                {
                    if (_interfaceCombo.Items[i] is CaptureDeviceInfo info &&
                        string.Equals(info.CaptureKey, selectedKey, StringComparison.OrdinalIgnoreCase))
                    {
                        matchIndex = i;
                        break;
                    }
                }
            }

            _interfaceCombo.SelectedIndex = matchIndex >= 0 ? matchIndex : 0;
        }

        var health = CaptureDeviceCatalog.DriverHealth;
        _npcapOk = health.IsReady;
        var hasDevices = CaptureDeviceCatalog.HasCaptureDevices;
        _startNpcapButton.IsEnabled = !_capture.IsCapturing && WireFishCaptureHealthChecks.IsNpcapStopped();

        if (!health.IsReady)
        {
            _warningBanner.IsVisible = true;
            _warningBanner.Text = health.Message ?? "Capture driver is not ready.";
            _startButton.IsEnabled = !_capture.IsCapturing && hasDevices;
            return;
        }

        _warningBanner.IsVisible = !hasDevices;
        _warningBanner.Text = hasDevices
            ? string.Empty
            : "No capture devices found. Install Npcap (Windows) or libpcap, then restart. You can still explore the UI without live capture.";
        _startButton.IsEnabled = !_capture.IsCapturing && hasDevices;
    }

    private async void OnStartNpcapClicked(object? sender, RoutedEventArgs e)
    {
        _startNpcapButton.IsEnabled = false;
        _warningBanner.IsVisible = true;
        _warningBanner.Text = "Starting Npcap service…";

        var result = await Task.Run(() => WireFishCaptureHealthChecks.TryStartNpcap(allowElevationPrompt: true));

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _npcapOk = result.IsReady;
            if (result.IsReady)
            {
                _warningBanner.IsVisible = false;
                _warningBanner.Text = string.Empty;
                PopulateInterfaces();
                UpdateStatus();
            }
            else
            {
                _warningBanner.IsVisible = true;
                _warningBanner.Text = result.Message ?? "Failed to start Npcap.";
                _startNpcapButton.IsEnabled = WireFishCaptureHealthChecks.IsNpcapStopped();
                UpdateStatus();
            }
        });
    }

    private async void OnStartClicked(object? sender, RoutedEventArgs e)
    {
        _startButton.IsEnabled = false;
        var device = _interfaceCombo.SelectedItem as CaptureDeviceInfo;
        var filter = _workspace.FilterBar.FilterText;
        var result = await Task.Run(async () => await _capture.StartAsync(device?.CaptureKey, filter));
        switch (result)
        {
            case CaptureStartResult.Started:
                _warningBanner.IsVisible = false;
                _stopButton.IsEnabled = true;
                _interfaceCombo.IsEnabled = false;
                _startNpcapButton.IsEnabled = false;
                _statusTimer.Start();
                break;
            case CaptureStartResult.NoDeviceSelected:
                _warningBanner.IsVisible = true;
                _warningBanner.Text = "Select a network interface before starting capture.";
                _startButton.IsEnabled = CaptureDeviceCatalog.HasCaptureDevices;
                break;
            case CaptureStartResult.Failed:
                _warningBanner.IsVisible = true;
                var health = CaptureDeviceCatalog.DriverHealth;
                _npcapOk = health.IsReady;
                _warningBanner.Text = health.Message
                    ?? "Capture failed to start. Check Npcap installation (Start Npcap button or Start-Service npcap) and interface permissions.";
                _startButton.IsEnabled = CaptureDeviceCatalog.HasCaptureDevices;
                break;
            default:
                _startButton.IsEnabled = CaptureDeviceCatalog.HasCaptureDevices;
                break;
        }

        UpdateStatus();
    }

    private async void OnStopClicked(object? sender, RoutedEventArgs e)
    {
        _stopButton.IsEnabled = false;
        _statusTimer.Stop();
        await Task.Run(async () => await _capture.StopAsync());
        _interfaceCombo.IsEnabled = true;
        PopulateInterfaces();
        UpdateStatus();
    }

    private void OnFilterApplyRequested(object? sender, string filter)
    {
        _workspace.FilterBar.SetFilterText(filter);
        if (_capture.IsCapturing)
        {
            _warningBanner.IsVisible = true;
            _warningBanner.Text = "BPF filter changes apply on the next capture start. Stop and Start to apply.";
        }
    }

    private void OnPacketSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_packetTable.SelectedItem is not PacketRow row)
        {
            _treeDetails.Clear();
            _hexDump.Clear();
            return;
        }

        _treeDetails.SetRoot(PacketDetailBuilder.Build(row));
        _hexDump.SetBytes(row.RawBytes);
    }

    private void UpdateStatus()
    {
        var driver = _npcapOk ? "Npcap OK" : "Npcap down";
        var state = _capture.IsCapturing ? "Capturing" : "Ready";
        _workspace.Toolbar.StatusText = $"{state} | {driver} | Packets: {_store.Count}";
    }
}
