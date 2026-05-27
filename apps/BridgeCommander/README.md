# Bridge Commander

Sci-fi bridge captain console dogfooding [Novolis.Commands](https://github.com/Novolis-Platform/novolis-commands) and **Novolis.Audio.Voice.Atc** (Sherpa Piper + radio DSP).

Built with **[Spectre.Console](https://spectreconsole.net)** — no Hex1b TUI.

## Run

**Default — full voiced patrol exchange** (scripted game sequence):

```bash
dotnet run --project apps/BridgeCommander
```

**Interactive manual orders** (Spectre panels + voice acknowledgments):

```bash
dotnet run --project apps/BridgeCommander -- --interactive
```

**Silent** (Spectre only, no TTS):

```bash
dotnet run --project apps/BridgeCommander -- --no-voice
```

Requires `Novolis.Audio.Voice.SherpaOnnx` **2026.1.3+** on GitHub Packages (bundled Piper model zip). Voice stack **2026.1.5+** adds ATC radio effects.

## Exchange script

`BridgeExchangeScript.PatrolEngagement` runs a full duty-shift sequence: computer/XO brief, captain orders (helm, tactical, engineering, comms, nav), station voice responses, and closing narration. Each beat is shown in Spectre markup and spoken in order (blocking playback).

## MCP (agent / QA automation)

Stdio MCP server for Cursor and other MCP clients:

```bash
dotnet build -c Release
dotnet exec bin/Release/net10.0/BridgeCommander.dll --mcp
```

**Cursor:** workspace `.cursor/mcp.json` registers `bridge-commander`. MCP/QA modes disable voice automatically.

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
