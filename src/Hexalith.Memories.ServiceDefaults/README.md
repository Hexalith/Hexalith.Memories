# Hexalith.Memories.ServiceDefaults

Shared hosting defaults for Hexalith.Memories services, including health endpoints, service
discovery, HTTP resilience, and OpenTelemetry registration.

```powershell
dotnet add package Hexalith.Memories.ServiceDefaults
```

Call `AddServiceDefaults()` during service registration only when the service registers the keyed
`redis` and `falkordb` connection multiplexers required by Redis OpenTelemetry instrumentation.
Services without those backends, including `Hexalith.Memories.Mcp`, must call
`AddServiceDefaults(configureRedisInstrumentation: false)`. Call `MapDefaultEndpoints()` when
mapping application endpoints in either mode.

This package is published because packable service packages such as `Hexalith.Memories.Mcp`
consume it directly; all `Hexalith.Memories.*` package dependencies must use the same release
version.
