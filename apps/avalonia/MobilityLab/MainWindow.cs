using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MobilityLab.Experiment;
using Novolis.Avalonia.Briefing;
using Novolis.Geopolitics.Core;

namespace MobilityLab;

internal sealed class MainWindow : Window
{
    static readonly Color Navy = Color.Parse("#0b1c2c");
    static readonly Color Panel = Color.Parse("#12263a");
    static readonly Color Teal = Color.Parse("#2a9d8f");
    static readonly Color Copper = Color.Parse("#c98b3a");
    static readonly Color Ink = Color.Parse("#e8eef4");
    static readonly Color Muted = Color.Parse("#8aa0b5");
    static readonly Color AlphaLine = Color.Parse("#e76f51");
    static readonly Color BetaLine = Color.Parse("#2a9d8f");

    readonly NumericUpDown _alphaTax = MakeTaxBox(0.38);
    readonly NumericUpDown _betaTax = MakeTaxBox(0.14);
    readonly NumericUpDown _months = new()
    {
        Minimum = 6m,
        Maximum = 120m,
        Increment = 1m,
        Value = 36m,
        Width = 88,
        FormatString = "0",
    };
    readonly CheckBox _warShock = new()
    {
        Content = "War shock (confounder)",
        IsChecked = false,
        Foreground = new SolidColorBrush(Ink),
    };

    readonly TextBlock _status = new()
    {
        Text = "HardPause — set parameters, then Run.",
        Foreground = new SolidColorBrush(Muted),
        FontSize = 13,
    };
    readonly TextBlock _mapText = MakeMono(12);
    readonly Canvas _series = new()
    {
        Height = 220,
        Background = new SolidColorBrush(Color.Parse("#0a1622")),
        ClipToBounds = true,
    };
    readonly ScorecardView _scorecard = new();
    readonly MetricTableView _metrics = new() { Height = 160 };
    readonly FeedPanel _feed = new() { Height = 140 };

    TaxMobilityWorld.Model? _model;
    Queue<string> _log = new();
    ExperimentSpec _spec = ExperimentSpec.Default;
    DispatcherTimer? _timer;

    public MainWindow()
    {
        Title = "MobilityLab — tax–mobility lab";
        Width = 1180;
        Height = 860;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Navy);

