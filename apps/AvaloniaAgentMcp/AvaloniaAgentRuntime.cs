using System.Text.Json;
using Novolis.Avalonia.Agent.Protocol;
using Novolis.Avalonia.Agent.Protocol.Dto;

namespace AvaloniaAgentMcp;

internal static class AvaloniaAgentRuntime
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static UiAgentClient? _client;

    public static async Task<T> WithClientAsync<T>(Func<UiAgentClient, Task<T>> action, CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _client ??= new UiAgentClient();
            if (!_client.IsConnected)
                await _client.ConnectDefaultAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return await action(_client).ConfigureAwait(false);
            }
            catch
            {
                await ResetClientAsync().ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task ResetClientAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync().ConfigureAwait(false);
            _client = null;
        }
    }

    public static string ToJson<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ScreenshotDir
    {
        get
        {
            var dir = Path.Combine(Path.GetTempPath(), "novolis-avalonia-agent");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string WriteScreenshot(UiScreenshotResponseDto response)
    {
        if (!response.Success || response.Png is null || response.Png.Length == 0)
            throw new InvalidOperationException(response.Error ?? "Screenshot failed.");

        var path = Path.Combine(ScreenshotDir, $"shot-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.png");
        File.WriteAllBytes(path, response.Png);
        return path;
    }
}
