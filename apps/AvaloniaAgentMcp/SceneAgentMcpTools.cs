using System.ComponentModel;
using ModelContextProtocol.Server;

namespace AvaloniaAgentMcp;

/// <summary>MCP tools for Novolis.Avalonia.3D scene session (Definition-driven surface on :18785).</summary>
[McpServerToolType]
public static class SceneAgentMcpTools
{
    [McpServerTool]
    [Description("List known lightweight scene-modeling session hosts (HTTP marker / default :18785).")]
    public static string SceneHosts() =>
        AvaloniaAgentRuntime.ToJson(new
        {
            hosts = SceneAgentRuntime.DiscoverHosts(),
            httpOverride = SceneAgentRuntime.HttpUrlOverride,
        });

    [McpServerTool]
    [Description("Connect to scene HTTP session. Empty uses marker or http://127.0.0.1:18785.")]
    public static async Task<string> SceneHttpConnect(
        [Description("Base URL, e.g. http://127.0.0.1:18785")]
        string? baseUrl = null,
        CancellationToken cancellationToken = default)
    {
        await SceneAgentRuntime.SetHttpUrlAsync(baseUrl).ConfigureAwait(false);
        var hello = await SceneAgentRuntime.HelloAsync(cancellationToken).ConfigureAwait(false);
        return AvaloniaAgentRuntime.ToJson(new
        {
            transport = "http",
            endpoint = SceneAgentRuntime.HttpUrlOverride
                        ?? $"(auto {SceneAgentRuntime.DefaultHttpPort})",
            hello,
        });
    }

    [McpServerTool]
    [Description("session.hello for lightweight scene modeling surface.")]
    public static async Task<string> SceneHello(CancellationToken cancellationToken = default)
    {
        var response = await SceneAgentRuntime.HelloAsync(cancellationToken).ConfigureAwait(false);
        return AvaloniaAgentRuntime.ToJson(response);
    }

    [McpServerTool]
    [Description("session.snapshot — scene document + selection + action catalog.")]
    public static async Task<string> SceneSnapshot(CancellationToken cancellationToken = default)
    {
        var response = await SceneAgentRuntime.SnapshotAsync(cancellationToken).ConfigureAwait(false);
        return AvaloniaAgentRuntime.ToJson(response);
    }

    [McpServerTool]
    [Description("session.actions — enabled scene actions (addlight, settransform, …).")]
    public static async Task<string> SceneActions(CancellationToken cancellationToken = default)
    {
        var response = await SceneAgentRuntime.ActionsAsync(cancellationToken).ConfigureAwait(false);
        return AvaloniaAgentRuntime.ToJson(response);
    }

    [McpServerTool]
    [Description("session.definition — auto-constructed OpenAPI/actions/MCP descriptors from Agent.Surface.")]
    public static async Task<string> SceneDefinition(CancellationToken cancellationToken = default)
    {
        var response = await SceneAgentRuntime.DefinitionAsync(cancellationToken).ConfigureAwait(false);
        return AvaloniaAgentRuntime.ToJson(response);
    }

    [McpServerTool]
    [Description("session.command — mutate the scene (addlight, addcamera, setlight, settransform, …).")]
    public static async Task<string> SceneCommand(
        [Description("Action id, e.g. addlight, addcamera, settransform, delete.")]
        string actionId,
        [Description("Light kind: omni, spot, infinite, area.")]
        string? lightKind = null,
        [Description("Node id (uuid) for select/set/delete.")]
        string? nodeId = null,
        [Description("Parent node id.")]
        string? parentId = null,
        [Description("Optional display name.")]
        string? name = null,
        [Description("Light intensity.")]
        float? intensity = null,
        [Description("Position X")]
        float? x = null,
        [Description("Position Y")]
        float? y = null,
        [Description("Position Z")]
        float? z = null,
        [Description("File path for open/save.")]
        string? path = null,
        [Description("Generator kind: cloner, symmetry, extrude.")]
        string? generatorKind = null,
        [Description("Modifier kind: weld, subdivision, optimize.")]
        string? modifierKind = null,
        CancellationToken cancellationToken = default)
    {
        var response = await SceneAgentRuntime.CommandAsync(
            new
            {
                actionId,
                lightKind,
                nodeId,
                parentId,
                name,
                intensity,
                x,
                y,
                z,
                path,
                generatorKind,
                modifierKind,
            },
            cancellationToken).ConfigureAwait(false);
        return AvaloniaAgentRuntime.ToJson(response);
    }
}
