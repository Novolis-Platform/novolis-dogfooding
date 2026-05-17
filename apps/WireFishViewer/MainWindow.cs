using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Novolis.Avalonia.Controls;
using Novolis.Avalonia.Layout;
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
    private readonly TextBlock _warningBanner;

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
        _warningBanner = new TextBlock
        {
            IsVisible = false,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(8, 0, 8, 4),
        };

        _startButton.Click += OnStartClicked;
        _stopButton.Click += OnStopClicked;
        _packetTable.SelectionChanged += OnPacketSelectionChanged;
        _workspace.FilterBar.ApplyRequested += OnFilterApplyRequested;

        _workspace.Toolbar.AddAction(_startButton);
        _workspace.Toolbar.AddAction(_stopButton);
        _workspace.Toolbar.AddAction(_interfaceCombo);

        ConfigureWindow();
        PopulateInterfaces();
        UpdateStatus();

        var root = new DockPanel();
        DockPanel.SetDock(_warningBanner, Dock.Top);
        root.Children.Add(_warningBanner);
        root.Children.Add(_workspace);
        Content = root;

        _packetTable.ItemsSource = _store.Packets;
        _store.CollectionChanged += (_, _) => UpdateStatus();
    }

    private static PacketTableView CreatePacketTable()
    {
        var table = new PacketTableView();
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

    private void PopulateInterfaces()
    {
        _interfaceCombo.ItemsSource = CaptureDeviceCatalog.ListDevices();
        if (_interfaceCombo.ItemCount > 0)
            _interfaceCombo.SelectedIndex = 0;

        var hasDevices = CaptureDeviceCatalog.HasCaptureDevices;
        _warningBanner.IsVisible = !hasDevices;
        _warningBanner.Text = hasDevices
            ? string.Empty
            : "No capture devices found. Install Npcap (Windows) or libpcap, then restart. You can still explore the UI without live capture.";
        _startButton.IsEnabled = hasDevices;
    }

    private async void OnStartClicked(object? sender, RoutedEventArgs e)
    {
        var device = _interfaceCombo.SelectedItem as CaptureDeviceInfo;
        var result = await _capture.StartAsync(device?.CaptureKey, _workspace.FilterBar.FilterText);
        switch (result)
        {
            case CaptureStartResult.Started:
                _startButton.IsEnabled = false;
                _stopButton.IsEnabled = true;
                _interfaceCombo.IsEnabled = false;
                break;
            case CaptureStartResult.NoDeviceSelected:
                _warningBanner.IsVisible = true;
                _warningBanner.Text = "Select a network interface before starting capture.";
                break;
            case CaptureStartResult.Failed:
                _warningBanner.IsVisible = true;
                _warningBanner.Text = "Capture failed to start. Check Npcap installation and interface permissions.";
                break;
        }

        UpdateStatus();
    }

    private async void OnStopClicked(object? sender, RoutedEventArgs e)
    {
        await _capture.StopAsync();
        _startButton.IsEnabled = CaptureDeviceCatalog.HasCaptureDevices;
        _stopButton.IsEnabled = false;
        _interfaceCombo.IsEnabled = true;
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
        var state = _capture.IsCapturing ? "Capturing" : "Ready";
        _workspace.Toolbar.StatusText = $"{state} | Packets: {_store.Count}";
    }
}