        Content = BuildLayout();
        ResetExperiment();
        RefreshUi();
    }

    Control BuildLayout()
    {
        var brand = new TextBlock
        {
            Text = "MobilityLab",
            FontSize = 36,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Copper),
            FontFamily = new FontFamily("Georgia, 'Times New Roman', serif"),
        };

        var hypothesis = new TextBlock
        {
            Text =
                "RQ: Holding geography and initial stocks fixed, does raising polity Alpha’s household tax " +
                "above Economy tax-push / Civics emigration thresholds cause (1) net population outflow, " +
                "(2) higher emigration pressure, and (3) weaker legitimacy vs low-tax twin Beta, " +
                "with Gamma as a low-tax destination?  Expected: α pop net < 0, α pressure > β, α L < β L.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Ink),
            FontSize = 14,
            Margin = new Thickness(0, 8, 0, 0),
        };

        var runBtn = PrimaryButton("Run", (_, _) => StartRun());
        var stepBtn = SecondaryButton("Step", (_, _) => StepOnce());
        var resetBtn = SecondaryButton("Reset", (_, _) =>
        {
            StopTimer();
            ResetExperiment();
            RefreshUi();
        });
        var pauseBtn = SecondaryButton("Pause", (_, _) =>
        {
            StopTimer();
            _status.Text = "HardPause — editing allowed.";
        });

        var controls = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                Labeled("α tax", _alphaTax),
                Labeled("β tax", _betaTax),
                Labeled("Months", _months),
                _warShock,
                runBtn,
                stepBtn,
                pauseBtn,
                resetBtn,
            },
        };

        var seriesLegend = new TextBlock
        {
            Text = "Series: α pop / pressure (copper-red) · β pop / pressure (teal) — normalized polylines",
            Foreground = new SolidColorBrush(Muted),
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 4),
        };

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,*"),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Margin = new Thickness(0, 12, 0, 0),
        };
        body.Children.Add(seriesLegend);
        Grid.SetColumn(seriesLegend, 0);
        Grid.SetRow(seriesLegend, 0);

        var seriesHost = new Border
        {
            Background = new SolidColorBrush(Panel),
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(2),
            Child = _series,
        };
        body.Children.Add(seriesHost);
        Grid.SetColumn(seriesHost, 0);
        Grid.SetRow(seriesHost, 1);
        seriesHost.SizeChanged += (_, e) =>
        {
            _series.Width = Math.Max(100, e.NewSize.Width - 16);
            DrawSeries();
        };

        var mapBorder = new Border
        {
            Background = new SolidColorBrush(Panel),
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(12, 0, 0, 0),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = "Province map",
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Teal),
                        Margin = new Thickness(0, 0, 0, 6),
                    },
                    _mapText,
                },
            },
        };
        body.Children.Add(mapBorder);
        Grid.SetColumn(mapBorder, 1);
        Grid.SetRow(mapBorder, 0);
        Grid.SetRowSpan(mapBorder, 2);

        var evidence = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            Margin = new Thickness(0, 12, 0, 0),
        };
        evidence.Children.Add(PanelBox("Scorecard", _scorecard, 0));
        evidence.Children.Add(PanelBox("Horizon metrics", _metrics, 1));
        evidence.Children.Add(PanelBox("Month feed", _feed, 2));

        return new ScrollViewer
        {
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 8,
                Children =
                {
                    brand,
                    hypothesis,
                    controls,
                    _status,
                    body,
                    evidence,
                },
            },
        };
    }

    static Border PanelBox(string title, Control child, int column)
    {
        var box = new Border
        {
            Background = new SolidColorBrush(Panel),
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(column == 0 ? 0 : 8, 0, 0, 0),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Copper),
                        Margin = new Thickness(0, 0, 0, 6),
                    },
                    child,
                },
            },
        };
        Grid.SetColumn(box, column);
        return box;
    }

    static StackPanel Labeled(string label, Control control) => new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 6,
        Margin = new Thickness(0, 0, 16, 8),
        VerticalAlignment = VerticalAlignment.Center,
        Children =
        {
            new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Muted),
                VerticalAlignment = VerticalAlignment.Center,
            },
            control,
        },
    };

    static NumericUpDown MakeTaxBox(double value) => new()
    {
        Minimum = 0.05m,
        Maximum = 0.55m,
        Increment = 0.01m,
        Value = (decimal)value,
        Width = 88,
        FormatString = "0.00",
    };

    static Button PrimaryButton(string text, EventHandler<Avalonia.Interactivity.RoutedEventArgs> onClick)
    {
        var b = new Button
        {
            Content = text,
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(16, 8),
            Background = new SolidColorBrush(Copper),
            Foreground = new SolidColorBrush(Navy),
            FontWeight = FontWeight.SemiBold,
        };
        b.Click += onClick;
        return b;
    }

    static Button SecondaryButton(string text, EventHandler<Avalonia.Interactivity.RoutedEventArgs> onClick)
    {
        var b = new Button
        {
            Content = text,
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(14, 8),
            Background = new SolidColorBrush(Color.Parse("#1a3348")),
            Foreground = new SolidColorBrush(Ink),
        };
        b.Click += onClick;
        return b;
    }

    static TextBlock MakeMono(double size) => new()
    {
        FontFamily = new FontFamily("Consolas, 'Courier New', monospace"),
        FontSize = size,
        Foreground = new SolidColorBrush(Ink),
        TextWrapping = TextWrapping.Wrap,
    };

    ExperimentSpec ReadSpec() => new(
        AlphaTax: (double)(_alphaTax.Value ?? 0.38m),
        BetaTax: (double)(_betaTax.Value ?? 0.14m),
        GammaTax: 0.12,
        Months: (int)(_months.Value ?? 36m),
        Seed: 42,
        WarShockOn: _warShock.IsChecked == true,
        AgentsEnabled: false);

    void ResetExperiment()
    {
        _spec = ReadSpec();
        _model = ExperimentHost.CreateFresh(_spec);
        _log = new Queue<string>();
        _log.Enqueue(
            $"Reset · α tax={_spec.AlphaTax:0.00} β={_spec.BetaTax:0.00} γ={_spec.GammaTax:0.00} " +
            $"warShock={_spec.WarShockOn}");
        _status.Text = "HardPause — ready.";
    }

    void StartRun()
    {
        StopTimer();
        ResetExperiment();
        _status.Text = $"Running {_spec.Months} months…";
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _timer.Tick += (_, _) =>
        {
            if (_model is null)
                return;
            var monthIndex = _model.History.Months.Count;
            if (monthIndex >= _spec.Months)
            {
                FinishRun();
                return;
            }

            TaxMobilityMonth.MaybeApplyWarShock(_model, _log, monthIndex);
            TaxMobilityMonth.Advance(_model, _log);
            RefreshUi();
        };
        _timer.Start();
    }

    void StepOnce()
    {
        StopTimer();
        if (_model is null)
            ResetExperiment();
        if (_model is null)
            return;

        // If params changed while paused, rebuild unless mid-horizon
        if (_model.History.Months.Count == 0)
        {
            _spec = ReadSpec();
            _model.Spec = _spec;
            _model.AgentsEnabled = _spec.AgentsEnabled;
            TaxMobilityWorld.LockTreatmentTaxes(_model);
        }

        var monthIndex = _model.History.Months.Count;
        if (monthIndex >= _spec.Months)
        {
            FinishRun();
            return;
        }

        TaxMobilityMonth.MaybeApplyWarShock(_model, _log, monthIndex);
        TaxMobilityMonth.Advance(_model, _log);
        _status.Text = $"Stepped to M{_model.History.Months.Count} / {_spec.Months}";
        RefreshUi();
        if (_model.History.Months.Count >= _spec.Months)
            FinishRun();
    }

    void FinishRun()
    {
        StopTimer();
        if (_model is null)
            return;
        var result = _model.History.Evaluate(_model);
        _status.Text =
            $"Complete — {result.PassCount}/{result.CheckCount} coupling checks " +
            $"{(result.AllPass ? "PASS" : "mixed")}.";
        RefreshUi();
    }

    void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }

    void RefreshUi()
    {
        if (_model is null)
            return;

        DrawSeries();
        DrawMap();
        var result = _model.History.Evaluate(_model);
        _scorecard.SetRows(
            result.Checks.Select(c => new ScorecardRow(
                c.Claim,
                c.Pass ? 1 : 0,
                c.Detail,
                filled: c.Pass)).ToList(),
            $"Coupling {result.PassCount}/{result.CheckCount}");

        _metrics.SetRows(
        [
            new MetricRow("α pop", $"{result.AlphaPopStart:0} → {result.AlphaPopEnd:0}",
                $"netΣ {result.AlphaNetMigrationSum:0}"),
            new MetricRow("β pop", $"{result.BetaPopStart:0} → {result.BetaPopEnd:0}"),
            new MetricRow("γ pop", $"{result.GammaPopStart:0} → {result.GammaPopEnd:0}"),
            new MetricRow("α peak push", $"{result.AlphaPeakPressure:0.00}",
                $"β {result.BetaPeakPressure:0.00}"),
            new MetricRow("α L end", $"{result.AlphaLegitimacyEnd:0.00}",
                $"β {result.BetaLegitimacyEnd:0.00}"),
            new MetricRow("migrated", $"{result.PopulationMigrated:0}", "telemetry"),
        ]);

        _feed.SetLines(_log.Select(line => new FeedLine("lab", Escape(line))).ToList());
    }

    static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    void DrawMap()
    {
        if (_model is null)
            return;
        var lines = _model.World.Provinces
            .OrderBy(p => p.Id.Value)
            .Select(p =>
            {
                var home = PolityTag(p.HomePolityId);
                var own = PolityTag(p.OwnerId);
                var occ = p.OwnerId == p.HomePolityId ? "" : $" OCC {home}→{own}";
                return $"{p.Name,-3} own={own} pop={p.Population,10:0}{occ}";
            });
        _mapText.Text = string.Join('\n', lines);
    }

    static string PolityTag(PolityId id) => id.Value switch { 0 => "α", 1 => "β", _ => "γ" };

    void DrawSeries()
    {
        _series.Children.Clear();
        if (_model is null || _model.History.Months.Count < 2)
            return;

        var w = _series.Bounds.Width > 10 ? _series.Bounds.Width : _series.Width;
        var h = _series.Height;
        if (w < 40 || h < 40)
            return;

        var months = _model.History.Months;
        DrawPoly(months.Select(m => m.Alpha.Population).ToList(), AlphaLine, w, h, 0.55);
        DrawPoly(months.Select(m => m.Beta.Population).ToList(), BetaLine, w, h, 0.55);
        DrawPoly(months.Select(m => m.Alpha.EmigrationPressure).ToList(), Copper, w, h, 0.35, dashed: true);
        DrawPoly(months.Select(m => m.Beta.EmigrationPressure).ToList(), Teal, w, h, 0.35, dashed: true);
    }

    void DrawPoly(
        IReadOnlyList<double> series,
        Color color,
        double w,
        double h,
        double verticalShare,
        bool dashed = false)
    {
        if (series.Count < 2)
            return;
        var min = series.Min();
        var max = series.Max();
        var span = Math.Max(1e-9, max - min);
        var pad = 8.0;
        var usableH = (h - 2 * pad) * verticalShare;
        var yBase = dashed ? h - pad : pad + usableH;

        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            for (var i = 0; i < series.Count; i++)
            {
                var x = pad + (w - 2 * pad) * i / (series.Count - 1);
                var t = (series[i] - min) / span;
                var y = dashed
                    ? yBase - t * usableH
                    : yBase - t * usableH;
                if (i == 0)
                    ctx.BeginFigure(new Point(x, y), false);
                else
                    ctx.LineTo(new Point(x, y));
            }
        }

        _series.Children.Add(new Avalonia.Controls.Shapes.Path
        {
            Data = geo,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = dashed ? 1.5 : 2.2,
            StrokeDashArray = dashed ? [4, 3] : null,
        });
    }
}
