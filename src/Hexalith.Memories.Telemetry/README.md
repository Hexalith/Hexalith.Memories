# Hexalith.Memories.Telemetry

Shared OpenTelemetry names for Hexalith.Memories.

Use this package when another Memories component needs the canonical `ActivitySource`, meter names,
metric instruments, or trace tags used across the server, client, CLI, MCP, and integration tests.

```powershell
dotnet add package Hexalith.Memories.Telemetry
```

Keeping these constants in one package prevents trace and metric naming drift between independently
published Memories packages.
