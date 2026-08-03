using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MobilityLab.Experiment;
using Novolis.Geopolitics.Core;

namespace MobilityLab;

internal sealed class MainWindow : Window
{
    static readonly Color Navy = Color.Parse("#0b1c2c");
    static readonly Color Panel = Color.Parse("#152a3d");
    static readonly Color Teal = Color.Parse("#3ecfaf");
    static readonly Color Copper = Color.Parse("#d4a04a");
    static readonly Color Ink = Color.Parse("#f0f4f8");
    static readonly Color Muted = Color.Parse("#b7c6d4");
    static readonly Color Fail = Color.Parse("#e07070");
    static readonly Color Pass = Color.Parse("#d4a04a");
    static readonly Color AlphaLine = Color.Parse("#f0845c");
    static readonly Color BetaLine = Color.Parse("#3ecfaf");
    static readonly Color ChartBg = Color.Parse("#0a1622");

    readonly NumericUpDown _alphaTax = MakeTaxBox(0.38);
    readonly NumericUpDown _betaTax = MakeTaxBox(0.14);
    readonly NumericUpDown _months = new()
    {
        Minimum = 6m,
        Maximum = 120m,
        Increment = 1m,
        Value = 36m,
        Width = 96,
        FormatString = "0",
        FontSize = 15,
        MinHeight = 36,
    };
    readonly CheckBox _warShock = new()
    {
        Content = "War shock (confounder)",
        IsChecked = false,
        Foreground = new SolidColorBrush(Ink),
        FontSize = 15,
        MinHeight = 36,
        VerticalContentAlignment = VerticalAlignment.Center,
    };

    readonly TextBlock _status = new()
    {
        Text = "HardPause — set parameters, then Run.",
        Foreground = new SolidColorBrush(Ink),
        FontSize = 16,
        FontWeight = FontWeight.SemiBold,
        Margin = new Thickness(0, 4, 0, 0),
    };

