using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using MeshBench.Services;

namespace MeshBench.Ui;

/// <summary>Timeline list with <c>git log --graph</c>-style ASCII lanes.</summary>
internal sealed class GitGraphTimelineList : ListBox
{
    public GitGraphTimelineList()
    {
        Background = Brushes.Transparent;
        FontFamily = "Cascadia Mono,Consolas,monospace";
        FontSize = 12;
        ItemTemplate = new FuncDataTemplate<GitGraphTimelineRow>(BuildRow, supportsRecycling: true);
    }

    public void SetRows(IReadOnlyList<GitGraphTimelineRow> rows)
    {
        if (rows.Count == 0)
        {
            ItemsSource = null;
            SelectedItem = null;
            return;
        }

        if (ItemsSource is IList<GitGraphTimelineRow> existing && existing.Count == rows.Count)
        {
            var same = true;
            for (var i = 0; i < rows.Count; i++)
            {
                if (existing[i].Id != rows[i].Id)
                {
                    same = false;
                    break;
                }
            }

            if (same)
                return;
        }

        ItemsSource = rows.ToList();
    }

    public void SelectHeadRow(IReadOnlyList<GitGraphTimelineRow> rows)
    {
        var head = rows.FirstOrDefault(r => r.IsHere);
        if (head is not null)
            SelectedItem = head;
    }

    public GitGraphTimelineRow? SelectedGitRow => SelectedItem as GitGraphTimelineRow;

    private static Control BuildRow(GitGraphTimelineRow? row, object? scope)
    {
        _ = scope;
        if (row is null)
            return new TextBlock();

        var dot = CreateCommitDot(row);
        ToolTip.SetTip(dot, row.IsHere ? "You are here (HEAD)" : row.BranchName);

        var graph = new TextBlock
        {
            Text = row.Graph,
            FontFamily = "Cascadia Mono,Consolas,monospace",
            Foreground = Brush(row.BranchColor, 0.55),
            MinWidth = 56,
            Margin = new Thickness(2, 2, 0, 2),
        };

        var subject = new TextBlock
        {
            Text = row.Subject,
            Foreground = Brushes.WhiteSmoke,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 2, 4, 2),
        };

        var kindBadge = new Border
        {
            Background = Brush(row.KindColor, 0.35),
            BorderBrush = Brush(row.KindColor, 1),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(4, 1),
            Child = new TextBlock
            {
                Text = row.SnapshotKind,
                FontSize = 10,
                Foreground = Brush(row.KindColor, 1),
            },
            VerticalAlignment = VerticalAlignment.Center,
        };

        var center = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { subject, kindBadge },
        };

        var branch = new TextBlock
        {
            Text = row.BranchName,
            Foreground = Brush(row.BranchColor, 1),
            Opacity = row.IsHere ? 1 : 0.85,
            FontWeight = row.IsHere ? FontWeight.SemiBold : FontWeight.Normal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 2, 8, 2),
        };

        Grid.SetColumn(dot, 0);
        Grid.SetColumn(graph, 1);
        Grid.SetColumn(center, 2);
        Grid.SetColumn(branch, 3);
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto"),
            Children = { dot, graph, center, branch },
        };
    }

    private static Control CreateCommitDot(GitGraphTimelineRow row)
    {
        var size = row.IsHere ? 12.0 : row.IsBranchPoint ? 10.0 : 8.0;
        var fill = Brush(row.BranchColor, 1);
        var dot = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size / 2),
            Background = fill,
            BorderBrush = row.IsHere ? Brushes.White : Brush(row.BranchColor, 0.6),
            BorderThickness = row.IsHere ? new Thickness(2) : new Thickness(0),
            Margin = new Thickness(8, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };

        if (!row.IsHere)
            return dot;

        return new Border
        {
            BorderBrush = Brush(row.BranchColor, 0.5),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius((size + 6) / 2),
            Padding = new Thickness(3),
            Child = dot,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0),
        };
    }

    private static IBrush Brush(GraphRgb rgb, double opacity) =>
        new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), rgb.R, rgb.G, rgb.B));
}
