# Hexalith.Memories.Client.Rest

Typed REST client for the Hexalith.Memories Server.

Use this package from applications, tools, and services that need to call the Memories HTTP API
through typed request and response models instead of hand-built HTTP calls.

```powershell
dotnet add package Hexalith.Memories.Client.Rest
```

The client package depends on `Hexalith.Memories.Contracts` and uses Microsoft HTTP client
extensions for registration and resilient outbound calls.
