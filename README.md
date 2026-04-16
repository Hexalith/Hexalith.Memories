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

## CLI (preview)

Story 7.2 expands the `memories` .NET global tool with `--format human|json|table`, three-axis search (`memories search query`), memory-unit inspection (`memories search inspect`), and the `--explain` score breakdown. Rich error messages, the quickstart wizard, and search/access telemetry land in Stories 7.3-7.5.

```bash
dotnet pack src/Hexalith.Memories.Cli -c Release -o ./artifacts
dotnet tool install -g --add-source ./artifacts Hexalith.Memories.Cli
memories --version
memories --format json tenant list
```

Use `--format json` for scripts and LLM agents — the envelope is versioned and stable. See:

- [CLI configuration](docs/dev/cli-config.md) — endpoint resolution, environment variables, PATH troubleshooting.
- [CLI output formats](docs/dev/cli-output-formats.md) — envelope schema, per-command examples, versioning policy.

## Operations

- [Rate limiting — per-tenant ceilings, 429 handling, extraction gate](docs/operations/rate-limiting.md)
- [Failure recovery — failed-unit registry, re-ingestion, retry policy overrides](docs/operations/failure-recovery.md)
- [Pipeline persistence — Redis durability, restart validation, warm restart and throughput benchmark](docs/operations/pipeline-persistence.md)
