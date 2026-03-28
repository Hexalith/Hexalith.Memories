# Hexalith.Memories

Hexalith Memory Module with syntactic and semantic search.

## Quick start

Initialize the required submodules before building the solution:

```bash
git submodule update --init --recursive
dotnet build
dotnet run --project src/Hexalith.Memories.AppHost
```

The submodules are checked out under:

- `src/submodules/Hexalith.Commons`
- `src/submodules/Hexalith.EventStore`

On first AppHost run, a gitignored local `secrets.json` placeholder is created automatically if it does not already exist.

## Local development stack

Running the AppHost boots the local development topology:

- Redis Stack on port `6379`
- FalkorDB on port `6380`
- Memories Server on port `5000`
- Dapr sidecar HTTP on port `3500`
- Dapr sidecar gRPC on port `50001`
- Aspire Dashboard opened by the AppHost

The deployment-oriented Dapr component manifests live under `deploy/dapr/components/`. The AppHost also attaches equivalent Dapr component resources for local development so the sidecar can load the state store and secret store automatically.

## Useful endpoints

- `http://localhost:5000/health`
- `http://localhost:5000/alive`
- `http://localhost:5000/ready`