    readonly TextBlock _mapText = MakeReadable(15, mono: true);
    readonly TextBlock _metricsText = MakeReadable(15);
    readonly StackPanel _scoreRows = new() { Spacing = 10 };
    readonly TextBlock _feedText = MakeReadable(14, mono: true);
    readonly TextBox _reportBox = new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = new FontFamily("Consolas, 'Courier New', monospace"),
        FontSize = 14,
        Foreground = new SolidColorBrush(Ink),
        Background = new SolidColorBrush(ChartBg),
        MinHeight = 280,
        MaxHeight = 420,
        IsReadOnly = true,
        BorderThickness = new Thickness(0),
    };

    readonly Canvas _popSeries = new()
    {
        Height = 180,
        Background = new SolidColorBrush(ChartBg),
        ClipToBounds = true,
    };
    readonly Canvas _pushSeries = new()
    {
        Height = 140,
        Background = new SolidColorBrush(ChartBg),
        ClipToBounds = true,
    };
    readonly TextBlock _popRaw = MakeReadable(13, mono: true);
    readonly TextBlock _pushRaw = MakeReadable(13, mono: true);

    TaxMobilityWorld.Model? _model;
    Queue<string> _log = new();
    ExperimentSpec _spec = ExperimentSpec.Default;
    DispatcherTimer? _timer;
    string _lastMarkdown = "";

    public MainWindow()
    {
        Title = "MobilityLab — tax–mobility desk";
        Width = 1280;
        Height = 960;
        MinWidth = 1024;
        MinHeight = 720;
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
            FontSize = 42,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Copper),
            FontFamily = new FontFamily("Georgia, 'Times New Roman', serif"),
        };

        var hypothesis = new TextBlock
        {
            Text =
                "RQ: Holding geography and initial stocks fixed, does raising Alpha’s household tax " +
                "above Economy tax-push / Civics emigration thresholds cause outflow, higher pressure, " +
                "and weaker early approval vs twin Beta, with Gamma as haven?\n" +
                "Estimator: same-seed counterfactual (Alpha tax = Beta tax) → ATT; plus twin DID on pop growth. " +
                "End L is diagnostic only (often ceiling-bound).",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Ink),
            FontSize = 16,
            LineHeight = 24,
            Margin = new Thickness(0, 10, 0, 4),
            MaxWidth = 1100,
        };

        var runBtn = PrimaryButton("Run", (_, _) => RunAll());
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
        var copyBtn = PrimaryButton("Copy markdown report", async (_, _) => await CopyReportAsync());

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
                copyBtn,
            },
        };

        var charts = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                ChartBlock("Population (α copper · β teal) — shared scale", _popSeries, _popRaw),
                ChartBlock("Emigration pressure (α copper · β teal) — shared 0–1", _pushSeries, _pushRaw),
            },
        };

        var mapPanel = Section(
            "Province map",
            new ScrollViewer
            {
                MaxHeight = 340,
                Content = _mapText,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            });

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2.2*,*"),
            Margin = new Thickness(0, 12, 0, 0),
        };
        body.Children.Add(charts);
        Grid.SetColumn(charts, 0);
        body.Children.Add(mapPanel);
        Grid.SetColumn(mapPanel, 1);
        mapPanel.Margin = new Thickness(14, 0, 0, 0);

        var evidence = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            Margin = new Thickness(0, 14, 0, 0),
        };
        var left = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                Section("Coupling scorecard", _scoreRows),
                Section("Effect sizes + horizon", _metricsText),
            },
        };
        var right = Section(
            "Month feed (recent)",
            new ScrollViewer
            {
                MaxHeight = 260,
                Content = _feedText,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            });
        evidence.Children.Add(left);
        Grid.SetColumn(left, 0);
        evidence.Children.Add(right);
        Grid.SetColumn(right, 1);
        right.Margin = new Thickness(14, 0, 0, 0);

        var reportSection = Section(
            "Markdown report — select all or use Copy markdown report",
            _reportBox);
        reportSection.Margin = new Thickness(0, 14, 0, 0);

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Margin = new Thickness(28, 24, 28, 32),
                Spacing = 6,
                Children =
                {
                    brand,
                    hypothesis,
                    controls,
                    _status,
                    body,
                    evidence,
                    reportSection,
                },
            },
        };
    }

    Border ChartBlock(string title, Canvas canvas, TextBlock raw)
    {
        var host = new Border
        {
            Background = new SolidColorBrush(Panel),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(3),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 15,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(Ink),
                    },
                    canvas,
                    raw,
                },
            },
        };
        host.SizeChanged += (_, e) =>
        {
            canvas.Width = Math.Max(120, e.NewSize.Width - 24);
            DrawSeries();
        };
        return host;
    }

    static Border Section(string title, Control child) => new()
    {
        Background = new SolidColorBrush(Panel),
        Padding = new Thickness(14),
        CornerRadius = new CornerRadius(3),
        Child = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 17,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Copper),
                },
                child,
            },
        },
    };

    static StackPanel Labeled(string label, Control control) => new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 8,
        Margin = new Thickness(0, 0, 18, 10),
        VerticalAlignment = VerticalAlignment.Center,
        Children =
        {
            new TextBlock
            {
                Text = label,
                FontSize = 15,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Ink),
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
        Width = 96,
        FormatString = "0.00",
        FontSize = 15,
        MinHeight = 36,
    };

    static Button PrimaryButton(string text, EventHandler<Avalonia.Interactivity.RoutedEventArgs> onClick)
    {
        var b = new Button
        {
            Content = text,
            Margin = new Thickness(0, 0, 10, 10),
            Padding = new Thickness(18, 10),
            Background = new SolidColorBrush(Copper),
            Foreground = new SolidColorBrush(Navy),
            FontWeight = FontWeight.Bold,
            FontSize = 15,
            MinHeight = 40,
        };
        b.Click += onClick;
        return b;
    }

    static Button SecondaryButton(string text, EventHandler<Avalonia.Interactivity.RoutedEventArgs> onClick)
    {
        var b = new Button
        {
            Content = text,
            Margin = new Thickness(0, 0, 10, 10),
            Padding = new Thickness(16, 10),
            Background = new SolidColorBrush(Color.Parse("#1e3a52")),
            Foreground = new SolidColorBrush(Ink),
            FontSize = 15,
            MinHeight = 40,
        };
        b.Click += onClick;
        return b;
    }

    static TextBlock MakeReadable(double size, bool mono = false) => new()
    {
        FontFamily = mono
            ? new FontFamily("Consolas, 'Courier New', monospace")
            : new FontFamily("Segoe UI, Candara, Calibri, sans-serif"),
        FontSize = size,
        Foreground = new SolidColorBrush(Ink),
        TextWrapping = TextWrapping.Wrap,
        LineHeight = size + 6,
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
        _lastMarkdown = "";
        _reportBox.Text = "(Run the experiment to generate a markdown report you can copy.)";
    }

    void RunAll()
    {
        StopTimer();
        _spec = ReadSpec();
        var sw = Stopwatch.StartNew();
        var host = ExperimentHost.Run(_spec);
        sw.Stop();
        _model = host.Model;
        _log = host.Log;
        _status.Text =
            $"Complete — {host.Result.PassCount}/{host.Result.CheckCount} scientific checks " +
            $"{(host.Result.AllPass ? "PASS" : "MIXED")} (treated + CF).";
        _status.Foreground = new SolidColorBrush(host.Result.AllPass ? Pass : Ink);
        ApplyResult(host.Result, sw.Elapsed);
    }

    void StepOnce()
    {
        StopTimer();
        if (_model is null)
            ResetExperiment();
        if (_model is null)
            return;

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
            FinishRun(TimeSpan.Zero);
            return;
        }

        TaxMobilityMonth.MaybeApplyWarShock(_model, _log, monthIndex);
        TaxMobilityMonth.Advance(_model, _log);
        _status.Text = $"Stepped to M{_model.History.Months.Count} / {_spec.Months}";
        RefreshUi();
        if (_model.History.Months.Count >= _spec.Months)
            FinishRun(TimeSpan.Zero);
    }

    void FinishRun(TimeSpan elapsed)
    {
        StopTimer();
        if (_model is null)
            return;
        var result = ExperimentHost.EvaluateAgainstCounterfactual(_model);
        _status.Text =
            $"Complete — {result.PassCount}/{result.CheckCount} scientific checks " +
            $"{(result.AllPass ? "PASS" : "MIXED")}.";
        _status.Foreground = new SolidColorBrush(result.AllPass ? Pass : Ink);
        ApplyResult(result, elapsed);
    }

    void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }

    async Task CopyReportAsync()
    {
        if (string.IsNullOrWhiteSpace(_lastMarkdown))
        {
            _status.Text = "No report yet — press Run first.";
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            _status.Text = "Clipboard unavailable — select the markdown box and copy manually.";
            _reportBox.Focus();
            _reportBox.SelectAll();
            return;
        }

        await clipboard.SetTextAsync(_lastMarkdown);
        _status.Text = "Markdown report copied to clipboard.";
        _reportBox.Focus();
        _reportBox.SelectAll();
    }

    void RefreshUi(TimeSpan? elapsed = null)
    {
        if (_model is null)
            return;

        DrawSeries();
        DrawMap();
        RebuildFeed();

        if (_model.History.Months.Count == 0)
            return;

        var result = ExperimentHost.EvaluateAgainstCounterfactual(_model);
        ApplyResult(result, elapsed);
    }

    void ApplyResult(ExperimentResult result, TimeSpan? elapsed)
    {
        if (_model is null)
            return;

        DrawSeries();
        DrawMap();
        RebuildScorecard(result);
        RebuildMetrics(result);
        RebuildFeed();
        _lastMarkdown = MarkdownReport.Build(result, _model, elapsed);
        _reportBox.Text = _lastMarkdown;
    }

    void RebuildScorecard(ExperimentResult result)
    {
        _scoreRows.Children.Clear();
        _scoreRows.Children.Add(new TextBlock
        {
            Text = $"Scientific checks {result.PassCount}/{result.CheckCount}" +
                   (result.AllPass ? " — all PASS" : " — review FAILs"),
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(result.AllPass ? Pass : Ink),
            Margin = new Thickness(0, 0, 0, 4),
        });

        foreach (var c in result.Checks)
        {
            _scoreRows.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse("#0f2030")),
                Padding = new Thickness(12, 10),
                CornerRadius = new CornerRadius(3),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"{(c.Pass ? "PASS" : "FAIL")}  ·  {c.Claim}",
                            FontSize = 16,
                            FontWeight = FontWeight.Bold,
                            Foreground = new SolidColorBrush(c.Pass ? Pass : Fail),
                        },
                        new TextBlock
                        {
                            Text = c.Detail,
                            FontSize = 14,
                            Foreground = new SolidColorBrush(Ink),
                            TextWrapping = TextWrapping.Wrap,
                        },
                    },
                },
            });
        }
    }

    void RebuildMetrics(ExperimentResult result)
    {
        var e = result.Effects;
        _metricsText.Text =
            $"ATT Alpha pop   {e.AttAlphaPop:+0;-0}  ({e.AttAlphaPopPct:+0.0%;-0.0%})   CF end {e.CounterfactualAlphaPopEnd:0}\n" +
            $"DID growth A-B  {e.DidPopGrowth:+0.0%;-0.0%}    push gap {e.MeanPushGapVsBeta:+0.000;-0.000}   ATT push {e.AttMeanPush:+0.000;-0.000}\n" +
            $"Gamma absorb    {e.GammaAbsorbShare:0.0%} of Alpha loss\n" +
            $"Early App A-B   {e.EarlyApprovalGap:+0.000;-0.000}   ATT early App {e.AttEarlyApproval:+0.000;-0.000}\n" +
            $"Early L A-B     {e.EarlyLegitimacyGap:+0.000;-0.000} (diagnostic)\n" +
            $"α pop {result.AlphaPopStart:0} → {result.AlphaPopEnd:0}   β {result.BetaPopStart:0} → {result.BetaPopEnd:0}   γ {result.GammaPopStart:0} → {result.GammaPopEnd:0}";
    }

    void RebuildFeed()
    {
        var recent = _log.Reverse().Take(14).Reverse();
        _feedText.Text = string.Join('\n', recent);
    }

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
                var occ = p.OwnerId == p.HomePolityId ? "" : $"  OCC {home}→{own}";
                return $"{p.Name,-4}  owner {own}   pop {p.Population,10:0}{occ}";
            });
        _mapText.Text = string.Join('\n', lines);
    }

    static string PolityTag(PolityId id) => id.Value switch { 0 => "α", 1 => "β", _ => "γ" };

    void DrawSeries()
    {
        _popSeries.Children.Clear();
        _pushSeries.Children.Clear();
        if (_model is null || _model.History.Months.Count < 2)
        {
            _popRaw.Text = "Raw: (need ≥2 months)";
            _pushRaw.Text = "";
            return;
        }

        var months = _model.History.Months;
        var aPop = months.Select(m => m.Alpha.Population).ToList();
        var bPop = months.Select(m => m.Beta.Population).ToList();
        var aPush = months.Select(m => m.Alpha.EmigrationPressure).ToList();
        var bPush = months.Select(m => m.Beta.EmigrationPressure).ToList();

        DrawShared(_popSeries, aPop, bPop, AlphaLine, BetaLine);
        DrawShared(_pushSeries, aPush, bPush, AlphaLine, BetaLine, fixedMin: 0, fixedMax: 1);

        var last = months[^1];
        _popRaw.Text =
            $"Raw end  α {last.Alpha.Population:0}   β {last.Beta.Population:0}   γ {last.Gamma.Population:0}";
        _pushRaw.Text =
            $"Raw end  α push {last.Alpha.EmigrationPressure:0.00}   β push {last.Beta.EmigrationPressure:0.00}   " +
            $"α L {last.Alpha.Legitimacy:0.00}   β L {last.Beta.Legitimacy:0.00}";
    }

    void DrawShared(
        Canvas canvas,
        IReadOnlyList<double> a,
        IReadOnlyList<double> b,
        Color aColor,
        Color bColor,
        double? fixedMin = null,
        double? fixedMax = null)
    {
        var w = canvas.Bounds.Width > 10 ? canvas.Bounds.Width : canvas.Width;
        var h = canvas.Height;
        if (w < 40 || h < 40 || a.Count < 2)
            return;

        var min = fixedMin ?? Math.Min(a.Min(), b.Min());
        var max = fixedMax ?? Math.Max(a.Max(), b.Max());
        var span = Math.Max(1e-9, max - min);

        canvas.Children.Add(MakeGridLine(w, h * 0.25));
        canvas.Children.Add(MakeGridLine(w, h * 0.5));
        canvas.Children.Add(MakeGridLine(w, h * 0.75));

        canvas.Children.Add(MakePoly(a, aColor, w, h, min, span, thickness: 3));
        canvas.Children.Add(MakePoly(b, bColor, w, h, min, span, thickness: 3));
    }

    static Avalonia.Controls.Shapes.Line MakeGridLine(double w, double y) => new()
    {
        StartPoint = new Point(0, y),
        EndPoint = new Point(w, y),
        Stroke = new SolidColorBrush(Color.Parse("#24384c")),
        StrokeThickness = 1,
    };

    static Avalonia.Controls.Shapes.Path MakePoly(
        IReadOnlyList<double> series,
        Color color,
        double w,
        double h,
        double min,
        double span,
        double thickness)
    {
        const double pad = 10;
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            for (var i = 0; i < series.Count; i++)
            {
                var x = pad + (w - 2 * pad) * i / (series.Count - 1);
                var t = (series[i] - min) / span;
                var y = h - pad - t * (h - 2 * pad);
                if (i == 0)
                    ctx.BeginFigure(new Point(x, y), false);
                else
                    ctx.LineTo(new Point(x, y));
            }
        }

        return new Avalonia.Controls.Shapes.Path
        {
            Data = geo,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = thickness,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
        };
    }
}
