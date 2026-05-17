using System.Text.Json;
using BridgeCommander.Bridge;
using BridgeCommander.Bridge.Mcp;
using Hex1b;

if (args.Contains("--mcp", StringComparer.OrdinalIgnoreCase))
{
    await BridgeMcpHost.RunStdioAsync(args);
    return;
}

if (args.Contains("--qa-smoke", StringComparer.OrdinalIgnoreCase))
{
    Environment.Exit(await BridgeMcpHost.RunQaSmokeAsync());
    return;
}

if (args.Contains("--mcp-test", StringComparer.OrdinalIgnoreCase))
{
    Environment.Exit(await BridgeMcpIntegrationTest.RunAsync());
    return;
}

if (args.Contains("--mcp-play", StringComparer.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Use: dotnet run --file scripts/bridge-mcp-play.cs (from apps/BridgeCommander)");
    Environment.Exit(1);
    return;
}

var transmitIndex = Array.FindIndex(args, a => string.Equals(a, "--transmit", StringComparison.OrdinalIgnoreCase));
if (transmitIndex >= 0 && transmitIndex + 1 < args.Length)
{
    await using var cliSession = BridgeSession.Create();
    var result = await cliSession.TransmitAsync(string.Join(' ', args[(transmitIndex + 1)..]));
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    return;
}

await using var session = BridgeSession.Create();
var app = BridgeHexApp.Create(session);
await app.RunAsync();
