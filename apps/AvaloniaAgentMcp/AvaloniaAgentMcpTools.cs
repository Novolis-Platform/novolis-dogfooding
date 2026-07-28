using System.ComponentModel;
using ModelContextProtocol.Server;

namespace AvaloniaAgentMcp;

[McpServerToolType]
public static class AvaloniaAgentMcpTools
{
    [McpServerTool]
    [Description("Handshake with the Avalonia agent host: protocol version, app title, process id.")]
    public static async Task<string> UiHello(CancellationToken cancellationToken = default)
    {
        var response = await AvaloniaAgentRuntime.WithClientAsync(
            c => c.HelloAsync(cancellationToken).AsTask(),
            cancellationToken).ConfigureAwait(false);
        return AvaloniaAgentRuntime.ToJson(response);
    }

    [McpServerTool]
    [Description("Dump the Avalonia interactive control tree (ids, roles, bounds, text, enabled/focused).")]
    public static async Task<string> UiTree(
        [Description("When true (default), only interactive controls and AgentId-tagged controls.")]
        bool interactiveOnly = true,
        CancellationToken cancellationToken = default)
    {
        var response = await AvaloniaAgentRuntime.WithClientAsync(
            c => c.TreeAsync(interactiveOnly, cancellationToken).AsTask(),
            cancellationToken).ConfigureAwait(false);
        return AvaloniaAgentRuntime.ToJson(response);
    }

    [McpServerTool]
    [Description("Capture a PNG screenshot of the Avalonia window (or a control by id). Returns JSON with file path under %TEMP%/novolis-avalonia-agent/.")]
    public static async Task<string> UiScreenshot(
        [Description("Optional AgentId / control id. Null = whole window.")]
        string? controlId = null,
        [Description("Optional max width in pixels; height scales proportionally.")]
        int? maxWidth = null,
        CancellationToken cancellationToken = default)
    {
        var response = await AvaloniaAgentRuntime.WithClientAsync(
            c => c.ScreenshotAsync(controlId, maxWidth, cancellationToken).AsTask(),
            cancellationToken).ConfigureAwait(false);

        if (!response.Success)
            return AvaloniaAgentRuntime.ToJson(response);

        var path = AvaloniaAgentRuntime.WriteScreenshot(response);
        return AvaloniaAgentRuntime.ToJson(new
        {
            response.RequestId,
            response.Success,
            response.Error,
            path,
            response.Width,
            response.Height
        });
    }

    [McpServerTool]
    [Description("Click an Avalonia control by AgentId, or at window coordinates (x, y).")]
    public static async Task<string> UiClick(
        [Description("Stable AgentId, e.g. lab.recovery.")]
        string? controlId = null,
        [Description("Window X when controlId is omitted.")]
        double? x = null,
        [Description("Window Y when controlId is omitted.")]
        double? y = null,
        CancellationToken cancellationToken = default)
    {
        var response = await AvaloniaAgentRuntime.WithClientAsync(
            c => c.ClickAsync(controlId, x, y, cancellationToken).AsTask(),
            cancellationToken).ConfigureAwait(false);
        return AvaloniaAgentRuntime.ToJson(response);
    }

    [McpServerTool]
    [Description("Type text into a focused or id-targeted control, and/or send special keys (Enter, Tab, Escape).")]
    public static async Task<string> UiType(
        [Description("Optional control AgentId to focus first.")]
        string? controlId = null,
        [Description("Text to append into a TextBox.")]
        string? text = null,
        [Description("Special keys, e.g. Enter, Tab, Escape.")]
        string[]? keys = null,
        CancellationToken cancellationToken = default)
    {
        var response = await AvaloniaAgentRuntime.WithClientAsync(
            c => c.TypeAsync(controlId, text, keys, cancellationToken).AsTask(),
            cancellationToken).ConfigureAwait(false);
        return AvaloniaAgentRuntime.ToJson(response);
    }

    [McpServerTool]
    [Description("Wait until a control id appears and optional enabled/text conditions match.")]
    public static async Task<string> UiWait(
        [Description("Control AgentId to wait for.")]
        string controlId,
        [Description("Optional required IsEnabled value.")]
        bool? enabled = null,
        [Description("Optional substring that control text must contain.")]
        string? textContains = null,
        [Description("Timeout in milliseconds (default 5000).")]
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        var response = await AvaloniaAgentRuntime.WithClientAsync(
            c => c.WaitAsync(controlId, enabled, textContains, timeoutMs, cancellationToken).AsTask(),
            cancellationToken).ConfigureAwait(false);
        return AvaloniaAgentRuntime.ToJson(response);
    }
}
