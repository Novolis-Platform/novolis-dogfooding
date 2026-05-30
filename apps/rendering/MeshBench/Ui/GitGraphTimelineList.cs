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

    public GitGraphTimelineRow? SelectedGitRow => SelectedItem as GitGraphTimelineRow;

    private static Control BuildRow(GitGraphTimelineRow? row, object? scope)
    {
        _ = scope;
        if (row is null)
            return new TextBlock();

        var graph = new TextBlock
        {
            Text = row.Graph,
            FontFamily = "Cascadia Mono,Consolas,monospace",
            Foreground = new SolidColorBrush(Color.FromRgb(120, 170, 220)),
            MinWidth = 72,
            Margin = new Thickness(4, 2, 0, 2),
        };

        var subject = new TextBlock
        {
            Text = row.Subject,
            Foreground = Brushes.WhiteSmoke,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(4, 2, 4, 2),
        };

        var refs = new TextBlock
        {
            Text = row.Refs,
            Opacity = row.IsHead ? 1 : 0.75,
            FontWeight = row.IsHead ? FontWeight.SemiBold : FontWeight.Normal,
            Foreground = row.IsHead
                ? new SolidColorBrush(Color.FromRgb(120, 220, 140))
                : new SolidColorBrush(Color.FromRgb(180, 180, 190)),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 2, 8, 2),
        };

        Grid.SetColumn(graph, 0);
        Grid.SetColumn(subject, 1);
        Grid.SetColumn(refs, 2);
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Children = { graph, subject, refs },
        };
    }
}
