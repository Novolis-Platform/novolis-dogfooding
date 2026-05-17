// MCP client: spawns Bridge Commander --mcp and plays a QA session via real stdio protocol.
// Run from repo: dotnet run scripts/bridge-mcp-play.cs
#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0
#:property ImplicitUsings=enable
#:property Nullable=enable
#:package ModelContextProtocol

using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

static string? FindDll()
{
    var dir = Directory.GetCurrentDirectory();
    for (var i = 0; i < 8 && dir is not null; i++)
    {
        foreach (var rel in new[]
        {
            Path.Combine("bin", "Release", "net10.0", "BridgeCommander.dll"),
            Path.Combine("novolis-dogfooding", "apps", "BridgeCommander", "bin", "Release", "net10.0", "BridgeCommander.dll")
        })
        {
            var candidate = Path.GetFullPath(Path.Combine(dir, rel));
            if (File.Exists(candidate))
                return candidate;
        }

        dir = Path.GetDirectoryName(dir);
    }

    return null;
}

var dll = FindDll();
if (dll is null)
{
    Console.Error.WriteLine("Build first: cd apps/BridgeCommander && dotnet build -c Release");
    return 1;
}

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "bridge-commander",
    Command = "dotnet",
    Arguments = ["exec", dll, "--mcp"],
    WorkingDirectory = Path.GetDirectoryName(dll)!
});

await using var client = await McpClient.CreateAsync(transport);

Console.WriteLine("=== Bridge Commander MCP connected ===\n");

var tools = await client.ListToolsAsync();
Console.WriteLine($"Tools: {string.Join(", ", tools.Select(t => t.Name))}\n");

static async Task<string> Call( McpClient c, string tool, Dictionary<string, object?> args)
{
    var result = await c.CallToolAsync(tool, args);
    return result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "";
}

// 1. Initial snapshot
var snap = await Call(client, "get_bridge_snapshot", []);
PrintSection("Initial snapshot", snap);

// 2. Reset and verify
var reset = await Call(client, "reset_bridge", []);
PrintSection("After reset", reset);

// 3. Natural orders (QA engineer walkthrough)
var orders = new[]
{
    "weaps target the closest enemy",
    "tactical fire weapons",
    "helm set heading to 122 mark 6 by 180 mark 2",
    "helm course 122 by 33",
    "eng divert shields",
    "helm all ahead full"
};

foreach (var order in orders)
{
    var result = await Call(client, "transmit_order", new Dictionary<string, object?>
    {
        ["prompt"] = order,
        ["waitForIdle"] = true
    });
    PrintSection($"Order: {order}", result);
}

// 4. Parse failure + suggestions
var bad = await Call(client, "transmit_order", new Dictionary<string, object?>
{
    ["prompt"] = "helm ful stop",
    ["waitForIdle"] = false
});
PrintSection("Typo order (expect suggestions)", bad);

// 5. Clear queue stress
_ = await Call(client, "transmit_order", new Dictionary<string, object?> { ["prompt"] = "helm warp 5", ["waitForIdle"] = false });
_ = await Call(client, "transmit_order", new Dictionary<string, object?> { ["prompt"] = "helm warp 8", ["waitForIdle"] = false });
var clear = await Call(client, "transmit_order", new Dictionary<string, object?> { ["prompt"] = "clear queue", ["waitForIdle"] = true });
PrintSection("Clear queue", clear);

// 6. KR-12 scenario via built-in QA
var kr12 = await Call(client, "run_qa_scenario", new Dictionary<string, object?> { ["scenario"] = "kr12-engagement" });
PrintSection("QA scenario kr12-engagement", kr12);

// 7. Final state
var final = await Call(client, "get_bridge_snapshot", []);
PrintSection("Final snapshot", final);

Console.WriteLine("\n=== MCP play session complete ===");
return 0;

static void PrintSection(string title, string json)
{
    Console.WriteLine($"--- {title} ---");
    try
    {
        using var doc = JsonDocument.Parse(json);
        Console.WriteLine(JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
    }
    catch
    {
        Console.WriteLine(json);
    }

    Console.WriteLine();
}
