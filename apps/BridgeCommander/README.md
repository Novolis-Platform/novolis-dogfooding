# Bridge Commander

Sci-fi bridge captain TUI dogfooding [Novolis.Commands](https://github.com/Novolis-Platform/novolis-commands).

## Run

```bash
dotnet run --project apps/BridgeCommander
```

Spoken bridge acknowledgments use **Novolis.Audio.Voice.Atc** (Sherpa Piper TTS from GPR). Disable with `--no-voice` (MCP/QA modes disable voice automatically).

```bash
dotnet run --project apps/audio/VoiceSmoke
dotnet run --project apps/audio/VoiceSmoke -- --null
```

## MCP (agent / QA automation)

Stdio MCP server for Cursor and other MCP clients:

```bash
dotnet build -c Release
dotnet exec bin/Release/net10.0/BridgeCommander.dll --mcp
```

**Cursor:** workspace `.cursor/mcp.json` registers `bridge-commander`. After build, open **Settings → MCP**, enable `bridge-commander`, and click refresh. Tool names are snake_case (`get_bridge_snapshot`, `transmit_order`, …).

**Play via MCP client (stdio, full session):**

```bash
cd apps/BridgeCommander
dotnet build -c Release
dotnet run --file scripts/bridge-mcp-play.cs
```

| Tool | Description |
|------|-------------|
| `GetBridgeSnapshot` | Ship state + command log |
| `TransmitOrder` | Submit an order (waits for queue idle by default) |
| `ResetBridge` | Reset to duty-shift defaults |
| `RunQaScenario` | Run built-in QA script |
| `ListQaScenarios` | List scenario names |

## Test suites

```bash
dotnet run -- --qa-smoke      # 5 end-to-end scenarios
dotnet run -- --mcp-test      # 34 MCP tool assertions
dotnet run -- --transmit "helm heading 270"   # one-off JSON result
pwsh scripts/test-mcp-stdio.ps1               # stdio protocol smoke
```
