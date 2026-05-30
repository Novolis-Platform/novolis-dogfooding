using System.Text;
using Novolis.Timeline;
using Novolis.Timeline.Presentation;
using Novolis.Snapshots;

namespace MeshBench.Services;

/// <summary>One row in a <c>git log --graph</c>-style timeline list.</summary>
public sealed record GitGraphTimelineRow(
    TimelineNodeId Id,
    string Graph,
    string Subject,
    string BranchName,
    string SnapshotKind,
    GraphRgb BranchColor,
    GraphRgb KindColor,
    bool IsHere,
    bool IsBranchPoint,
    string Marker,
    DateTimeOffset CreatedAt);

/// <summary>Builds ASCII graph prefixes for timeline tree nodes.</summary>
internal static class GitGraphTimelineBuilder
{
    public static IReadOnlyList<GitGraphTimelineRow> Build(
        TimelineTreeView? tree,
        IReadOnlyList<TimelineNode<ZipSnapshotRef>>? flatNodes = null,
        IReadOnlyList<Branch>? branches = null,
        TimelineHead? head = null)
    {
        if (tree is not null && tree.Roots.Count > 0)
        {
            var rows = new List<GitGraphTimelineRow>();
            for (var i = 0; i < tree.Roots.Count; i++)
                Walk(tree.Roots[i], rows, [], i == tree.Roots.Count - 1);

            rows.Reverse();
            return rows;
        }

        if (flatNodes is { Count: > 0 })
            return BuildFlat(flatNodes, branches, head);

        return [];
    }

    private static IReadOnlyList<GitGraphTimelineRow> BuildFlat(
        IReadOnlyList<TimelineNode<ZipSnapshotRef>> nodes,
        IReadOnlyList<Branch>? branches,
        TimelineHead? head)
    {
        var branchNames = branches?.ToDictionary(b => b.Id, b => b.Name.Value)
            ?? new Dictionary<BranchId, string>();

        var rows = nodes
            .OrderByDescending(n => n.CreatedAt)
            .Select(node =>
            {
                var branchName = branchNames.TryGetValue(node.BranchId, out var name)
                    ? name
                    : node.BranchId.ToString();
                var isHere = head is not null
                    && head.NodesByBranch.TryGetValue(node.BranchId, out var headId)
                    && headId == node.Id;
                return ToRow(
                    node.Id,
                    graph: "* ",
                    node.Metadata.Label ?? node.Metadata.Kind,
                    branchName,
                    node.Metadata.Kind,
                    isHere,
                    isBranchPoint: false,
                    node.CreatedAt);
            })
            .ToList();

        return rows;
    }

    private static void Walk(
        TimelineTreeNode node,
        List<GitGraphTimelineRow> rows,
        List<bool> lanes,
        bool isLastSibling)
    {
        rows.Add(ToRow(
            node.Id,
            FormatGraph(lanes, isLastSibling),
            node.Presentation.Label,
            node.BranchName,
            node.Presentation.Kind,
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

    private static GitGraphTimelineRow ToRow(
        TimelineNodeId id,
        string graph,
        string subject,
        string branchName,
        string snapshotKind,
        bool isHere,
        bool isBranchPoint,
        DateTimeOffset createdAt)
    {
        var branchColor = GitGraphPalette.BranchColor(branchName);
        var kindColor = GitGraphPalette.KindColor(snapshotKind);
        var marker = isHere ? "●" : isBranchPoint ? "◉" : "○";
        return new GitGraphTimelineRow(
            id,
            graph,
            subject,
            branchName,
            GitGraphPalette.KindLabel(snapshotKind),
            branchColor,
            kindColor,
            isHere,
            isBranchPoint,
            marker,
            createdAt);
    }

    private static string FormatGraph(IReadOnlyList<bool> lanes, bool isLastSibling)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < lanes.Count; i++)
            sb.Append(lanes[i] ? "│ " : "  ");

        if (lanes.Count > 0)
            sb.Append(isLastSibling ? "└─" : "├─");

        sb.Append(' ');
        return sb.ToString();
    }
}
