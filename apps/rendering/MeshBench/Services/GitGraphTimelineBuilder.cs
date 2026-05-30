using System.Text;
using Novolis.Timeline;
using Novolis.Timeline.Presentation;

namespace MeshBench.Services;

/// <summary>One row in a <c>git log --graph</c>-style timeline list.</summary>
public sealed record GitGraphTimelineRow(
    TimelineNodeId Id,
    string Graph,
    string Subject,
    string Refs,
    bool IsHead,
    bool IsBranchPoint,
    DateTimeOffset CreatedAt);

/// <summary>Builds ASCII graph prefixes for timeline tree nodes.</summary>
internal static class GitGraphTimelineBuilder
{
    public static IReadOnlyList<GitGraphTimelineRow> Build(TimelineTreeView? tree)
    {
        if (tree is null || tree.Roots.Count == 0)
            return [];

        var rows = new List<GitGraphTimelineRow>();
        for (var i = 0; i < tree.Roots.Count; i++)
            Walk(tree.Roots[i], rows, [], i == tree.Roots.Count - 1);

        rows.Reverse();
        return rows;
    }

    private static void Walk(
        TimelineTreeNode node,
        List<GitGraphTimelineRow> rows,
        List<bool> lanes,
        bool isLastSibling)
    {
        rows.Add(new GitGraphTimelineRow(
            node.Id,
            FormatGraph(lanes, isLastSibling),
            node.Presentation.Label,
            FormatRefs(node),
            node.IsHead,
            node.IsBranchPoint,
            node.CreatedAt));

        for (var i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            var childLanes = new List<bool>(lanes);
            if (node.Children.Count > 1)
                childLanes.Add(i != node.Children.Count - 1);
            else if (lanes.Count > 0)
                childLanes[^1] = false;

            Walk(child, rows, childLanes, i == node.Children.Count - 1);
        }
    }

    private static string FormatGraph(IReadOnlyList<bool> lanes, bool isLastSibling)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < lanes.Count; i++)
            sb.Append(lanes[i] ? "│ " : "  ");

        if (lanes.Count > 0)
            sb.Append(isLastSibling ? "└─" : "├─");

        sb.Append('*');
        return sb.ToString();
    }

    private static string FormatRefs(TimelineTreeNode node)
    {
        var parts = new List<string> { node.BranchName };
        if (node.IsHead)
            parts.Add("HEAD");
        if (node.IsBranchPoint)
            parts.Add("branch-point");
        return string.Join(", ", parts);
    }
}
