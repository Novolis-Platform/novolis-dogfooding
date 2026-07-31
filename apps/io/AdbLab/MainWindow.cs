using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Novolis.IO.Mobile.Android;

namespace AdbLab;

internal sealed class MainWindow : Window
{
    static readonly IBrush Bg = new SolidColorBrush(Color.FromRgb(14, 20, 28));
    static readonly IBrush Panel = new SolidColorBrush(Color.FromRgb(22, 32, 42));
    static readonly IBrush BorderC = new SolidColorBrush(Color.FromRgb(40, 60, 75));
    static readonly IBrush Text = new SolidColorBrush(Color.FromRgb(230, 236, 242));
    static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(150, 168, 184));
    static readonly IBrush Accent = new SolidColorBrush(Color.FromRgb(46, 160, 140));

    readonly AndroidDebugBridge? _adb;
    readonly ListBox _deviceList = new();
    readonly TextBlock _adbPath = new();
    readonly TextBlock _status = new();
    readonly TextBox _deviceInfo = new();
    readonly TextBox _packageBox = new();
    readonly TextBox _log = new();
    readonly Button _refreshBtn;
    readonly Button _statsBtn;
    readonly Button _inspectBtn;
    readonly Button _installBtn;
    bool _busy;
    string? _selectedSerial;

    public MainWindow()
    {
        Title = "Novolis Adb Lab";
        Width = 1180;
        Height = 820;
        MinWidth = 800;
        MinHeight = 520;
        Background = Bg;

        _refreshBtn = PrimaryButton("Refresh devices", () => _ = RefreshAsync());
        _statsBtn = PrimaryButton("Refresh stats", () => _ = ReloadStatsAsync());
        _inspectBtn = PrimaryButton("Inspect package", () => _ = InspectPackageAsync());
        _installBtn = SecondaryButton("Install APK…", () => _ = InstallApkAsync());

        try
        {
            _adb = new AndroidDebugBridge();
        }
        catch (Exception ex)
        {
            Content = ErrorBody($"Could not locate adb.\n\n{ex.Message}");
            return;
        }

        _adbPath.Text = $"{_adb.Transport} · {_adb.AdbPath}";
        _adbPath.FontSize = 12;
        _adbPath.Foreground = Muted;

        _status.Text = "Ready.";
        _status.FontSize = 12;
        _status.Foreground = Muted;
        _status.VerticalAlignment = VerticalAlignment.Center;

        _deviceInfo.IsReadOnly = true;
        _deviceInfo.AcceptsReturn = true;
        _deviceInfo.TextWrapping = TextWrapping.NoWrap;
        _deviceInfo.FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New, monospace");
        _deviceInfo.FontSize = 12;
        _deviceInfo.Background = Panel;
        _deviceInfo.BorderBrush = BorderC;
        _deviceInfo.BorderThickness = new Thickness(1);
        _deviceInfo.Foreground = Text;
        _deviceInfo.Text = "Select a device.";
        _deviceInfo.MinHeight = 280;

        _deviceList.Background = Panel;
        _deviceList.BorderBrush = BorderC;
        _deviceList.BorderThickness = new Thickness(1);
        _deviceList.MinHeight = 160;
        _deviceList.SelectionChanged += (_, _) => OnDeviceSelected();

        _packageBox.Text = AdbSmoke.BooksMobilePackage;
        _packageBox.PlaceholderText = "package name";
        _packageBox.MinWidth = 280;

        _log.IsReadOnly = true;
        _log.AcceptsReturn = true;
        _log.TextWrapping = TextWrapping.Wrap;
        _log.FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New, monospace");
        _log.FontSize = 12;
        _log.Background = Panel;
        _log.BorderBrush = BorderC;
        _log.BorderThickness = new Thickness(1);
        _log.MinHeight = 160;

        Content = BuildLayout();
        Opened += (_, _) => _ = RefreshAsync();
    }

    Control BuildLayout()
    {
        var header = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(16, 14, 16, 8),
            Children =
            {
                new TextBlock
                {
                    Text = "Adb Lab",
                    FontSize = 22,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Text,
                },
                new TextBlock
                {
                    Text = "Dogfood Novolis.IO.Mobile.Android — tethered device discovery and package read.",
                    FontSize = 13,
                    Foreground = Muted,
                },
                _adbPath,
            },
        };

        var left = new DockPanel
        {
            Margin = new Thickness(16, 8, 8, 16),
            Children =
            {
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Margin = new Thickness(0, 0, 0, 8),
                    [DockPanel.DockProperty] = Dock.Top,
                    Children = { _refreshBtn, _statsBtn, _status },
                },
                Section("Devices", _deviceList),
            },
        };

        var rightTop = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(8, 8, 16, 8),
            Children =
            {
                Section("Device stats", _deviceInfo),
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        _packageBox,
                        _inspectBtn,
                        _installBtn,
                    },
                },
            },
        };

        var right = new DockPanel
        {
            Margin = new Thickness(8, 0, 16, 16),
            Children =
            {
                new Border
                {
                    Child = rightTop,
                    [DockPanel.DockProperty] = Dock.Top,
                },
                Section("Log", _log),
            },
        };

        var split = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("340,*"),
            RowDefinitions = new RowDefinitions("*"),
        };
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        split.Children.Add(left);
        split.Children.Add(right);

        return new DockPanel
        {
            Children =
            {
                new Border
                {
                    Background = Panel,
                    BorderBrush = BorderC,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Child = header,
                    [DockPanel.DockProperty] = Dock.Top,
                },
                split,
            },
        };
    }

    async Task RefreshAsync()
    {
        if (_adb is null || !BeginBusy("Listing devices…"))
            return;
        try
        {
            var devices = await Task.Run(() => _adb.ListDevices()).ConfigureAwait(true);
            _deviceList.ItemsSource = devices
                .Select(d => new DeviceRow(d))
                .ToList();
            AppendLog($"devices: {devices.Count}");
            foreach (var d in devices)
                AppendLog($"  {d.Serial}  {d.State}  {d.Model}");

            if (_deviceList.ItemCount > 0 && _deviceList.SelectedIndex < 0)
                _deviceList.SelectedIndex = 0;

            SetStatus(devices.Count == 0
                ? "No devices. Enable USB debugging / authorize this PC."
                : $"{devices.Count} device(s).");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
            AppendLog($"ERROR {ex.Message}");
        }
        finally
        {
            EndBusy();
        }
    }

    void OnDeviceSelected()
    {
        if (_deviceList.SelectedItem is not DeviceRow row)
        {
            _selectedSerial = null;
            _deviceInfo.Text = "Select a device.";
            return;
        }

        _selectedSerial = row.Device.Serial;
        _ = LoadInfoAsync(row.Device.Serial);
    }

    Task ReloadStatsAsync()
    {
        if (_selectedSerial is null && SelectedSerial() is { } s)
            _selectedSerial = s;
        if (_selectedSerial is null)
        {
            SetStatus("Select a device first.");
            return Task.CompletedTask;
        }

        return LoadInfoAsync(_selectedSerial);
    }

    async Task LoadInfoAsync(string serial)
    {
        if (_adb is null || !BeginBusy($"Reading stats for {serial}…"))
            return;
        try
        {
            var info = await Task.Run(() => _adb.GetDeviceInfo(serial)).ConfigureAwait(true);
            _deviceInfo.Text = info.FormatReport();
            AppendLog(
                $"stats {serial}: {info.Manufacturer} {info.Model} · " +
                $"A{info.AndroidVersion}/SDK{info.SdkVersion} · " +
                $"bat {info.Battery?.Level?.ToString() ?? "?"}%" +
                (info.Battery is { } b ? $" {b.StatusLabel}" : "") +
                $" · {info.Display?.PhysicalSize ?? "?"} @{info.Display?.DensityDpi?.ToString() ?? "?"}dpi · " +
                $"RAM {FormatShortMb(info.Memory?.MemAvailableKb)}/{FormatShortMb(info.Memory?.MemTotalKb)} avail");
            SetStatus($"Loaded stats for {serial}.");
        }
        catch (Exception ex)
        {
            _deviceInfo.Text = ex.Message;
            SetStatus(ex.Message);
            AppendLog($"ERROR {ex.Message}");
        }
        finally
        {
            EndBusy();
        }
    }

    static string FormatShortMb(long? kb) =>
        kb is null ? "?" : $"{kb.Value / 1024.0:0.#}MiB";

    async Task InspectPackageAsync()
    {
        var serial = SelectedSerial();
        if (serial is null)
        {
            SetStatus("Select a device first.");
            return;
        }

        var package = _packageBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(package))
        {
            SetStatus("Enter a package name.");
            return;
        }

        if (_adb is null || !BeginBusy($"Inspecting {package}…"))
            return;
        try
        {
            var adb = _adb;
            var text = await Task.Run(() =>
            {
                var sb = new StringBuilder();
                var path = adb.Shell($"pm path {package}", serial);
                sb.AppendLine($"$ pm path {package}  (exit {path.ExitCode})");
                sb.AppendLine(path.StdOut.Trim());
                if (!string.IsNullOrWhiteSpace(path.StdErr))
                    sb.AppendLine(path.StdErr.Trim());

                var ver = adb.Shell(
                    $"dumpsys package {package} | grep -E 'versionName=|versionCode=|lastUpdateTime=|firstInstallTime=|pkg=|userId=' | head -n 24",
                    serial);
                sb.AppendLine();
                sb.AppendLine("$ dumpsys package (version / install)");
                sb.AppendLine(ver.StdOut.Trim());

                var act = adb.Shell($"cmd package resolve-activity --brief {package} | head -n 6", serial);
                sb.AppendLine();
                sb.AppendLine("$ resolve-activity");
                sb.AppendLine(act.StdOut.Trim());
                return sb.ToString();
            }).ConfigureAwait(true);

            AppendLog(text.TrimEnd());
            SetStatus(text.Contains($"package:{package}", StringComparison.Ordinal) ||
                      text.Contains(package + "/", StringComparison.Ordinal)
                ? $"Package {package} found."
                : $"Package {package} not found (or empty pm path).");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
            AppendLog($"ERROR {ex.Message}");
        }
        finally
        {
            EndBusy();
        }
    }

    async Task InstallApkAsync()
    {
        var serial = SelectedSerial();
        if (serial is null)
        {
            SetStatus("Select a device first.");
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Install APK",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Android package") { Patterns = ["*.apk"] },
            ],
        }).ConfigureAwait(true);

        if (files.Count == 0)
            return;

        var path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Could not resolve APK path.");
            return;
        }

        if (_adb is null || !BeginBusy($"Installing {Path.GetFileName(path)}…"))
            return;
        try
        {
            var installer = new AndroidAppInstaller(_adb);
            var expected = _packageBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(expected) || expected.Contains(' ', StringComparison.Ordinal))
                expected = null;

            var result = await Task.Run(() => installer.Install(path, new ApkInstallOptions
            {
                Serial = serial,
                Reinstall = true,
                GrantPermissions = true,
                ExpectedPackageName = expected,
                VerifyInstalled = expected is not null,
            })).ConfigureAwait(true);

            AppendLog(result.Message);
            if (result.Validation is { Warnings.Count: > 0 } v)
            {
                foreach (var w in v.Warnings)
                    AppendLog($"warn: {w}");
            }

            if (result.Package is { } pkg)
                AppendLog($"package {pkg.PackageName} path={pkg.ApkPath} version={pkg.VersionName} ({pkg.VersionCode})");

            SetStatus(result.Ok ? "Install succeeded." : "Install failed.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
            AppendLog($"ERROR {ex.Message}");
        }
        finally
        {
            EndBusy();
        }
    }

    string? SelectedSerial() =>
        _deviceList.SelectedItem is DeviceRow row ? row.Device.Serial : null;

    bool BeginBusy(string message)
    {
        if (_busy)
            return false;
        _busy = true;
        _refreshBtn.IsEnabled = false;
        _statsBtn.IsEnabled = false;
        _inspectBtn.IsEnabled = false;
        _installBtn.IsEnabled = false;
        SetStatus(message);
        return true;
    }

    void EndBusy()
    {
        _busy = false;
        _refreshBtn.IsEnabled = true;
        _statsBtn.IsEnabled = true;
        _inspectBtn.IsEnabled = true;
        _installBtn.IsEnabled = true;
    }

    void SetStatus(string text) => _status.Text = text;

    void AppendLog(string text)
    {
        if (_log.Text?.Length > 0)
            _log.Text += Environment.NewLine;
        _log.Text += text;
        _log.CaretIndex = _log.Text?.Length ?? 0;
    }

    static Control ErrorBody(string message) =>
        new TextBlock
        {
            Text = message,
            Margin = new Thickness(24),
            Foreground = Text,
            TextWrapping = TextWrapping.Wrap,
        };

    static Control Section(string title, Control child) =>
        new DockPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Muted,
                    Margin = new Thickness(0, 0, 0, 6),
                    [DockPanel.DockProperty] = Dock.Top,
                },
                child,
            },
        };

    static Button PrimaryButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Content = text,
            Background = Accent,
            Foreground = Brushes.White,
            Padding = new Thickness(12, 6),
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    static Button SecondaryButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Content = text,
            Background = Panel,
            Foreground = Text,
            BorderBrush = BorderC,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 6),
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    sealed class DeviceRow(AdbDevice device)
    {
        public AdbDevice Device { get; } = device;

        public override string ToString() =>
            $"{Device.Serial}  ·  {Device.State}  ·  {Device.Model ?? Device.Product ?? "—"}";
    }
}
